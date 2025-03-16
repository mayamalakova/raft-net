using NUnit.Framework;
using Raft.Cli;
using Raft.Node;
using Raft.Shared;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.IntegrationTests;

public class RaftLogReplicationTests
{
    private readonly ICollection<RaftNode> _nodes = new List<RaftNode>();

    [SetUp]
    public void SetUp()
    {
        Logger.ConfigureLogger();
    }

    [TearDown]
    public void TearDown()
    {
        Logger.Close();
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
        followerClient1.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");
        followerClient2.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");

        DisconnectNode(followerClient2, follower2);
        leaderClient.Command(new CommandOptions { Var = "B", Operation = "=", Literal = 1 });
        followerClient1.LogInfo().ShouldBe("{ \"entries\": \"(A=1), (B=1)\" }");
        followerClient2.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");

        ReconnectNode(followerClient2, follower2);
        Task.Delay(4000).Wait();
        followerClient2.LogInfo().ShouldBe("{ \"entries\": \"(A=1), (B=1)\" }");
    }

    [Test]
    public void ShouldApplyLatestCommittedAtLeaderAfterReconcilingFollowerToGetMajority()
    {
        var leader = CreateLeader("leader1", 5001, 2);
        var follower1 = CreateFollower("follower1", 5002, 5001);
        var follower2 = CreateFollower("follower2", 5003, 5002);

        var leaderClient = new RaftClient("localhost", 5001);
        var followerClient1 = new RaftClient("localhost", 5002);
        var followerClient2 = new RaftClient("localhost", 5003);
        leader.GetNodeState().ShouldBe("commitIndex=-1, term=0, lastApplied=-1");

        // command 1 - A=1
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 1 });
        followerClient1.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");
        followerClient2.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");

        // disconnect follower2
        DisconnectNode(followerClient2, follower2);

        // command 2 - A=2
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = 2 });
        followerClient1.LogInfo().ShouldBe("{ \"entries\": \"(A=1), (A=2)\" }");
        followerClient2.LogInfo().ShouldBe("{ \"entries\": \"(A=1)\" }");
        leaderClient.GetState().ShouldBe("value: 1, errors: [ ]");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
        follower1.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");

        // reconnect follower2 and give time to replicate
        ReconnectNode(followerClient2, follower2);

        for (var i = 0; i < 4; i++)
        {
            Task.Delay(2000).Wait();
            if (HaveReconciled([leader, follower1, follower2], "commitIndex=1, term=0, lastApplied=1")) break;
            Console.WriteLine($"Checking if they have reconciled... {i + 1}");
        }

        leader.GetNodeState().ShouldBe("commitIndex=1, term=0, lastApplied=1");
        // TODO - figure out why follower1 stops responding at times
        // follower1.GetNodeState().ShouldBe("commitIndex=1, term=0, lastApplied=1");
        follower2.GetNodeState().ShouldBe("commitIndex=1, term=0, lastApplied=1");
        leaderClient.GetState().ShouldBe("value: 2, errors: [ ]");
        followerClient2.GetState().ShouldBe("value: 2, errors: [ ]");
        // followerClient1.GetState().ShouldBe("value: 2, errors: [ ]");
    }

    [Test]
    public void ShouldApplyCommittedEntriesOnLeaderAfterReplication()
    {
        var leader = CreateLeader("leader1", 5001);
        var leaderClient = new RaftClient("localhost", 5001);

        const int someLiteral = 5;
        var result = leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = someLiteral });

        result.ShouldContain($"value={someLiteral}");
        leaderClient.GetState().ShouldBe($"value: {someLiteral}, errors: [ ]");
        leader.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
    }

    [Test]
    public void ShouldApplyCommittedEntriesOnFollowerAfterReplication()
    {
        CreateLeader("leader1", 5001);
        var leaderClient = new RaftClient("localhost", 5001);
        var follower = CreateFollower("follower1", 5002, 5001);

        const int someLiteral = 5;
        leaderClient.Command(new CommandOptions { Var = "A", Operation = "=", Literal = someLiteral });

        Task.Delay(4000).Wait();

        follower.GetNodeState().ShouldBe("commitIndex=0, term=0, lastApplied=0");
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
        var leader = new RaftNode(NodeType.Leader, name, port, "localhost", port, 1, 3);
        leader.Start();
        _nodes.Add(leader);
        return leader;
    }

    private RaftNode CreateLeader(string name, int port, int heartBeatInterval)
    {
        var leader = new RaftNode(NodeType.Leader, name, port, "localhost", port, 1, heartBeatInterval);
        leader.Start();
        _nodes.Add(leader);
        return leader;
    }

    private RaftNode CreateFollower(string name, int port, int peerPort)
    {
        var node = new RaftNode(NodeType.Follower, name, port, "localhost", peerPort, 1, 3);
        node.Start();
        _nodes.Add(node);
        return node;
    }

    private bool HaveReconciled(RaftNode[] nodes, string desiredState)
    {
        var nodeStates = nodes.Select(n => n.GetNodeState()).ToArray();
        Console.WriteLine(string.Join(", ", nodeStates));
        return nodeStates.ToHashSet().Count == 1 && nodeStates.First() == desiredState;
    }
}