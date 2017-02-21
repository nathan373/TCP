using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CommunicationsLibrary.AsyncSocketCore;
using System.Text;

namespace CommunicationsLibrary
{
    public sealed class SocketListener : ISocketListener
    {
        private const int MessageHeaderSize = 4;
        private int _receivedHandShakeCount = 0; 

        private BlockingCollection<MessageData> _sendingQueue;
        private Thread _sendMessageWorker;

        private static Mutex _mutex = new Mutex();

        //Buffers for sockets are unmanaged by .NET. 
        //So memory used for buffers gets "pinned", which makes the
        //.NET garbage collector work around it, fragmenting the memory. 
        //Circumvent this problem by putting all buffers together 
        //in one block in memory. Then we will assign a part of that space 
        //to each SocketAsyncEventArgs object, and
        //reuse that buffer space each time we reuse the SocketAsyncEventArgs object.
        //Create a large reusable set of buffers for all socket operations.
        BufferManager _theBufferManager;

        // the socket used to listen for incoming connection requests
        private Socket _listenSocket;
        
        //total clients connected to the server
        private int _connectedSocketCount;
        
        // pool of reusable SocketAsyncEventArgs objects for receive and send socket operations
        private AsyncSocketEventArgsPool _poolOfRecSendEventArgs;

        //A Semaphore has two parameters, the initial number of available slots
        // and the maximum number of slots. We'll make them the same. 
        //This Semaphore is used to keep from going over max connection #.
        private Semaphore _acceptedClientsSemaphore;
        private AutoResetEvent _waitSendEvent;
        
        private readonly ISocketListenerSettings _socketListenerSettings;
        private readonly ILog _log;

        public SocketListener(ISocketListenerSettings theSocketListenerSettings, ILog log)
        {
            this._socketListenerSettings = theSocketListenerSettings;
            this._log = log;
            _log.WriteLine("SocketListener constructor");
            //Allocate memory for buffers. We are using a separate buffer space for
            //receive and send, instead of sharing the buffer space, like the Microsoft
            //example does.            
            this._theBufferManager = new BufferManager(this._socketListenerSettings.BufferSize * this._socketListenerSettings.NumberOfSaeaForRecSend * this._socketListenerSettings.OpsToPreAllocate,
            this._socketListenerSettings.BufferSize * this._socketListenerSettings.OpsToPreAllocate);
            
            this._poolOfRecSendEventArgs = new AsyncSocketEventArgsPool(this._socketListenerSettings.NumberOfSaeaForRecSend, log);
            
            // Create a semaphore that can satisfy up to maxConnectionCount concurrent requests. 
            _acceptedClientsSemaphore = new Semaphore(this._socketListenerSettings.MaxConnections, this._socketListenerSettings.MaxConnections);

            _sendingQueue = new BlockingCollection<MessageData>();

            _sendMessageWorker = new Thread(new ThreadStart(SendQueueMessage));

            Init();

            _waitSendEvent = new AutoResetEvent(false);
        }

       
        public void Start(IPEndPoint localEndPoint)
        {
            _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.ReceiveBufferSize = this._socketListenerSettings.BufferSize;
            _listenSocket.SendBufferSize = this._socketListenerSettings.BufferSize;
            _listenSocket.Bind(localEndPoint);
            _listenSocket.Listen(this._socketListenerSettings.MaxAcceptOps);
            _sendMessageWorker.Start();
            StartAccept(null);
            _mutex.WaitOne();
        }

        public void Stop()
        {
            try
            {
                _log.WriteLine("Socket Closed.");
                _listenSocket.Close();
            }
            catch (Exception e)
            {
                _log.WriteLine(e.ToString());
            }
            _mutex.ReleaseMutex();
        }

        //____________________________________________________________________________
        // initializes the server by preallocating reusable buffers and 
        // context objects (SocketAsyncEventArgs objects).  
        //It is NOT mandatory that you preallocate them or reuse them. But, but it is 
        //done this way to illustrate how the API can 
        // easily be used to create reusable objects to increase server performance.
        private void Init()
        {
            // Allocate one large byte buffer block, which all I/O operations will 
            //use a piece of. This gaurds against memory fragmentation.
            this._theBufferManager.InitBuffer();

            //The pool that we built ABOVE is for SocketAsyncEventArgs objects that do
            // accept operations. 
            //Now we will build a separate pool for SAEAs objects 
            //that do receive/send operations. One reason to separate them is that accept
            //operations do NOT need a buffer, but receive/send operations do. 
            //ReceiveAsync and SendAsync require
            //a parameter for buffer size in SocketAsyncEventArgs.Buffer.
            // So, create pool of SAEA objects for receive/send operations.
            SocketAsyncEventArgs eventArgObjectForPool;

            for (int i = 0; i < this._socketListenerSettings.NumberOfSaeaForRecSend; i++)
            {
                eventArgObjectForPool = new SocketAsyncEventArgs();

                // assign a byte buffer from the buffer block to 
                //this particular SocketAsyncEventArg object
                this._theBufferManager.SetBuffer(eventArgObjectForPool);

                eventArgObjectForPool.Completed += OnIoCompleted;

                _poolOfRecSendEventArgs.Push(eventArgObjectForPool);
            }
            _log.WriteLine("Init method");
        }


        private void OnIoCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    _log.WriteLine("The last operation completed on the socket was not a receive or send");
                    break;
            }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += (sender, e) => ProcessAccept(e);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            //Semaphore class is used to control access to a resource or pool of 
            //resources. Enter the semaphore by calling the WaitOne method, which is 
            //inherited from the WaitHandle class, and release the semaphore 
            //by calling the Release method. This is a mechanism to prevent exceeding
            // the max # of connections we specified. We'll do this before
            // doing AcceptAsync. If maxConnections value has been reached,
            //then the application will pause here until the Semaphore gets released,
            //which happens in the CloseClientSocket method.    
            _acceptedClientsSemaphore.WaitOne();

            //Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is 
            //working asynchronously. The 
            //SocketAsyncEventArgs.Completed event on the acceptEventArg parameter 
            //will be raised upon completion of accept op.
            //AcceptAsync will call the AcceptEventArg_Completed
            //method when it completes, because when we created this SocketAsyncEventArgs
            //object before putting it in the pool, we set the event handler to do it.
            //AcceptAsync returns false if the I/O operation completed synchronously.            
            //The SocketAsyncEventArgs.Completed event on the acceptEventArg 
            //parameter will NOT be raised when AcceptAsync returns false.
            if (!_listenSocket.AcceptAsync(acceptEventArg))
            {
                //The code in this if (!willRaiseEvent) statement only runs 
                //when the operation was completed synchronously. It is needed because 
                //when Socket.AcceptAsync returns false, 
                //it does NOT raise the SocketAsyncEventArgs.Completed event.
                //And we need to call ProcessAccept and pass it the SocketAsyncEventArgs object.
                //This is only when a new connection is being accepted.
                ProcessAccept(acceptEventArg);
                _log.WriteLine("Socket ProcessAccept.");
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                SocketAsyncEventArgs readEventArgs = _poolOfRecSendEventArgs.Pop();
                if (readEventArgs != null)
                {
                    readEventArgs.UserToken = new AsyncSocketUserToken(e.AcceptSocket);
                    Interlocked.Increment(ref _connectedSocketCount);
                    _log.WriteLine("Client connection accepted. There are " + _connectedSocketCount + " clients connected to the server");
                    if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                    {
                        ProcessReceive(readEventArgs);
                    }
                }
                else
                {
                    _log.WriteLine("There are no more available sockets to allocate.");
                }
            }
            catch (SocketException ex)
            {
                AsyncSocketUserToken token = e.UserToken as AsyncSocketUserToken;
                _log.WriteLine("Error when processing data received from" + token.Socket.RemoteEndPoint +":\r\n" + ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Accept the next connection request.
            StartAccept(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                AsyncSocketUserToken token = e.UserToken as AsyncSocketUserToken;

                
                ProcessReceivedData(token.DataStartOffset, token.NextReceiveOffset - token.DataStartOffset + e.BytesTransferred, 0, token, e);

               
                token.NextReceiveOffset += e.BytesTransferred;

               
                if (token.NextReceiveOffset == e.Buffer.Length)
                {
                   
                    token.NextReceiveOffset = 0;

                   
                    if (token.DataStartOffset < e.Buffer.Length)
                    {
                        var notYesProcessDataSize = e.Buffer.Length - token.DataStartOffset;
                        Buffer.BlockCopy(e.Buffer, token.DataStartOffset, e.Buffer, 0, notYesProcessDataSize);

                       
                        token.NextReceiveOffset = notYesProcessDataSize;
                    }

                    token.DataStartOffset = 0;
                }

               
                e.SetBuffer(token.NextReceiveOffset, e.Buffer.Length - token.NextReceiveOffset);

                
                if (e.SocketError == SocketError.Success && !token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void ProcessReceivedData(int dataStartOffset, int totalReceivedDataSize, int alreadyProcessedDataSize, AsyncSocketUserToken token, SocketAsyncEventArgs e)
        {
            if (alreadyProcessedDataSize >= totalReceivedDataSize)
            {
                return;
            }

            if (e.UserToken != null && token.MessageSize == null)
            {
                
                if (totalReceivedDataSize > MessageHeaderSize)
                {
                   
                    var headerData = new byte[MessageHeaderSize];
                    Buffer.BlockCopy(e.Buffer, dataStartOffset, headerData, 0, MessageHeaderSize);
                    var messageSize = BitConverter.ToInt32(headerData, 0);

                    token.MessageSize = messageSize;
                    token.DataStartOffset = dataStartOffset + MessageHeaderSize;

                    
                    ProcessReceivedData(token.DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + MessageHeaderSize, token, e);
                }
            }
            else if (e.UserToken != null)
            {
                var messageSize = token.MessageSize.Value;
                
                if (totalReceivedDataSize - alreadyProcessedDataSize >= messageSize)
                {
                    var messageData = new byte[messageSize];
                    Buffer.BlockCopy(e.Buffer, dataStartOffset, messageData, 0, messageSize);
                    ProcessMessage(messageData, token, e);

                    
                    token.DataStartOffset = dataStartOffset + messageSize;
                    token.MessageSize = null;

                    ProcessReceivedData(token.DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + messageSize, token, e);
                }
            }
        }

        private void ProcessMessage(byte[] messageData, AsyncSocketUserToken token, SocketAsyncEventArgs e)
        {
            var value = Encoding.ASCII.GetString(messageData).ToUpper();
            _log.WriteLine("received command: " + Encoding.ASCII.GetString(messageData) +" length:" + messageData.Length);
            
            switch (value)
            {
                case "HELO":
                    Interlocked.Increment(ref _receivedHandShakeCount);
                    _sendingQueue.Add(new MessageData { Message = Encoding.ASCII.GetBytes("HI"), Token = token });
                    break;
                case "CONNECTIONS":
                    
                    if (_receivedHandShakeCount > 0)
                    {
                        _sendingQueue.Add(new MessageData
                        {
                            Message = Encoding.ASCII.GetBytes(_receivedHandShakeCount.ToString()),
                            Token = token
                        });
                        }
                    break;
                case "PRIME":
                    if (_receivedHandShakeCount > 0)
                    {
                        _sendingQueue.Add(new MessageData {Message = Encoding.ASCII.GetBytes("142339"), Token = token});
                    }
                    break;
                case "TERMINATE":
                    if (_receivedHandShakeCount > 0)
                    {
                        int i;
                        _sendingQueue.Add(new MessageData {Message = Encoding.ASCII.GetBytes("BYE"), Token = token});
                      
                        CloseClientSocket(e);
                    }
                    break;
                default:
                    {
                        //Ignore all other message.
                        //sendingQueue.Add(new MessageData { Message = messageData, Token = token });
                        break;
                    }
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            _poolOfRecSendEventArgs.Push(e);
            _waitSendEvent.Set();
        }
        private void SendQueueMessage()
        {
            while (true)
            {
                var messageData = _sendingQueue.Take();
                if (messageData != null)
                {
                    SendMessage(messageData, BuildMessage(messageData.Message));
                }
            }
        }

        private void SendMessage(MessageData messageData, byte[] message)
        {
            var sendEventArgs = _poolOfRecSendEventArgs.Pop();
            if (sendEventArgs != null)
            {
                sendEventArgs.SetBuffer(message, 0, message.Length);
                sendEventArgs.UserToken = messageData.Token;
                messageData.Token.Socket.SendAsync(sendEventArgs);
            }
            else
            {
                _waitSendEvent.WaitOne();
                SendMessage(messageData, message);
            }
        }

        private static byte[] BuildMessage(byte[] data)
        {
            var header = BitConverter.GetBytes(data.Length);
            var message = new byte[header.Length + data.Length];
            header.CopyTo(message, 0);
            data.CopyTo(message, header.Length);
            return message;
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            try
            {
                e.SocketError = SocketError.Shutdown;
                var token = e.UserToken as AsyncSocketUserToken;
                token.Dispose();
            }
            catch (Exception)
            {
                _log.WriteLine("Failed to shutdown Socket");
            }
            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            

            // Put the SocketAsyncEventArg back into the pool,
            // to be used by another client. This 
            this._poolOfRecSendEventArgs.Push(e);

            // decrement the counter keeping track of the total number of clients 
            //connected to the server
            Interlocked.Decrement(ref _connectedSocketCount);
            _log.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server" + _connectedSocketCount);
            
            //Release Semaphore so that its connection counter will be decremented.
            //This must be done AFTER putting the SocketAsyncEventArg back into the pool
            _acceptedClientsSemaphore.Release();
        }
    }
}
