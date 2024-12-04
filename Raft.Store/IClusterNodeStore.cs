using Raft.Store.Domain;

namespace Raft.Store;

public interface IClusterNodeStore
{
    string AddNode(string nodeName, NodeAddress nodeAddress);
    IEnumerable<NodeInfo> GetNodes();
    int GetNextIndex(string nodeName);
    void IncreaseNextLogIndex(string nodeName, int entriesCount);
    void DecreaseNextLogIndex(string nodeName);
    void IncreaseMatchingIndex(string nodeName, int entriesCount);
    void DecreaseMatchingIndex(string nodeName);
    string GetNextIndexesPrintable();
}