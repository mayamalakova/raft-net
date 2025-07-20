using NSubstitute;
using NUnit.Framework;
using Raft.Node.Election;
using Raft.Shared.Timing;
using Raft.Store.Domain;
using Shouldly;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Raft.Node.Tests;

[TestFixture]
public class RaftNodeTimerTests
{
    private RaftNode _node;
    private MockTimerFactory _mockTimerFactory;
    private MockTimer _mockTimer;

    [SetUp]
    public void Setup()
    {
        _mockTimer = new MockTimer();
        _mockTimerFactory = new MockTimerFactory(_mockTimer);

        _node = new RaftNode(
            role: NodeType.Follower,
            nodeName: "TestNode",
            port: 5000,
            clusterHost: "localhost",
            clusterPort: 5001,
            replicationTimeoutSeconds: 5,
            heartBeatIntervalSeconds: 1,
            timerFactory: _mockTimerFactory
        )
        {
            ElectionManager = Substitute.For<IElectionManager>()
        };
    }

    [Test]
    public void ShouldStopLeaderTracker_WhenFollowerBecomesLeader()
    {
        _node.BecomeFollower(SomeLeader(), 1);
        _node.BecomeLeader(2);

        _mockTimer.Enabled.ShouldBeFalse();
    }

    [Test]
    public void ShouldStartLeaderTracker_WhenNodeIsFollower()
    {
        _node.BecomeFollower(SomeLeader(), 1);

        _mockTimer.Enabled.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotStartLeaderTracker_WhenNodeIsLeader()
    {
        _node.BecomeLeader(1);

        _mockTimer.Enabled.ShouldBeFalse();
    }

    [Test]
    public void ShouldBecomeCandidateAndStopTracker_WhenFollowerTimerExpires()
    {
        _node.BecomeFollower(SomeLeader(), 1);

        _mockTimer.SimulateElapsed();

        _node.StateStore.Role.ShouldBe(NodeType.Candidate);
        _mockTimer.Enabled.ShouldBeFalse();
    }

    private static NodeInfo SomeLeader()
    {
        return new NodeInfo("leader1", new NodeAddress("localhost", 5001));
    }

    [TearDown]
    public void Cleanup()
    {
        _node.Stop();
    }
}