using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services.Cluster;
using Raft.Node.Tests.MockHelpers;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Node.Tests;

[TestFixture]
public class LogReplicatorTests
{
    private INodeStateStore _nodeStore;
    private IClientPool _clientPool;
    private IClusterNodeStore _clusterStore;
    private IAppendEntriesRequestFactory _appendEntriesRequestFactory;
    private LogReplicator _logReplicator;

    [SetUp]
    public void SetUp()
    {
        _nodeStore = Substitute.For<INodeStateStore>();
        _clientPool = Substitute.For<IClientPool>();
        _clusterStore = Substitute.For<IClusterNodeStore>();
        _appendEntriesRequestFactory = Substitute.For<IAppendEntriesRequestFactory>();
        _logReplicator = new LogReplicator(_nodeStore, _clientPool, _clusterStore, "lead1", 2)
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };
    }
    
    [Test]
    public void LeaderShouldIncreaseNextLogIndexWhenAppendEntryReturnsSuccess()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _clusterStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        _clusterStore.GetNextIndex(nodeName).Returns(0);
        _clusterStore.IncreaseNextLogIndex(nodeName, 1).Returns(1);
        _nodeStore.GetLastEntries(1)
            .Returns([new LogEntry(new Command("A", CommandOperation.Assignment, 1), 0)]);
        _nodeStore.LogLength.Returns(1);
        SetUpMockAppendEntriesClient(followerAddress);

        _logReplicator.ReplicateToFollowers();

        _clusterStore.Received().IncreaseNextLogIndex(nodeName, 1);
        _clusterStore.Received().SetMatchingIndex(nodeName, 0);
        _clusterStore.DidNotReceive().DecreaseNextLogIndex(Arg.Any<string>());
    }

    [Test]
    public void ShouldUpdateCommitIndexAfterMajoritySuccess()
    {
        var nodeStore = new NodeStateStore
        {
            Role = NodeType.Leader, CommitIndex = -1, CurrentTerm = 0
        };
        nodeStore.AppendLogEntry(new LogEntry(new Command("A", CommandOperation.Assignment, 1), 0));
        var clusterStore = new ClusterNodeStore();
        var followerAddress = new NodeAddress("host", 199);
        clusterStore.AddNode("fol1", followerAddress);
        var replicator = new LogReplicator(nodeStore, _clientPool, clusterStore, "lead1", 2)
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };
        SetUpMockAppendEntriesClient(followerAddress);
        
        replicator.ReplicateToFollowers();
        
        nodeStore.CommitIndex.ShouldBe(0);
    }

    [Test]
    public void ShouldNotUpdateCommitIndexWithoutMajoritySuccess()
    {
        var nodeStore = new NodeStateStore
        {
            Role = NodeType.Leader, CommitIndex = -1, CurrentTerm = 0
        };
        nodeStore.AppendLogEntry(new LogEntry(new Command("A", CommandOperation.Assignment, 1), 0));
        var clusterStore = new ClusterNodeStore();
        var follower1 = new NodeAddress("host", 199);
        var follower2 = new NodeAddress("host", 299);
        clusterStore.AddNode("fol1", follower1);
        clusterStore.AddNode("fol2", follower2);
        SetUpMockAppendEntriesClient(follower1);
        SetUpMockAppendEntriesClient(follower2, false);
        var replicator = new LogReplicator(nodeStore, _clientPool, clusterStore, "lead1", 2)
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };

        replicator.ReplicateToFollowers();
        
        nodeStore.CommitIndex.ShouldBe(-1);
    }

    [Test]
    public void LeaderShouldNotDecreaseIndexWhenAppendEntryWithNoEntriesReturnsSuccess()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _clusterStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        _clusterStore.GetNextIndex(nodeName).Returns(0);
        _clusterStore.IncreaseNextLogIndex(nodeName, 0).Returns(0);
        _nodeStore.LogLength.Returns(0);
        SetUpMockAppendEntriesClient(followerAddress);

        _logReplicator.ReplicateToFollowers();

        _clusterStore.Received().IncreaseNextLogIndex(nodeName, 0);
        _clusterStore.Received().SetMatchingIndex(nodeName, -1);
        _clusterStore.DidNotReceive().DecreaseNextLogIndex(Arg.Any<string>());
    }

    [Test]
    public void LeaderShouldDecreaseNextLogIndexWhenAppendEntryReturnsFailure()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _clusterStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        SetUpMockAppendEntriesClient(followerAddress, false);
        _nodeStore.GetLastEntries(1)
            .Returns([new LogEntry(new Command("A", CommandOperation.Assignment, 5), 0)]);

        _logReplicator.ReplicateToFollowers();

        _clusterStore.DidNotReceive().IncreaseNextLogIndex(nodeName, Arg.Any<int>());
        _clusterStore.DidNotReceive().SetMatchingIndex(nodeName, Arg.Any<int>());
        _clusterStore.Received().DecreaseNextLogIndex(nodeName);
    }

    [TestCase(0, 0, true)]
    [TestCase(1, 0, false)]
    [TestCase(1, 1, true)]
    public void ShouldReplyIfReplicationToNodeIsComplete(int logLength, int nextIndex, bool expectedResult)
    {
        _nodeStore.LogLength.Returns(logLength);
        _clusterStore.GetNextIndex("someNode").Returns(nextIndex);
        _logReplicator.IsReplicationComplete("someNode").ShouldBe(expectedResult);
    }

    private void SetUpMockAppendEntriesClient(NodeAddress followerAddress,
        bool success = true)
    {
        var mockFollowerClient = Substitute.For<AppendEntriesSvc.AppendEntriesSvcClient>();
        var appendEntriesReply = new AppendEntriesReply()
        {
            Success = success
        };
        mockFollowerClient.AppendEntriesAsync(Arg.Any<AppendEntriesRequest>(), Arg.Any<CallOptions>())
            .Returns(ClientMockHelpers.CreateAsyncUnaryCall(appendEntriesReply));
        _clientPool.GetAppendEntriesClient(followerAddress).Returns(mockFollowerClient);
    }
}