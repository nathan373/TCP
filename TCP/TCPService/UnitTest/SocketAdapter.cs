using System.Net.Sockets;

namespace UnitTest
{
    /// <summary>
	/// Adapter for System.Net.Sockets.Socket to implement ISocket.
	/// </summary>
	class SocketAdapter : ISocket
    {
        private Socket _socket;

        public SocketAdapter()
        {
            // create a "real" Socket with the needed parameters
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        #region ISocket Member (directly pass all calls to the real Socket)

        public void Connect(string host, int port)
        {
            _socket.Connect(host, port);
        }

        public void Close()
        {
            _socket.Close();
        }

        public int Receive(byte[] buffer, int size, SocketFlags flags)
        {
            return _socket.Receive(buffer);
        }

        public int Send(byte[] buffer, int size, SocketFlags flags)
        {
            return _socket.Send(buffer);
        }

        #endregion
    }
}
