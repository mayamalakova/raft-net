using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Services.Cluster;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Node.Tests;

public class RequestVoteServiceTests
{
    private INodeStateStore _stateStore;
    private RequestVoteService _service;

    [SetUp]
    public void SetUp()
    {
        _stateStore = new NodeStateStore();
        _service = new RequestVoteService(_stateStore, Substitute.For<IRaftNode>());
    }

    [TestCase(NodeType.Follower)]
    [TestCase(NodeType.Candidate)]
    [TestCase(NodeType.Leader)]
    public void ShouldGrantVote_WhenTermIsHigher(NodeType role)
    {
        _stateStore.CurrentTerm = 1;
        _stateStore.Role = role;
        var request = new RequestForVoteMessage
        {
            Term = 2,
            CandidateId = "candidate1"
        };

        var reply = _service.RequestVote(request, Substitute.For<ServerCallContext>()).Result;

        reply.VoteGranted.ShouldBeTrue();
        reply.Term.ShouldBe(2);
        _stateStore.CurrentTerm.ShouldBe(2);
        _stateStore.VotedFor.ShouldBe("candidate1");
        _stateStore.LastVoteTerm.ShouldBe(2);
        _stateStore.Role.ShouldBe(role); //role should remain unchanged  
    }

    [Test]
    public void ShouldRejectVote_WhenTermIsLower()
    {
        _stateStore.CurrentTerm = 3;
        var request = new RequestForVoteMessage
        {
            Term = 2,
            CandidateId = "candidate1"
        };

        var reply = _service.RequestVote(request, Substitute.For<ServerCallContext>()).Result;

        reply.VoteGranted.ShouldBeFalse();
        reply.Term.ShouldBe(3);
        _stateStore.CurrentTerm.ShouldBe(3);
        _stateStore.VotedFor.ShouldBeNull();
    }

    [Test]
    public void ShouldRejectVote_WhenAlreadyVotedForSameTerm()
    {
        _stateStore.CurrentTerm = 2;
        _stateStore.VotedFor = "candidate1";
        _stateStore.LastVoteTerm = 2;
        
        var request = new RequestForVoteMessage
        {
            Term = 2,
            CandidateId = "candidate2"
        };

        var reply = _service.RequestVote(request, Substitute.For<ServerCallContext>()).Result;

        reply.VoteGranted.ShouldBeFalse();
        reply.Term.ShouldBe(2);
        _stateStore.VotedFor.ShouldBe("candidate1"); // Should not change
    }

    [Test]
    public void ShouldGrantVote_WhenSameTermAndNotVotedYet()
    {
        _stateStore.CurrentTerm = 2;
        var request = new RequestForVoteMessage
        {
            Term = 2,
            CandidateId = "candidate1"
        };

        var reply = _service.RequestVote(request, Substitute.For<ServerCallContext>()).Result;

        reply.VoteGranted.ShouldBeTrue();
        reply.Term.ShouldBe(2);
        _stateStore.VotedFor.ShouldBe("candidate1");
        _stateStore.LastVoteTerm.ShouldBe(2);
    }
} 