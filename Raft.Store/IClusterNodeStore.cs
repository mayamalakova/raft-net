using Raft.Store.Domain;

namespace Raft.Store;

public interface IClusterNodeStore
{
    string AddNode(string nodeName, NodeAddress nodeAddress);
    IEnumerable<NodeInfo> GetNodes();
    int GetNextIndex(string nodeName);
    int IncreaseNextLogIndex(string nodeName, int entriesCount);
    void DecreaseNextLogIndex(string nodeName);
    void SetMatchingIndex(string nodeName, int newMatchingIndex);
    string GetNextIndexesPrintable();
}