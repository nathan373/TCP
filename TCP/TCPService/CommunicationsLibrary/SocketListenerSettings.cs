using System;
using System.Net;

namespace CommunicationsLibrary
{
    public class SocketListenerSettings : ISocketListenerSettings
    {
        // the maximum number of connections the sample is designed to handle simultaneously 
        private int maxConnections;

        // this variable allows us to create some extra SAEA objects for the pool,
        // if we wish.
        private int numberOfSaeaForRecSend;
        
        // tells us how many objects to put in pool for accept operations
        private int maxSimultaneousAcceptOps;

        // buffer size to use for each socket receive operation
        private int receiveBufferSize;
        
        // See comments in buffer manager.
        private int opsToPreAllocate;

        // Endpoint for the listener.
        private IPEndPoint localEndPoint;

        public SocketListenerSettings(int maxConnections, int excessSaeaObjectsInPool, int maxSimultaneousAcceptOps, int receiveBufferSize, int opsToPreAlloc, IPEndPoint theLocalEndPoint)
        {
            this.maxConnections = maxConnections;
            this.numberOfSaeaForRecSend = maxConnections + excessSaeaObjectsInPool;
            this.maxSimultaneousAcceptOps = maxSimultaneousAcceptOps;            
            this.receiveBufferSize = receiveBufferSize;          
            this.opsToPreAllocate = opsToPreAlloc;
            this.localEndPoint = theLocalEndPoint;
        }

        public int MaxConnections
        {
            get
            {
                return this.maxConnections;
            }
        }
        public int NumberOfSaeaForRecSend
        {
            get
            {
                return this.numberOfSaeaForRecSend;
            }
        }

        public int MaxAcceptOps
        {
            get
            {
                return this.maxSimultaneousAcceptOps;
            }
        }
    
        public int BufferSize
        {
            get
            {
                return this.receiveBufferSize;
            }
        }
       
        public int OpsToPreAllocate
        {
            get
            {
                return this.opsToPreAllocate;
            }
        }
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return this.localEndPoint;
            }
        }
    }
}
