using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services.Admin;
using Raft.Node.Communication.Services.Cluster;
using Raft.Node.HeartBeat;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;
using Serilog;

namespace Raft.Node;

/// <summary>
/// A node in a Raft cluster.
/// Listens for messages from other nodes in the cluster and from admin clients.
/// </summary>
public class RaftNode
{
    private readonly IClusterMessageReceiver _clusterMessageReceiver;
    private readonly IMessageReceiver _adminMessageReceiver;
    private readonly INodeStateStore _stateStore;
    private readonly IClientPool _clientPool;
    private readonly IClusterNodeStore _clusterStore;
    private readonly HeartBeatRunner _heartBeatRunner;

    private readonly string _nodeName;
    private readonly string _nodeHost;
    private readonly int _nodePort;
    private readonly NodeAddress _peerAddress;
    private readonly RaftLeaderService _leaderService;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort, int timeoutSeconds, int
        heartBeatIntervalSeconds)
    {
        _nodeHost = "localhost"; //TODO this should be externally visible IP address
        _nodeName = nodeName;
        _nodePort = port;
        _peerAddress = new NodeAddress(clusterHost, clusterPort);
        _stateStore = new NodeStateStore { Role = role };
        _clientPool = new ClientPool();
        _clusterStore = new ClusterNodeStore();
        var replicationStateManager = new ReplicationStateManager(_stateStore, _clusterStore);
        var logReplicator = new LogReplicator(_stateStore, _clientPool, _clusterStore, _nodeName, timeoutSeconds);
        _leaderService = new RaftLeaderService(logReplicator, replicationStateManager, _stateStore, _clusterStore);
        _heartBeatRunner = new HeartBeatRunner(heartBeatIntervalSeconds * 1000, () =>
        {
            _leaderService.ReconcileCluster();
        });

        _clusterMessageReceiver = new ClusterMessageReceiver(port, GetClusterServices());
        _adminMessageReceiver = new AdminMessageReceiver(port + 1000, GetAdminServices());
    }

    private IEnumerable<INodeService> GetClusterServices()
    {
        return
        [
            new LeaderDiscoveryService(_stateStore),
            new RegisterNodeService(_clusterStore),
            new CommandProcessingService(_stateStore, _clusterStore, _clientPool, _leaderService, _heartBeatRunner),
            new AppendEntriesService(_stateStore, _nodeName),
        ];
    }

    private IEnumerable<INodeService> GetAdminServices()
    {
        return [
            new CommandProcessingService(_stateStore, _clusterStore, _clientPool, _leaderService, _heartBeatRunner),
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, new NodeAddress(_nodeHost, _nodePort), _stateStore, _clusterStore),
            new LogInfoService(_stateStore),
            new ControlService(_heartBeatRunner, _clusterMessageReceiver, _stateStore, _nodeName),
            new GetStateService(_stateStore)
        ];
    }

    public void Start()
    {
        _stateStore.LeaderAddress = _stateStore.Role == NodeType.Follower
            ? AskForLeader()
            : _peerAddress;
        if (_stateStore.Role == NodeType.Follower)
        {
            var registerNodeClient = _clientPool.GetRegisterNodeClient(_stateStore.LeaderAddress);
            var registerReply = registerNodeClient.RegisterNode(new RegisterNodeRequest
            {
                Name = _nodeName,
                Host = _nodeHost, 
                Port = _nodePort
            });
            Log.Information($"Registered with leader {registerReply}");
        }

        _clusterMessageReceiver.Start();
        _adminMessageReceiver.Start();
        if (_stateStore.Role == NodeType.Leader)
        {
            _heartBeatRunner.StartBeating();
        }
    }

    private NodeAddress AskForLeader()
    {
        var client = _clientPool.GetLeaderDiscoveryClient(_peerAddress);
        var reply = client.GetLeader(new LeaderQueryRequest());
        var leaderAddress = new NodeAddress(reply.Host, reply.Port);
        Log.Information($"{_nodeName} found leader: {leaderAddress}");
        return leaderAddress;
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
        return $"commitIndex={_stateStore.CommitIndex}, term={_stateStore.CurrentTerm}, lastApplied={_stateStore.LastApplied}";
    }
}