using Raft.Store.Domain;

namespace Raft.Store;

public interface INodeStateStore
{
    NodeType Role { get; set; }
    NodeAddress LeaderAddress { get; set; }
}