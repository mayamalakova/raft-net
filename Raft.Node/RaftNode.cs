using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services.Admin;
using Raft.Node.Communication.Services.Cluster;
using Raft.Node.Election;
using Raft.Node.HeartBeat;
using Raft.Node.Timing;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;
using Serilog;

namespace Raft.Node;

public interface IRaftNode
{
    void Start();
    void Stop();
    string GetClusterState();
    string GetNodeState();
    void BecomeLeader(int term);
    void BecomeFollower(string leaderId, int term);
    void BecomeCandidate();
}

/// <summary>
/// A node in a Raft cluster.
/// Listens for messages from other nodes in the cluster and from admin clients.
/// </summary>
public class RaftNode : IRaftNode
{
    private readonly IClusterMessageReceiver _clusterMessageReceiver;
    private readonly IMessageReceiver _adminMessageReceiver;
    private readonly IClientPool _clientPool;
    private readonly IClusterNodeStore _clusterStore;
    private readonly HeartBeatRunner _heartBeatRunner;

    private readonly string _nodeName;
    private readonly string _nodeHost;
    private readonly int _nodePort;
    private readonly NodeAddress _peerAddress;
    private readonly RaftLeaderService _leaderService;
    private readonly ILeaderPresenceTracker _leaderPresenceTracker;

    public INodeStateStore StateStore { get; }

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort,
        int replicationTimeoutSeconds, int heartBeatIntervalSeconds, ITimerFactory timerFactory)
    {
        _nodeHost = "localhost"; //TODO this should be externally visible IP address
        _nodeName = nodeName;
        _nodePort = port;
        _peerAddress = new NodeAddress(clusterHost, clusterPort);
        StateStore = new NodeStateStore { Role = role };
        _clientPool = new ClientPool();
        _clusterStore = new ClusterNodeStore();
        var replicationStateManager = new ReplicationStateManager(StateStore, _clusterStore);
        var logReplicator =
            new LogReplicator(StateStore, _clientPool, _clusterStore, _nodeName, replicationTimeoutSeconds);
        _leaderService = new RaftLeaderService(logReplicator, replicationStateManager, StateStore, _clusterStore);
        _heartBeatRunner = new HeartBeatRunner(heartBeatIntervalSeconds * 1000, StateStore,
            () => { _leaderService.ReconcileCluster(); });
        _leaderPresenceTracker = new LeaderPresenceTracker(this, timerFactory);

        _clusterMessageReceiver = new ClusterMessageReceiver(port, GetClusterServices());
        _adminMessageReceiver = new AdminMessageReceiver(port + 1000, GetAdminServices());
    }

    private IEnumerable<INodeService> GetClusterServices()
    {
        return
        [
            new LeaderDiscoveryService(StateStore),
            new RegisterNodeService(StateStore, _clusterStore, _clientPool),
            new CommandProcessingService(StateStore, _clientPool, _leaderService, _heartBeatRunner),
            new AppendEntriesService(StateStore, this, _leaderPresenceTracker, _nodeName),
            new RequestVoteService(StateStore)
        ];
    }

    private IEnumerable<INodeService> GetAdminServices()
    {
        return
        [
            new CommandProcessingService(StateStore, _clientPool, _leaderService, _heartBeatRunner),
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, new NodeAddress(_nodeHost, _nodePort), StateStore, _clusterStore),
            new LogInfoService(StateStore),
            new ControlService(_heartBeatRunner, _clusterMessageReceiver, StateStore, _nodeName),
            new GetStateService(StateStore)
        ];
    }

    public void Start()
    {
        switch (StateStore.Role)
        {
            case NodeType.Leader:
                StartLeader();
                break;
            case NodeType.Follower:
                StartFollower();
                break;
            default:
                throw new ArgumentException($"Unknown node role {StateStore.Role}");
        }
    }

    private void StartLeader()
    {
        StateStore.LeaderInfo = new NodeInfo(_nodeName, _peerAddress);
        _clusterMessageReceiver.Start();
        _adminMessageReceiver.Start();
        _heartBeatRunner.StartBeating();
    }

    private void StartFollower()
    {
        var leaderReply = AskForLeader();
        StateStore.LeaderInfo = new NodeInfo(leaderReply.name, leaderReply.address);
        _clusterStore.AddNode(leaderReply.name, leaderReply.address);
        var registerNodeClient = _clientPool.GetRegisterNodeClient(StateStore.LeaderInfo.NodeAddress);
        var registerReply = registerNodeClient.RegisterNode(new RegisterNodeRequest
        {
            Name = _nodeName,
            Host = _nodeHost,
            Port = _nodePort
        });
        Log.Information($"Registered with leader {registerReply.Reply}");

        foreach (var n in registerReply.Nodes)
        {
            _clusterStore.AddNode(n.Name, new NodeAddress(n.Host, n.Port));
        }

        _clusterMessageReceiver.Start();
        _adminMessageReceiver.Start();
        _leaderPresenceTracker.Start();
    }

    private (string name, NodeAddress address) AskForLeader()
    {
        var client = _clientPool.GetLeaderDiscoveryClient(_peerAddress);
        var reply = client.GetLeader(new LeaderQueryRequest());
        var leaderAddress = new NodeAddress(reply.Host, reply.Port);
        Log.Information($"{_nodeName} found leader {reply.Name}: {leaderAddress}");
        return (reply.Name, leaderAddress);
    }

    public void Stop()
    {
        _clusterMessageReceiver.Stop();
        _adminMessageReceiver.Stop();
    }

    public string GetClusterState()
    {
        var lastIndexes = _clusterStore.GetNodes()
            .Select(x => (x.NodeName, _clusterStore.GetNextIndex(x.NodeName)))
            .ToArray();
        return string.Join(',', lastIndexes);
    }

    public string GetNodeState()
    {
        return
            $"commitIndex={StateStore.CommitIndex}, term={StateStore.CurrentTerm}, lastApplied={StateStore.LastApplied}";
    }

    public void BecomeLeader(int term)
    {
        Log.Information($"{_nodeName} becoming leader");
        _leaderPresenceTracker.Stop();
        StateStore.Role = NodeType.Leader;
        StateStore.LeaderInfo = new NodeInfo(_nodeName, new NodeAddress(_nodeHost, _nodePort));
        StateStore.CurrentTerm = term;
        // Reset vote state when becoming leader
        StateStore.VotedFor = null;
        StateStore.LastVoteTerm = -1;
        _heartBeatRunner.StartBeating();
    }

    public void BecomeFollower(NodeInfo leaderInfo, int term)
    {
        Log.Information($"{_nodeName} becoming follower");
        StateStore.Role = NodeType.Follower;
        _heartBeatRunner.StopBeating();
        StateStore.CurrentTerm = term;
        StateStore.LeaderInfo = leaderInfo;
        // Reset vote state when becoming follower
        StateStore.VotedFor = null;
        StateStore.LastVoteTerm = -1;
        _leaderPresenceTracker.Start();
    }

    public void BecomeFollower(string leaderId, int term)
    {
        var newLeader = _clusterStore.GetNodes().First(x => x.NodeName == leaderId);
        BecomeFollower(newLeader, term);
    }

    public void BecomeCandidate()
    {
        Log.Information($"{_nodeName} becoming candidate");
        StateStore.Role = NodeType.Candidate;
        _leaderPresenceTracker.Stop();
        
        StateStore.CurrentTerm++;
        var tasks = _clusterStore.GetNodes()
            .ToDictionary(
                node => node.NodeName,
                node => SendRequestForVote(node, StateStore.CurrentTerm)
            );
        
        var collectVotesWithinElectionTimeout = Task.WhenAny(
            Task.WhenAll(tasks.Values),
            Task.Delay(TimeSpan.FromSeconds(5))
        );
        
        collectVotesWithinElectionTimeout.Wait();
        
        var (repliesReceived, higherTermReceived) = CountAndLogVotes(tasks);
        Log.Information("{NodeName} received {RepliesReceived} replies out of {TasksCount} nodes", _nodeName, repliesReceived, tasks.Count);
        
        // If we received a higher term, step down immediately
        if (higherTermReceived)
        {
            Log.Information($"{_nodeName} received higher term in vote reply, stepping down to follower");
            // StateStore.Role = NodeType.Follower;
            // _leaderPresenceTracker.Start();
            return;
        }
        
        ProcessElectionResult(repliesReceived, tasks.Count);
    }

    private (int votesReceived, bool higherTermReceived) CountAndLogVotes(Dictionary<string, Task<RequestForVoteReply>> taskEntries)
    {
        var failed = taskEntries.Where(t => !t.Value.IsCompletedSuccessfully);
        var notGranted = taskEntries.Where(t => t.Value is { IsCompletedSuccessfully: true, Result.VoteGranted: false });
        var granted = taskEntries.Where(t => t.Value is { IsCompletedSuccessfully: true, Result.VoteGranted: true }).ToArray();
        
        failed.ToList().ForEach(t => Log.Information("Failed to get vote response for vote to {NodeName}", t.Key));
        notGranted.ToList().ForEach(t => Log.Information("Vote not granted for {NodeName}", t.Key));
        granted.ToList().ForEach(t => Log.Information("Vote granted by {NodeName}", t.Key));

        // Check if any reply contained a higher term than our current term
        var higherTermReceived = taskEntries.Any(t => 
            t.Value.IsCompletedSuccessfully && 
            t.Value.Result.Term > StateStore.CurrentTerm);

        return (granted.Count(), higherTermReceived);
    }

    private void ProcessElectionResult(int votesReceived, int totalNodes)
    {
        var totalVotes = votesReceived + 1; // +1 for self
        var votesNeeded = (totalNodes / 2) + 1; // Majority
        
        Log.Information($"{_nodeName} election result: {totalVotes}/{totalNodes} votes (need {votesNeeded} for majority)");
        
        if (totalVotes >= votesNeeded)
        {
            Log.Information($"{_nodeName} won election with {totalVotes}/{totalNodes} votes");
            BecomeLeader(StateStore.CurrentTerm);
        }
        else
        {
            Log.Information($"{_nodeName} lost election with {totalVotes}/{totalNodes} votes, becoming follower");
            // StateStore.Role = NodeType.Follower;
            // _leaderPresenceTracker.Start();
        }
    }

    private Task<RequestForVoteReply> SendRequestForVote(NodeInfo node, int term)
    {
        var request = new RequestForVoteMessage() { CandidateId = _nodeName, Term = term};
        var client = _clientPool.GetRequestForVoteClient(node.NodeAddress);
        return client.RequestVoteAsync(request).ResponseAsync;
    }
}