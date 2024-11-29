using NUnit.Framework;
using Raft.Cli;
using Raft.Node;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.IntegrationTests;

public class RaftNodeTests
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
    public void RaftNodesShouldFindLeader()
    {
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);

        var followerRaftClient = new RaftClient("localhost", 5002);

        var pingReply = followerRaftClient.Ping();
        pingReply.ShouldBe("{ \"reply\": \"Pong from follower1 at localhost:5002\" }");
        var followerInfo = followerRaftClient.Info();
        followerInfo.ShouldBe("{ \"name\": \"follower1\", \"role\": \"Follower\", \"address\": \"localhost:5002\", \"leaderAddress\": \"localhost:5001\" }");

        var leaderRaftClient = new RaftClient("localhost", 5001);
        var leaderInfo = leaderRaftClient.Info();
        leaderInfo.ShouldBe("{ \"name\": \"leader1\", \"role\": \"Leader\", \"address\": \"localhost:5001\", \"leaderAddress\": \"localhost:5001\", \"knownNodes\": \"(follower1=localhost:5002),(follower2=localhost:5003)\" }");
    }

    [Test]
    public void ShouldAppendLogEntriesAtLeader()
    {
        var leader = CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        var followerClient2 = new RaftClient("localhost", 5003);

        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        leader.GetClusterState().ShouldBe("(follower1, 0),(follower2, 0)");
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "+", Literal = 5 });

        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        leader.GetClusterState().ShouldBe("(follower1, 1),(follower2, 1)");
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
        leader.GetClusterState().ShouldBe("(follower1, 0),(follower2, 0)");
        
        follower.Stop();
        _nodes.Remove(follower);
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "+", Literal = 5 });

        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        leader.GetClusterState().ShouldBe("(follower1, 1),(follower2, 0)");
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
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