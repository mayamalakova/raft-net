using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Client;
using Raft.Node.Election;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Tests;

[TestFixture]
public class ElectionManagerTests
{
    private ElectionManager _electionManager;
    private IClusterNodeStore _clusterStore;
    private IClientPool _clientPool;
    private IElectionResultsReceiver _resultsReceiver;

    [SetUp]
    public void SetUp()
    {
        _clusterStore = Substitute.For<IClusterNodeStore>();
        _clientPool = Substitute.For<IClientPool>();
        _resultsReceiver = Substitute.For<IElectionResultsReceiver>();
    }

    [Test]
    public async Task ShouldCallOnElectionWon_WhenMajorityVotesGranted()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeInfo("node1", new NodeAddress("localhost", 5002)),
            new NodeInfo("node2", new NodeAddress("localhost", 5003))
        };
        _clusterStore.GetNodes().Returns(nodes);
        SetupVoteClientMock(new RequestForVoteReply { Term = 1, VoteGranted = true });
        _electionManager = new ElectionManager("nodeA", _clusterStore, _clientPool, _resultsReceiver);

        // Act
        await _electionManager.StartElectionAsync(1);

        // Assert
        _resultsReceiver.Received(1).OnElectionWon(1);
    }

    [Test]
    public async Task ShouldCallOnElectionLost_WhenNotEnoughVotes()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeInfo("node1", new NodeAddress("localhost", 5002)),
            new NodeInfo("node2", new NodeAddress("localhost", 5003))
        };
        _clusterStore.GetNodes().Returns(nodes);
        SetupVoteClientMock(new RequestForVoteReply { Term = 1, VoteGranted = false });
        _electionManager = new ElectionManager("nodeA", _clusterStore, _clientPool, _resultsReceiver);

        // Act
        await _electionManager.StartElectionAsync(1);

        // Assert
        _resultsReceiver.Received(1).OnElectionLost(1);
    }

    [Test]
    public async Task ShouldCallOnHigherTermReceivedWithVoteReply_WhenHigherTermSeen()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeInfo("node1", new NodeAddress("localhost", 5002))
        };
        _clusterStore.GetNodes().Returns(nodes);
        SetupVoteClientMock(new RequestForVoteReply { Term = 2, VoteGranted = false });
        _electionManager = new ElectionManager("nodeA", _clusterStore, _clientPool, _resultsReceiver);

        // Act
        await _electionManager.StartElectionAsync(1);

        // Assert
        _resultsReceiver.Received(1).OnHigherTermReceivedWithVoteReply(2);
    }

    private void SetupVoteClientMock(RequestForVoteReply voteReply)
    {
        var mockVoteClient = Substitute.For<RequestForVoteSvc.RequestForVoteSvcClient>();
        mockVoteClient.RequestVoteAsync(Arg.Any<RequestForVoteMessage>())
            .Returns(Task.FromResult(voteReply).ToAsyncUnaryCall());
        _clientPool.GetRequestForVoteClient(Arg.Any<NodeAddress>()).Returns(mockVoteClient);
    }
}

public static class TaskExtensions
{
    public static Grpc.Core.AsyncUnaryCall<T> ToAsyncUnaryCall<T>(this Task<T> task)
    {
        return new Grpc.Core.AsyncUnaryCall<T>(
            task,
            Task.FromResult(new Grpc.Core.Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => new Grpc.Core.Metadata(),
            () => { }
        );
    }
}