using Grpc.Core;
using Shared;

namespace Raft;

public class RaftGrpcServer
{
    private readonly int _port;

    public RaftGrpcServer(int port)
    {
        _port = port;
    }

    public void Init()
    {
        var server = new Server
        {
            Services = { Svc.BindService(new MyService()) },
            Ports = { new ServerPort("0.0.0.0", _port, ServerCredentials.Insecure) }
        };
        server.Start();
    }
}