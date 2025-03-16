using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class RegisterNodeService(INodeStateStore stateStore, IClusterNodeStore clusterStore, IClientPool clientPool) : RegisterNodeSvc.RegisterNodeSvcBase, INodeService
{
    public override Task<RegisterNodeRply> RegisterNode(RegisterNodeRequest request, ServerCallContext context)
    {
        if (stateStore.Role == NodeType.Leader)
        {
            UpdateClusterMembersAtFollowers(request);
        }
        
        var result = clusterStore.AddNode(request.Name, new NodeAddress(request.Host, request.Port));
        Log.Information($"Added node {request.Name}. Cluster members are {clusterStore}");
        return Task.FromResult(new RegisterNodeRply
        {
            Reply = result
        });
    }

    private void UpdateClusterMembersAtFollowers(RegisterNodeRequest request)
    {
        foreach (var node in clusterStore.GetNodes())
        {
            var registerNodeClient = clientPool.GetRegisterNodeClient(node.NodeAddress);
            registerNodeClient.RegisterNode(request);
        }
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return RegisterNodeSvc.BindService(this);
    }
}