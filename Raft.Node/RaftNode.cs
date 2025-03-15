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

public class RaftNode
{
    private readonly IRaftMessageReceiver _nodeMessageReceiver;
    private readonly INodeStateStore _stateStore;
    private readonly IClientPool _clientPool;
    private readonly IClusterNodeStore _clusterStore;
    private readonly HeartBeatRunner _heartBeatRunner;
    private readonly IMessageReceiver _controlMessageServer;

    private readonly string _nodeName;
    private readonly string _nodeHost;
    private readonly int _nodePort;
    private readonly NodeAddress _peerAddress;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort, int timeoutSeconds)
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
        _heartBeatRunner = new HeartBeatRunner(3000, () =>
        {
            logReplicator.ReplicateToFollowers();
            replicationStateManager.UpdateCommitIndex(_clusterStore.GetNodes().ToArray());
        });
        IEnumerable<INodeService> clusterServices =
        [
            new LeaderDiscoveryService(_stateStore),
            new RegisterNodeService(_clusterStore),
            new LogReplicationService(_stateStore, _clusterStore, _clientPool, logReplicator, replicationStateManager, _heartBeatRunner), //listening for command forwarded by other nodes to leader
            new AppendEntriesService(_stateStore),
        ];
        _nodeMessageReceiver = new RaftMessageReceiver(port, clusterServices);
        IEnumerable<INodeService> adminServices =
        [
            new LogReplicationService(_stateStore, _clusterStore, _clientPool, logReplicator, replicationStateManager, _heartBeatRunner), // listening for command from the cli
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, new NodeAddress(_nodeHost, _nodePort), _stateStore, _clusterStore),
            new LogInfoService(_stateStore),
            new ControlService(_heartBeatRunner, _nodeMessageReceiver, _stateStore),
            new GetStateService(_stateStore)
        ];
        _controlMessageServer = new ControlMessageReceiver(port + 1000, adminServices);
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

        _nodeMessageReceiver.Start();
        _controlMessageServer.Start();
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
        _nodeMessageReceiver.Stop();
        _controlMessageServer.Stop();
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