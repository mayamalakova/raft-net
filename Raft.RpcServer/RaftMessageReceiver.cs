using Grpc.Core;
using Raft.Communication.Contract;

namespace Raft;

public class RaftMessageReceiver(int port, IEnumerable<INodeService> services) : IRaftMessageReceiver
{
    private Server _server = new()
    {
        Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
    };
    
    public void Start()
    {
        foreach (var service in services)
        {
            _server.Services.Add(service.GetServiceDefinition());
        }
        _server.Start();
    }

    public void Stop()
    {
        _server.ShutdownAsync().Wait();
    }

    public void DisconnectFromCluster()
    {
        Stop();
    }

    public void ReconnectToCluster()
    {
        _server = new Server
        {
            Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
        };
        Start();
    }
}