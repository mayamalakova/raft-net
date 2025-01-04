using NUnit.Framework;
using Raft.Cli;
using Raft.Node;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.IntegrationTests;

public class RaftLogReplicationTests
{
    private readonly ICollection<RaftNode> _nodes = new List<RaftNode>();
    
    [TearDown]
    public void TearDown()
    {
        foreach (var node in _nodes)
        {
            node.Stop();
        }
        _nodes.Clear();
    }
    
    [Test]
    public void ShouldReconcileFollowerLogAfterReconnecting()
    {
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        var follower2 = CreateFollower("follower2", 5003, 5002);

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        var followerClient2 = new RaftClient("localhost", 5003);
        
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1)\" }");
        followerClient2.LogInfo().ShouldBe( "{ \"entries\": \"(A=1)\" }");

        DisconnectNode(followerClient2, follower2);
        leaderClient.Command(new CommandOptions { Var = "B", Operation = "=", Literal = 1 });
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (B=1)\" }");
        followerClient2.LogInfo().ShouldBe( "{ \"entries\": \"(A=1)\" }");

        ReconnectNode(followerClient2, follower2);
        Task.Delay(4000).Wait();
        followerClient2.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (B=1)\" }");
    }

    [Test]
    public void ShouldApplyCommittedEntriesAfterReplication()
    {
        var leader = CreateLeader("leader1", 5001);
        var leaderClient = new RaftClient("localhost", 5001);

        const int someLiteral = 5;
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = someLiteral });
        
        leaderClient.GetState().ShouldBe($"value: {someLiteral}, errors: [ ]");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
    }

    private void ReconnectNode(RaftClient followerClient2, RaftNode follower2)
    {
        followerClient2.Reconnect();
        _nodes.Add(follower2);
    }

    private void DisconnectNode(RaftClient followerClient2, RaftNode follower2)
    {
        followerClient2.Disconnect();
        _nodes.Remove(follower2);
    }

    private RaftNode CreateLeader(string name, int port)
    {
        var leader = new RaftNode(NodeType.Leader, name, port, "localhost", port, 1);
        leader.Start();
        _nodes.Add(leader);
        return leader;
    }
    
    private RaftNode CreateFollower(string name, int port, int peerPort)
    {
        var node = new RaftNode(NodeType.Follower, name, port, "localhost", peerPort, 1);
        node.Start();
        _nodes.Add(node);
        return node;
    }
}