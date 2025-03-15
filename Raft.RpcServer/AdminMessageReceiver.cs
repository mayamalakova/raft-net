using Grpc.Core;
using Raft.Communication.Contract;

namespace Raft;

/// <summary>
/// Processes messages from the admin client
/// </summary>
/// <param name="port"></param>
/// <param name="services"></param>
public class AdminMessageReceiver(int port, IEnumerable<INodeService> services): IMessageReceiver
{
    private readonly Server _server = new()
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

}
