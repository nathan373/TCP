using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunicationsLibrary;
using System.Net;

namespace TestServer
{
    static class Program
    {
        //If you make this true, then the above "watch-" variables will print to
        //both Console and log, instead of just to log. I suggest only using this if
        //you are having a problem with an application that is crashing.
        public static readonly bool consoleWatch = true;

        //This variable determines the number of 
        //SocketAsyncEventArg objects put in the pool of objects for receive/send.
        //The value of this variable also affects the Semaphore.
        //This app uses a Semaphore to ensure that the max # of connections
        //value does not get exceeded.
        //Max # of connections to a socket can be limited by the Windows Operating System
        //also.
        public const int maxNumberOfConnections = 3000;

        public const string DEFAULT_SERVER = "127.0.0.1";
        public const int DEFAULT_PORT = 55555;

        //You would want a buffer size larger than 25 probably, unless you know the
        //data will almost always be less than 25. It is just 25 in our test app.
        public const int testBufferSize = 25;

        //This is the maximum number of asynchronous accept operations that can be 
        //posted simultaneously. This determines the size of the pool of 
        //SocketAsyncEventArgs objects that do accept operations. Note that this
        //is NOT the same as the maximum # of connections.
        public const int maxSimultaneousAcceptOps = 10;

        //For the BufferManager
        public const int opsToPreAlloc = 2;    // 1 for receive, 1 for send

        //allows excess SAEA objects in pool.
        public const int excessSaeaObjectsInPool = 1;
      
        //This is for logging during testing.        
        //You can change the path in the TestFileWriter class if you need to.
        public static ILog _log;

        static void Main(string[] args)
        {
            //Create a log file writer, so you can see the flow easily.
            //It can be printed. Makes it easier to figure out complex program flow.
            //The log StreamWriter uses a buffer. So it will only work right if you close
            //the server console properly at the end of the test.
            _log = new Log(consoleWatch);

            try
            {
                // Get endpoint for the listener.                
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(DEFAULT_SERVER), DEFAULT_PORT);

                WriteInfoToConsole(localEndPoint);
                //This object holds a lot of settings that we pass from Main method
                //to the SocketListener. In a real app, you might want to read
                //these settings from a database or windows registry settings that
                //you would create.
                ISocketListenerSettings theSocketListenerSettings = 
                    new SocketListenerSettings 
                    (maxNumberOfConnections, excessSaeaObjectsInPool, maxSimultaneousAcceptOps, testBufferSize, opsToPreAlloc, localEndPoint);


                SocketListener listener = new SocketListener(theSocketListenerSettings, _log);
                listener.Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), DEFAULT_PORT));

                _log.WriteLine("Server listening on port {0}. Press any key to terminate the server process..." + DEFAULT_PORT);
                Console.Read();

                _log.Close();
                listener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        //Start up setup
        public static void WriteInfoToConsole(IPEndPoint localEndPoint)
        {
            Console.WriteLine("The following options can be changed in Program.cs file.");
            Console.WriteLine("server buffer size = " + testBufferSize);
            Console.WriteLine("max connections = " + maxNumberOfConnections);
            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("local endpoint = " + IPAddress.Parse(((IPEndPoint)localEndPoint).Address.ToString()) + ": " + ((IPEndPoint)localEndPoint).Port.ToString());
            Console.WriteLine("server machine name = " + Environment.MachineName);

            Console.WriteLine();
            Console.WriteLine("Client and server should be on separate machines for best results.");
            Console.WriteLine("And your firewalls on both client and server will need to allow the connection.");
            Console.WriteLine();
        }
    }
}
