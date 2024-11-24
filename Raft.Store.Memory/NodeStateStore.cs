using Raft.Store.Domain;

namespace Raft.Store.Memory;

public class NodeStateStore : INodeStateStore
{
    public NodeType Role { get; set; }
    public NodeAddress LeaderAddress { get; set; }
}