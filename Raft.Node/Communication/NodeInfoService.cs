using Grpc.Core;
using Raft.Store;

namespace Raft.Node.Communication;

public class NodeInfoService: NodeInfoSvc.NodeInfoSvcBase
{
    private readonly string _name;
    private readonly INodeStateStore _stateStore;

    private NodeInfoService(string name, INodeStateStore stateStore)
    {
        _name = name;
        _stateStore = stateStore;
    }
    
    public override Task<NodeInfoReply> GetInfo(NodeInfoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NodeInfoReply()
        {
            Address = context.Host,
            Name = _name,
            Role = _stateStore.Role.ToString(), 
            LeaderAddress = _stateStore.LeaderAddress.ToString() 
        });
    }
    
    public static ServerServiceDefinition GetServiceDefinition(string name, INodeStateStore stateStore)
    {
        return NodeInfoSvc.BindService(new NodeInfoService(name, stateStore));
    }
}