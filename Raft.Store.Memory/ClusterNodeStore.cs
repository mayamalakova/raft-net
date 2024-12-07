using System.Collections.Concurrent;
using Raft.Store.Domain;

namespace Raft.Store.Memory;

public class ClusterNodeStore: IClusterNodeStore
{
    private readonly ConcurrentDictionary<string, NodeAddress> _nodes = new();
    private readonly ConcurrentDictionary<string, int> _nextIndex = new();
    private readonly ConcurrentDictionary<string, int> _matchingIndex = new();
    
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

    public int IncreaseNextLogIndex(string nodeName, int entriesCount)
    {
        var newValue = GetNextIndex(nodeName) + entriesCount;
        _nextIndex[nodeName] = newValue;
        return newValue;
    }
    
    public void SetMatchingIndex(string nodeName, int newMatchingIndex)
    {
        _matchingIndex[nodeName] = newMatchingIndex;
    }

    public void DecreaseNextLogIndex(string nodeName)
    {
        var nextIndex = GetNextIndex(nodeName);
        _nextIndex[nodeName] = Math.Max(0, nextIndex - 1);
    }

    public string GetNextIndexesPrintable()
    {
        var items = _nextIndex.Select(k => $"{k.Key}: {k.Value}");
        return string.Join(',', items);
    }

    public override string ToString()
    {
        return string.Join(",", _nodes.Select(n => $"({n.Key}={n.Value})"));
    }

    public int GetMatchingIndex(string nodeName)
    {
        return _matchingIndex.GetValueOrDefault(nodeName, -1);
    }
}