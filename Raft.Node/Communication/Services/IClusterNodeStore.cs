using Raft.Store.Domain;

namespace Raft.Node.Communication.Services;

public interface IClusterNodeStore
{
    string AddNode(string nodeName, NodeAddress nodeAddress);
    IEnumerable<NodeInfo> GetNodes();
    int GetLastLogIndex(string nodeName);
}

public record NodeInfo(string NodeName, NodeAddress NodeAddress);