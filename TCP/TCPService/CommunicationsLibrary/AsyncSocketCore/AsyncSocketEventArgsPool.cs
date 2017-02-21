using System;
using System.Net.Sockets;
using System.Threading;

namespace CommunicationsLibrary.AsyncSocketCore
{
    internal sealed class AsyncSocketEventArgsPool
    {
        #region Private Members
        //just for assigning an ID so we can watch our objects while testing.
        private int nextTokenId = 0;
        private ILog _log;
        // Pool of reusable SocketAsyncEventArgs objects.     
        private FixedSizedQueue<SocketAsyncEventArgs> queue;
        #endregion
        // initializes the object pool
        internal AsyncSocketEventArgsPool(int capacity, ILog log)
        {
            this.queue = new FixedSizedQueue<SocketAsyncEventArgs>(capacity);
            this._log = log;
        }

        // The number of SocketAsyncEventArgs instances in the pool.         
        internal int Count
        {
            get { return this.queue.Count; }
        }

        internal int AssignTokenId()
        {
            int tokenId = Interlocked.Increment(ref nextTokenId);
            return tokenId;
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        internal SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs args;
            if (this.queue.TryDequeue(out args))
            {
                return args;
            }
            return null;
        }

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        internal void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                _log.WriteLine("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            this.queue.Enqueue(item);
        }
    }
}
