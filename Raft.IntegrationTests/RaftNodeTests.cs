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

        var raftClient = new RaftClient("localhost", 5002);

        var pingReply = raftClient.Ping();
        pingReply.ShouldBe("{ \"reply\": \"Pong from follower1 at localhost:5002\" }");
        var info = raftClient.Info();
        info.ShouldBe("{ \"name\": \"follower1\", \"role\": \"Follower\", \"address\": \"localhost:5002\", \"leaderAddress\": \"localhost:5001\" }");
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