using System.Collections.Concurrent;
using Raft.Store.Domain;

namespace Raft.Store.Memory;

public class ClusterNodeStore: IClusterNodeStore
{
    private readonly ConcurrentDictionary<string, NodeAddress> _nodes = new();
    private readonly ConcurrentDictionary<string, int> _nextIndex = new();
    
    public string AddNode(string nodeName, NodeAddress nodeAddress)
    {
        _nodes.AddOrUpdate(nodeName, nodeAddress, (_, _) => nodeAddress);
        return $"{nodeName} added at {nodeAddress}";
    }

    public IEnumerable<NodeInfo> GetNodes()
    {
        return _nodes.Keys.Select(k => new NodeInfo(k, _nodes[k]));
    }

    public int GetNextIndex(string nodeName)
    {
        return _nextIndex.GetValueOrDefault(nodeName, 0);
    }

    public void IncreaseLastLogIndex(string nodeName, int entriesCount)
    {

        _nextIndex[nodeName] = GetNextIndex(nodeName) + entriesCount;
    }

    public void DecreaseLastLogIndex(string nodeName)
    {
        var nextIndex = _nextIndex[nodeName];
        _nextIndex[nodeName] = nextIndex - 1;
    }

    public override string ToString()
    {
        return string.Join(",", _nodes.Select(n => $"({n.Key}={n.Value})"));
    }
}