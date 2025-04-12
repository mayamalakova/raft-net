using NUnit.Framework;
using Raft.Cli;
using Raft.Node;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.IntegrationTests;

public class RaftNodeTests
{
    private readonly ICollection<IRaftNode> _nodes = new List<IRaftNode>();
    
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
    public void RaftNodesShouldFindLeaderAndAddItToKnownNodes()
    {
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);

        var followerClient1 = new RaftClient("localhost", 5002);

        var pingReply = followerClient1.Ping();
        pingReply.ShouldBe("{ \"reply\": \"Pong from follower1 at localhost:6002\" }");
        var followerInfo = followerClient1.Info();
        followerInfo.ShouldContain("\"leaderAddress\": \"localhost:5001\"");
        followerInfo.ShouldContain("knownNodes\": \"(follower2=localhost:5003),(leader1=localhost:5001)\"");

        var leaderRaftClient = new RaftClient("localhost", 5001);
        leaderRaftClient.Info().ShouldContain("\"knownNodes\": \"(follower1=localhost:5002),(follower2=localhost:5003)\"");
    }
    
    [Test]
    public void ShouldPopulateFollowerKnownNodesWhenRegistered()
    {
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);

        var followerClient2 = new RaftClient("localhost", 5003);

        var followerInfo = followerClient2.Info();

        followerInfo.ShouldContain("\"knownNodes\": \"(follower1=localhost:5002),(leader1=localhost:5001)\"");
    }
    
    [Test]
    public void ShouldUpdateKnownNodesOfAllExistingFollowersWhenNewNodeIsRegistered()
    {
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        
        followerClient1.Info().ShouldContain("\"knownNodes\": \"(leader1=localhost:5001)\"");
        
        CreateFollower("follower2", 5003, 5002);

        followerClient1.Info().ShouldContain("\"knownNodes\": \"(follower2=localhost:5003),(leader1=localhost:5001)\"");
    }

    [Test]
    public void ShouldAppendLogEntriesAtLeader()
    {
        var leader = CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);
        leader.GetNodeState().ShouldBe("commitIndex=-1, term=0, lastApplied=-1");

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        var followerClient2 = new RaftClient("localhost", 5003);

        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1)\" }");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
        leader.GetClusterState().ShouldBe("(follower1, 1),(follower2, 1)");

        leaderClient.Command(new CommandOptions { Var = "A", Operation = "+", Literal = 5 });
        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        leader.GetNodeState().ShouldBe("commitIndex=1, term=0, lastApplied=1");
        leader.GetClusterState().ShouldBe("(follower1, 2),(follower2, 2)");
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        followerClient2.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
    }

    [Test]
    public void ShouldNotUpdateNextLogIndexWhenUnableToConnect()
    {
        var leader = CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        var follower = CreateFollower("follower2", 5003, 5002);

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);

        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        leader.GetClusterState().ShouldBe("(follower1, 1),(follower2, 1)");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
        
        follower.Stop();
        _nodes.Remove(follower);
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "+", Literal = 5 });

        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
        leader.GetClusterState().ShouldBe("(follower1, 2),(follower2, 1)");
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
    }

    [Test]
    public void ShouldTurnToFollowerWhenGettingRequestFromNewerTerm()
    {
        var oldLeaderPort = 5000;
        var oldLeader = CreateLeader("nodeA", oldLeaderPort);
        var oldClient = new RaftClient("localhost", oldLeaderPort);
        oldClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });

        var newLeaderPort = 5001;
        var newLeader = CreateFollower("nodeB", newLeaderPort, oldLeaderPort);
        var newClient = new RaftClient("localhost", newLeaderPort);
        const int newTerm = 1;
        newLeader.BecomeLeader(newTerm);

        var reply = newClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 5 });
        reply.ShouldContain("Success at nodeB");
        var oldLeaderInfo = oldClient.Info();
        oldLeaderInfo.ShouldContain("\"role\": \"Follower\"");
        oldLeaderInfo.ShouldContain($"\"leaderAddress\": \"localhost:{newLeaderPort}\"");
        oldLeader.GetNodeState().ShouldContain($"term={newTerm}");
    }

    private IRaftNode CreateLeader(string name, int port)
    {
        var leader = new RaftNode(NodeType.Leader, name, port, "localhost", port, 1, 3);
        leader.Start();
        _nodes.Add(leader);
        return leader;
    }
    
    private IRaftNode CreateFollower(string name, int port, int peerPort)
    {
        var node = new RaftNode(NodeType.Follower, name, port, "localhost", peerPort, 1, 3);
        node.Start();
        _nodes.Add(node);
        return node;
    }
}