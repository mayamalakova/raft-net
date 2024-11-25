using Raft.Store.Domain;

namespace Raft.Store;

public interface INodeStateStore
{
    // ReSharper disable once UnusedMemberInSuper.Global
    NodeType Role { get; set; }
    NodeAddress? LeaderAddress { get; set; }
}