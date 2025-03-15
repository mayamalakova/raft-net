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

        var matchingIndexes = _clusterStore.GetMatchingIndexes();
        for (var current = _stateStore.LogLength - 1; current > _stateStore.CommitIndex; current--)
        {
            var termAtCurrent = _stateStore.GetTermAtIndex(current);
            if (termAtCurrent == _stateStore.CurrentTerm && matchingIndexes.Count(x => x >= current) > nodes.Length / 2)
            {
                _stateStore.CommitIndex = current;
                return;
            }
        }
    }
}


