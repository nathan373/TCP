using System;
using System.Net.Sockets;

namespace CommunicationsLibrary.AsyncSocketCore
{
    internal sealed class MessageData
    {
        internal AsyncSocketUserToken Token;
        internal byte[] Message;
    }

    internal sealed class AsyncSocketUserToken : IDisposable
    {
        internal Socket Socket { get; private set; }
        internal int? MessageSize { get; set; }
        internal int DataStartOffset { get; set; }
        internal int NextReceiveOffset { get; set; }

        internal AsyncSocketUserToken(Socket socket)
        {
            this.Socket = socket;
        }

        #region IDisposable Members

        public void Dispose()
        {
           this.Socket.Shutdown(SocketShutdown.Both);
           this.Socket.Close();
        }

        #endregion
    }
}
