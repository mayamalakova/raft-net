using NSubstitute;
using NUnit.Framework;
using Raft.Node.Election;
using Raft.Node.Tests.MockHelpers;
using Shouldly;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Raft.Node.Tests;

[TestFixture]
public class LeaderPresenceTrackerTests
{
    private IRaftNode _mockNode;
    private LeaderPresenceTracker _tracker;
    private MockTimer _mockTimer;

    [SetUp]
    public void Setup()
    {
        _mockNode = Substitute.For<IRaftNode>();
        _mockTimer = new MockTimer();
        _tracker = new LeaderPresenceTracker(_mockNode, new MockTimerFactory(_mockTimer));
    }

    [Test]
    public void ShouldStartPresenceTimer()
    {
        _tracker.Start();

        _tracker.IsStarted().ShouldBeTrue();
    }

    [Test]
    public void ShouldNotRestartTimerWhenAlreadyStarted()
    {
        var firstInterval = _tracker.Start();
        var secondInterval = _tracker.Start();

        firstInterval.ShouldBe(secondInterval);
    }

    [Test]
    public void ShouldStopTimer()
    {
        _tracker.Start();
        _tracker.Stop();
        
        _mockTimer.SimulateElapsed();

        _mockTimer.Enabled.ShouldBeFalse();
        _mockNode.DidNotReceive().BecomeCandidate();
    }

    [Test]
    public void Reset_RestartTimerWithNewInterval()
    {
        var firstInterval = _tracker.Start();
        Thread.Sleep(10);

        _tracker.Reset();
        _mockTimer.Enabled.ShouldBeTrue();
        _mockTimer.Interval.ShouldBe(firstInterval);
    }

    [Test]
    public void ShouldBecomeCandidateWhenTimeExpires()
    {
        _tracker.Start();
        
        _mockTimer.SimulateElapsed();
        
        _mockNode.Received(1).BecomeCandidate();
    }

    [TearDown]
    public void Cleanup()
    {
        _tracker.Stop();
    }
}