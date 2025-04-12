using NUnit.Framework;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.Node.Tests;

public class LeaderTests
{
    private RaftNode _leader;

    [SetUp]
    public void SetUp()
    {
        _leader = new RaftNode(NodeType.Leader, "A", 1000, "localhost", 1001, 20, 2);
    }

    [Test]
    public void ShouldConvertToFollower()
    {
        var newLeader = new NodeInfo("B", new NodeAddress("localhost", 1002));
        _leader.BecomeFollower(newLeader, 1);
        
        _leader.StateStore.Role.ShouldBe(NodeType.Follower);
        _leader.StateStore.LeaderInfo.ShouldBe(newLeader);
    }
}