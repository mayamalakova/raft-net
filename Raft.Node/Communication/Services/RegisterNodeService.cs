using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store.Domain;

namespace Raft.Node.Communication.Services;

public class RegisterNodeService(IClusterNodeStore nodeStore) : RegisterNodeSvc.RegisterNodeSvcBase, INodeService
{
    public override Task<RegisterNodeRply> RegisterNode(RegisterNodeRequest request, ServerCallContext context)
    {
        var result = nodeStore.AddNode(request.Name, new NodeAddress(request.Host, request.Port));
        Console.WriteLine($"Added node {request.Name}. Cluster members are {nodeStore}");
        return Task.FromResult(new RegisterNodeRply
        {
            Reply = result
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return RegisterNodeSvc.BindService(this);
    }
}