using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Services.Cluster;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Node.Tests;

public class ReplicationStateManagerTests
{
    private INodeStateStore _nodeStateStore;
    private IClusterNodeStore _clusterNodeStore;
    private ReplicationStateManager _replicationStateManager;

    [SetUp]
    public void SetUp()
    {
        _nodeStateStore = new NodeStateStore();
        _clusterNodeStore = Substitute.For<IClusterNodeStore>();
        _replicationStateManager = new ReplicationStateManager(_nodeStateStore, _clusterNodeStore);
    }

    private void PopulateLog(int[] terms)
    {
        foreach (var term in terms)
        {
            var command = new Command("a", CommandOperation.Assignment, 1);
            _nodeStateStore.AppendLogEntry(new LogEntry(command, term));
        }
    }

    [Test]
    public void ShouldUpdateState_WhenAllMatchingIndexesAgree()
    {
        PopulateLog([1, 1]);
        _nodeStateStore.CurrentTerm = 1;
        _clusterNodeStore.GetMatchingIndexes().Returns([1, 1]);
        
        _replicationStateManager.UpdateCommitIndex([
            new NodeInfo("node1", new NodeAddress("host1", 1)), 
            new NodeInfo("node2", new NodeAddress("host2", 2))
        ]);
        
        _nodeStateStore.CommitIndex.ShouldBe(1);
    }
    
    [Test]
    public void ShouldUpdateState_WhenMajorityMatchingIndexesAgree()
    {
        PopulateLog([1, 1, 1]);
        _nodeStateStore.CurrentTerm = 1;
        _clusterNodeStore.GetMatchingIndexes().Returns([1, 1, 2]);
        _replicationStateManager.UpdateCommitIndex([
            new NodeInfo("node1", new NodeAddress("some", 1)), 
            new NodeInfo("node2", new NodeAddress("some", 2)),
            new NodeInfo("node3", new NodeAddress("some", 3))
        ]);
        
        _nodeStateStore.CommitIndex.ShouldBe(1);
    }

    [Test]
    public void ShouldNotUpdateState_WhenMatchingIndexTermIsDifferent()
    {
        PopulateLog([1, 1]);
        _nodeStateStore.CurrentTerm = 2;
        _clusterNodeStore.GetMatchingIndex("node1").Returns(1);
        _clusterNodeStore.GetMatchingIndex("node2").Returns(1);
        _clusterNodeStore.GetMatchingIndex("node3").Returns(2);
        _replicationStateManager.UpdateCommitIndex([
            new NodeInfo("node1", new NodeAddress("some", 1)), 
            new NodeInfo("node2", new NodeAddress("some", 2)),
            new NodeInfo("node3", new NodeAddress("some", 3))
        ]);
        
        _nodeStateStore.CommitIndex.ShouldBe(-1);
    }
}