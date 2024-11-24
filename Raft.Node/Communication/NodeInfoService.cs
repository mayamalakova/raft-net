using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;

namespace Raft.Node.Communication;

public class NodeInfoService(string name, INodeStateStore stateStore) : NodeInfoSvc.NodeInfoSvcBase, INodeService
{
    public override Task<NodeInfoReply> GetInfo(NodeInfoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NodeInfoReply()
        {
            Address = context.Host,
            Name = name,
            Role = stateStore.Role.ToString(), 
            LeaderAddress = stateStore.LeaderAddress.ToString() 
        });
    }
    
    public ServerServiceDefinition GetServiceDefinition()
    {
        return NodeInfoSvc.BindService(this);
    }
}