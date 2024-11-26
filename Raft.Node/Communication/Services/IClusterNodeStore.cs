using Raft.Store.Domain;

namespace Raft.Node.Communication.Services;

public interface IClusterNodeStore
{
    string AddNode(string nodeName, NodeAddress nodeAddress);
    IEnumerable<NodeAddress> GetNodes();
}