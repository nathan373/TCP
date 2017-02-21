using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Rhino.Mocks;

namespace UnitTest
{
    /// <summary>
    /// A test class for ...
    /// </summary>
    [TestFixture]
    public class TCPConnectionTest
    {
        private const string TCP_HOST = "127.0.0.1";
        private const int TCP_PORT = 55555;

        MockRepository _mocks;
        ISocket _mockSocket;

        /// <summary>
        /// This setup funcitons runs before each test method
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            _mockSocket = _mocks.StrictMock<ISocket>();
            _FTPMessages = new List<String>();
        }

        /// <summary>
        /// This setup funcitons runs after each test method
        /// </summary>
        [TearDown]
        public void TearDownForEachTest()
        {
        }

        /// <summary>
        /// Tests the opening of a new FTP connection.
        /// </summary>
        [Test]
        public void OpenConnection()
        {
            ExpectConnect(_mockSocket);

            ReplayAll();

            // Get endpoint for the listener.                
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(TCP_HOST), TCP_PORT);

            SocketClient client = new SocketClient(_mockSocket, localEndPoint);

            _mocks.VerifyAll();
        }
        
        /// <summary>
		/// Logs in to the FTP server.
		/// </summary>
		
        /// <summary>
        /// Tests closing the connection.
        /// </summary>
        [Test]
        public void Close()
        {
            ExpectConnect(_mockSocket);

            ExpectSend(_mockSocket, "QUIT");
            ExpectReceive(_mockSocket, "226 Closing fake connection");

            ReplayAll();

            // Get endpoint for the listener.                
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(TCP_HOST), TCP_PORT);

            SocketClient client = new SocketClient(_mockSocket, localEndPoint);
            client.Close();
            _mocks.VerifyAll();
        }

        #region Delegate for the Receive method (needed because the parameter "buffer" is modified)
        private delegate int ReceiveDelegate(byte[] buffer, int size, SocketFlags flags);
        /// <summary>
        /// A delegate for Receive that is called by ExpectReceive.
        /// Copies the response message to the buffer and returns the size.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private int Receive(byte[] buffer, int size, SocketFlags flags)
        {
            _FTPMessageIterator.MoveNext();
            byte[] str = Encoding.ASCII.GetBytes(_FTPMessageIterator.Current);
            for (int i = 0; i < str.Length; i++)
                buffer[i] = str[i];
            return str.Length;
        }
        #endregion

        #region Needed to pass messages to the Receive delegate
        private IList<String> _FTPMessages;
        private IEnumerator<String> _FTPMessageIterator;
        #endregion

        #region Helper methods (to avoid duplicated code)
        private void ReplayAll()
        {
            _mocks.ReplayAll();
            _FTPMessageIterator = _FTPMessages.GetEnumerator();
        }

        /// <summary>
        /// Expect a call of the Connect method and simulate a correct response by the FTP server.
        /// </summary>
        /// <param name="mockSocket">The fake socket.</param>
        private void ExpectConnect(ISocket mockSocket)
        {
            mockSocket.Connect(TCP_HOST, TCP_PORT);
            ExpectReceive(mockSocket, "220 This is a fake FTP server");
        }

        /// <summary>
        /// Expect a call of the Receive method and simulate a response by the FTP server using the given message.
        /// </summary>
        /// <param name="mockSocket">The fake socket.</param>
        /// <param name="message">The message to return.</param>
        private void ExpectReceive(ISocket mockSocket, string message)
        {
            _FTPMessages.Add(message);
            Expect.Call(mockSocket.Receive(new byte[SocketClient.BLOCK_SIZE], SocketClient.BLOCK_SIZE, 0)).Do(new ReceiveDelegate(Receive));
        }

        /// <summary>
        /// Expect a call of the Send method with the given message.
        /// </summary>
        /// <param name="mockSocket">The fake socket.</param>
        /// <param name="message">The expected message.</param>
        private void ExpectSend(ISocket mockSocket, string message)
        {
            byte[] buffer = new byte[SocketClient.BLOCK_SIZE];
            buffer = Encoding.ASCII.GetBytes(message + "\r\n");
            Expect.Call(mockSocket.Send(buffer, buffer.Length, 0)).Return(buffer.Length);
        }
        #endregion
    }
}
