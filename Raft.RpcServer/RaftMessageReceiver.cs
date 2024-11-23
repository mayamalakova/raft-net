using Grpc.Core;
using Raft.Communication.Contract;
using Shared;

namespace Raft;

public class RaftMessageReceiver : IRaftMessageReceiver
{
    private readonly Server _server;

    public RaftMessageReceiver(int port)
    {
        _server = new()
        {
            Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
        };
        
    }

    public void Start(IEnumerable<Svc.SvcBase> services)
    {
        foreach (var service in services)
        {
            _server.Services.Add(Svc.BindService(service));
        }
        _server.Start();
    }
}