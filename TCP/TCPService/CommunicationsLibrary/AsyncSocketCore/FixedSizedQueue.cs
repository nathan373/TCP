using System.Collections.Concurrent;

namespace CommunicationsLibrary.AsyncSocketCore
{
    internal class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        internal int Size { get; private set; }

        internal FixedSizedQueue(int size)
        {
            Size = size;
        }

        internal new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
