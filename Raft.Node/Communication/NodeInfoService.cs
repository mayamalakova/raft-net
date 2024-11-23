using Grpc.Core;

namespace Raft.Node.Communication;

public class NodeInfoService: NodeInfoSvc.NodeInfoSvcBase
{
    private readonly string _name;
    private readonly int _port;
    private readonly NodeType _role;

    private NodeInfoService(string name, NodeType role)
    {
        _name = name;
        _role = role;
    }
    
    public override Task<NodeInfoReply> GetInfo(NodeInfoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NodeInfoReply()
        {
            Address = context.Host,
            Role = _role.ToString(), //TODO this will be transient, we need to be able to update it
            Name = _name,
            LeaderAddress = "?" //TODO this will be transient, we need to be able to update it
        });
    }
    
    public static ServerServiceDefinition GetServiceDefinition(string name, NodeType role)
    {
        return NodeInfoSvc.BindService(new NodeInfoService(name, role));
    }
}