using Raft.Node.Communication.Services.Cluster;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node;

public class RaftLeaderService(LogReplicator logReplicator, ReplicationStateManager replicationStateManager, 
    INodeStateStore stateStore, IClusterNodeStore clusterStore)
{
    public State ReconcileCluster()
    {
        logReplicator.ReplicateToFollowers();
        replicationStateManager.UpdateCommitIndex(clusterStore.GetNodes().ToArray());
        var newState = stateStore.ApplyCommitted();
        return newState;
    }
}