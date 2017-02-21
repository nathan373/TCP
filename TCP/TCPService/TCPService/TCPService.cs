using System;
using System.Net;
using System.ServiceProcess;
using CommunicationsLibrary;

namespace TCPService
{
    /// <summary>
    /// Class that will run as a Windows Service and its display name is
    /// TCP (Production Demo) in Windows Services.
    /// This service basically start a server on service start 
    /// (on OnStart method) and shutdown the server on the servie stop 
    /// (on OnStop method).
    /// </summary>
    public partial class TCPService : ServiceBase
    {
        //If you make this true, then the above "watch-" variables will print to
        //both Console and log, instead of just to log. I suggest only using this if
        //you are having a problem with an application that is crashing.
        private static readonly bool consoleWatch = false;

        //This variable determines the number of 
        //SocketAsyncEventArg objects put in the pool of objects for receive/send.
        //The value of this variable also affects the Semaphore.
        //This app uses a Semaphore to ensure that the max # of connections
        //value does not get exceeded.
        //Max # of connections to a socket can be limited by the Windows Operating System
        //also.
        private const int maxNumberOfConnections = 3000;

        private const string DEFAULT_SERVER = "127.0.0.1";
        private const int DEFAULT_PORT = 55555;

        //You would want a buffer size larger than 25 probably, unless you know the
        //data will almost always be less than 25. It is just 25 in our test app.
        private const int testBufferSize = 25;

        //This is the maximum number of asynchronous accept operations that can be 
        //posted simultaneously. This determines the size of the pool of 
        //SocketAsyncEventArgs objects that do accept operations. Note that this
        //is NOT the same as the maximum # of connections.
        private const int maxSimultaneousAcceptOps = 10;

        //For the BufferManager
        private const int opsToPreAlloc = 2;    // 1 for receive, 1 for send

        //allows excess SAEA objects in pool.
        private const int excessSaeaObjectsInPool = 1;

        //This is for logging during testing.        
        //You can change the path in the TestFileWriter class if you need to.
        public Log log;

        private SocketListener listener;

        public TCPService()
        {
            this.InitializeComponent();
        }

        public void OnDebug()
        {
            this.OnStart(null);
        }

        /// <summary>
        /// This method starts the polling Timer each time the service is started
        /// </summary>
        protected override void OnStart(string[] args)
        {
            //Create a log file writer, so you can see the flow easily.
            //It can be printed. Makes it easier to figure out complex program flow.
            //The log StreamWriter uses a buffer. So it will only work right if you close
            //the server console properly at the end of the test.
            log = new Log(consoleWatch);

            try
            {
                // Get endpoint for the listener.                
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(DEFAULT_SERVER), DEFAULT_PORT);

                //This object holds a lot of settings that we pass from Main method
                //to the SocketListener. In a real app, you might want to read
                //these settings from a database or windows registry settings that
                //you would create.
                SocketListenerSettings theSocketListenerSettings =
                    new SocketListenerSettings
                    (maxNumberOfConnections, excessSaeaObjectsInPool, maxSimultaneousAcceptOps, testBufferSize, opsToPreAlloc, localEndPoint);

                listener = new SocketListener(theSocketListenerSettings, log);
                listener.Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), DEFAULT_PORT));

                log.WriteLine("Server listening on port {0}. Press any key to terminate the server process..." + DEFAULT_PORT);
            }
            catch (Exception ex)
            {
                log.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// This method stops the polling Timer each time the service is stopped
        /// </summary>
        protected override void OnStop()
        {
            log.Close();
            listener.Stop();
        }  
    }
}
