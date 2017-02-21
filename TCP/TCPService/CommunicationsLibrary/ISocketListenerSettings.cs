using System.Net;

namespace CommunicationsLibrary
{
    public interface ISocketListenerSettings
    {
        int MaxConnections { get; }

        int NumberOfSaeaForRecSend { get; }

        int MaxAcceptOps { get; }

        int BufferSize { get; }

        int OpsToPreAllocate { get; }

        IPEndPoint LocalEndPoint { get; }
    }
}
