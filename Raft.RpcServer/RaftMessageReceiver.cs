using Grpc.Core;
using Raft.Communication.Contract;

namespace Raft;

public class RaftMessageReceiver(int port) : IRaftMessageReceiver
{
    private readonly Server _server = new()
    {
        Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
    };

    public void Start(IEnumerable<ServerServiceDefinition> services)
    {
        foreach (var service in services)
        {
            _server.Services.Add(service);
        }
        _server.Start();
    }
}