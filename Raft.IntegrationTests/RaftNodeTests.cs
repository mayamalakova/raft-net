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
        CreateLeader("leader1", 5001);
        CreateFollower("follower1", 5002, 5001);
        CreateFollower("follower2", 5003, 5002);

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        var followerClient2 = new RaftClient("localhost", 5003);

        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "+", Literal = 5 });

        leaderClient.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        followerClient1.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
        followerClient2.LogInfo().ShouldBe( "{ \"entries\": \"(A=1), (A+5)\" }");
    }

    private void CreateLeader(string name, int port)
    {
        var leader = new RaftNode(NodeType.Leader, name, port, "localhost", port);
        leader.Start();
        _nodes.Add(leader);
    }
    
    private void CreateFollower(string name, int port, int peerPort)
    {
        var leader = new RaftNode(NodeType.Follower, name, port, "localhost", peerPort);
        leader.Start();
        _nodes.Add(leader);
    }
}