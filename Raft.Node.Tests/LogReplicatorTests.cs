using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services;
using Raft.Node.Tests.MockHelpers;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Shouldly;

namespace Raft.Node.Tests;

[TestFixture]
public class LogReplicatorTests
{
    private INodeStateStore _mockStateStore;
    private IClientPool _clientPool;
    private IClusterNodeStore _nodeStore;
    private IAppendEntriesRequestFactory _appendEntriesRequestFactory;
    private LogReplicator _logReplicator;

    [SetUp]
    public void SetUp()
    {
        _mockStateStore = Substitute.For<INodeStateStore>();
        _clientPool = Substitute.For<IClientPool>();
        _nodeStore = Substitute.For<IClusterNodeStore>();
        _appendEntriesRequestFactory = Substitute.For<IAppendEntriesRequestFactory>();
        _logReplicator = new LogReplicator(_mockStateStore, _clientPool, _nodeStore, "lead1", 2)
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };
    }
    
    [Test]
    public void LeaderShouldIncreaseNextLogIndexWhenAppendEntryReturnsSuccess()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _nodeStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        _nodeStore.GetNextIndex(nodeName).Returns(0);
        _mockStateStore.GetLastEntries(1)
            .Returns([new LogEntry(new Command("A", CommandOperation.Assignment, 1), 0)]);
        _mockStateStore.LogLength.Returns(1);
        SetUpMockAppendEntriesClient(followerAddress);

        _logReplicator.ReplicateToFollowers();

        _nodeStore.Received().IncreaseLastLogIndex(nodeName, 1);
        _nodeStore.DidNotReceive().DecreaseLastLogIndex(Arg.Any<string>());
    }
    
    [Test]
    public void LeaderShouldNotDecreaseIndexWhenAppendEntryWithNoEntriesReturnsSuccess()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _nodeStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        _nodeStore.GetNextIndex(nodeName).Returns(0);
        _mockStateStore.LogLength.Returns(0);
        SetUpMockAppendEntriesClient(followerAddress);

        _logReplicator.ReplicateToFollowers();

        _nodeStore.Received().IncreaseLastLogIndex(nodeName, 0);
        _nodeStore.DidNotReceive().DecreaseLastLogIndex(Arg.Any<string>());
    }

    [Test]
    public void LeaderShouldDecreaseNextLogIndexWhenAppendEntryReturnsFailure()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        var nodeName = "someNode";
        _nodeStore.GetNodes().Returns([new NodeInfo(nodeName, followerAddress)]);
        SetUpMockAppendEntriesClient(followerAddress, false);
        _mockStateStore.GetLastEntries(1)
            .Returns([new LogEntry(new Command("A", CommandOperation.Assignment, 5), 0)]);

        _logReplicator.ReplicateToFollowers();

        _nodeStore.DidNotReceive().IncreaseLastLogIndex(nodeName, 1);
        _nodeStore.Received().DecreaseLastLogIndex(nodeName);
    }

    [TestCase(0, 0, true)]
    [TestCase(1, 0, false)]
    [TestCase(1, 1, true)]
    public void ShouldReplyIfReplicationToNodeIsComplete(int logLength, int nextIndex, bool expectedResult)
    {
        _mockStateStore.LogLength.Returns(logLength);
        _nodeStore.GetNextIndex("someNode").Returns(nextIndex);
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