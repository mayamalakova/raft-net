using Grpc.Core;
using Raft.Communication.Contract;

namespace Raft;

public class ControlMessageReceiver(int port, INodeService controlService): IRaftMessageReceiver
{
    private readonly Server _server = new()
    {
        Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
    };

    public void Start()
    {
        _server.Services.Add(controlService.GetServiceDefinition());
        _server.Start();
    }

    public void Stop()
    {
        _server.ShutdownAsync().Wait();
    }

    public void DisconnectFromCluster()
    {
        throw new NotImplementedException();
    }

    public void ReconnectToCluster()
    {
        throw new NotImplementedException();
    }
}
