using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Communication.Services.Cluster;

public class ReplicationStateManager
{
    private readonly INodeStateStore _stateStore;
    private readonly IClusterNodeStore _clusterStore;

    public ReplicationStateManager(INodeStateStore stateStore, IClusterNodeStore clusterStore)
    {
        _stateStore = stateStore;
        _clusterStore = clusterStore;
    }

    public void UpdateCommitIndex(NodeInfo[] nodes)
    {
        if (nodes.Length == 0)
        {
            _stateStore.CommitIndex = _stateStore.LogLength - 1;
            return;
        }
        var matchingIndexes = nodes.Select(x => _clusterStore.GetMatchingIndex(x.NodeName)).ToArray();
        var current = _stateStore.LogLength;
        while (current > _stateStore.CommitIndex)
        {
            var next = current - 1;
            var termAtNext = _stateStore.GetTermAtIndex(next);
            if (termAtNext == _stateStore.CurrentTerm && matchingIndexes.Count(x => x >= next) > nodes.Length / 2)
            {
                _stateStore.CommitIndex = next;
                return;
            }
            current = next;
        }
    }

    private int MatchingCountAtIndex(int[] matchingIndexes, int current)
    {
        return matchingIndexes.Count(x => x >= current);
    }
}