using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Communication.Services.Admin;

public class NodeInfoService(string name, NodeAddress nodeAddress, INodeStateStore stateStore, IClusterNodeStore nodeStore) : NodeInfoSvc.NodeInfoSvcBase, INodeService
{
    public override Task<NodeInfoReply> GetInfo(NodeInfoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NodeInfoReply
        {
            Address = nodeAddress.ToString(),
            Name = name,
            Role = stateStore.Role.ToString(), 
            LeaderAddress = stateStore.LeaderAddress?.ToString(),
            KnownNodes = nodeStore.ToString()
        });
    }
    
    public ServerServiceDefinition GetServiceDefinition()
    {
        return NodeInfoSvc.BindService(this);
    }
}