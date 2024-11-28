using Raft.Store.Domain;

namespace Raft.Store;

public interface IClusterNodeStore
{
    string AddNode(string nodeName, NodeAddress nodeAddress);
    IEnumerable<NodeInfo> GetNodes();
    int GetLastLogIndex(string nodeName);
    void IncreaseLastLogIndex(string nodeName, int entriesCount);
}