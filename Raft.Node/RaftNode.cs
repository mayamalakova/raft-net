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
    void BecomeFollowerOfLeaderWithId(string leaderId, int term);
    void BecomeCandidate();
}

/// <summary>
/// A node in a Raft cluster.
/// Listens for messages from other nodes in the cluster and from admin clients.
/// </summary>
public class RaftNode : IRaftNode, IElectionResultsReceiver
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
    public IElectionManager ElectionManager { get; set; }

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

        ElectionManager = new ElectionManager(_nodeName, _clusterStore, _clientPool, this);
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

    public void BecomeFollower(NodeInfo? leaderInfo, int term)
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

    public void BecomeFollowerOfLeaderWithId(string leaderId, int term)
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
        ElectionManager.StartElectionAsync(StateStore.CurrentTerm);
    }

    public void OnElectionWon(int termAtElectionStart)
    {
        if (StateStore.CurrentTerm == termAtElectionStart && StateStore.Role == NodeType.Candidate)
        {
            BecomeLeader(StateStore.CurrentTerm);
        }
    }

    public void OnElectionLost(int termAtElectionStart)
    {
        if (StateStore.CurrentTerm == termAtElectionStart && StateStore.Role == NodeType.Candidate)
        {
            ElectionManager.StartElectionAsync(StateStore.CurrentTerm);
        }
    }

    public void OnHigherTermReceivedWithVoteReply(int newTerm)
    {
        Log.Information($"{_nodeName} received higher term in vote reply, stepping down to follower");
        // Become follower with no leader info (will be set when AppendEntries received)
        BecomeFollower(null, newTerm);
    }
}