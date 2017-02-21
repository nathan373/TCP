using System.Net.Sockets;

namespace UnitTest
{
    public interface ISocket
    {
        void Connect(string host, int port);
        void Close();
        int Receive(byte[] buffer, int size, SocketFlags flags);
        int Send(byte[] buffer, int size, SocketFlags flags);
    }
}
