namespace Raft.Node;

public interface INodeStateStore
{
    NodeType Role { get; set; }
    NodeAddress LeaderAddress { get; set; }
}

public class NodeStateStore : INodeStateStore
{
    public NodeType Role { get; set; }
    public NodeAddress LeaderAddress { get; set; }
}