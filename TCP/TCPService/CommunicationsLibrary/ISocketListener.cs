using System.Net;

namespace CommunicationsLibrary
{
    public interface ISocketListener
    {
        void Start(IPEndPoint localEndPoint);
        void Stop();
    }
}
