using Grpc.Core;
using Shared;

namespace Raft.Node.Communication;

public class RaftMessageReceiver(int port, string leaderHost, int leaderPort) : IRaftMessageReceiver {
    private readonly Server _server = new()
    {
        Services = { Svc.BindService(new MessageProcessingService(leaderHost, leaderPort)) },
        Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
    };

    public void Start()
    {
        _server.Start();
        
    }
}