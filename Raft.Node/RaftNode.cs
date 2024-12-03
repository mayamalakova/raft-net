using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services;
using Raft.Node.HeatBeat;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _nodeMessageReceiver;
    private readonly INodeStateStore _stateStore;
    private readonly IClientPool _clientPool;
    private readonly IClusterNodeStore _nodeStore;
    private readonly HeartBeatRunner _heartBeatRunner;
    private readonly IMessageReceiver _controlMessageServer;

    private readonly string _nodeName;
    private readonly int _nodePort;
    private readonly NodeAddress _peerAddress;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort, int timeoutSeconds)
    {
        _nodeName = nodeName;
        _nodePort = port;
        _peerAddress = new NodeAddress(clusterHost, clusterPort);
        _stateStore = new NodeStateStore { Role = role };
        _clientPool = new ClientPool();
        _nodeStore = new ClusterNodeStore();
        var logReplicator = new LogReplicator(_stateStore, _clientPool, _nodeStore, _nodeName, timeoutSeconds);
        _heartBeatRunner = new HeartBeatRunner(3000, () => logReplicator.ReplicateToFollowers());
        IEnumerable<INodeService> nodeServices =
        [
            new LeaderDiscoveryService(_stateStore),
            new RegisterNodeService(_nodeStore),
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, _stateStore, _nodeStore),
            new LogReplicationService(_stateStore, _clientPool, logReplicator, _heartBeatRunner),
            new AppendEntriesService(_stateStore),
            new LogInfoService(_stateStore)
        ];
        _nodeMessageReceiver = new RaftMessageReceiver(port, nodeServices);
        var controlService = new ControlService(_heartBeatRunner, _nodeMessageReceiver);
        _controlMessageServer = new ControlMessageReceiver(port + 1000, controlService);
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
                Host = "localhost", //TODO this should be externally visible IP address
                Port = _nodePort
            });
            Console.WriteLine($"Registered with leader {registerReply}");
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
        Console.WriteLine($"{_nodeName} found leader: {leaderAddress}");

        return leaderAddress;
    }

    public void Stop()
    {
        _nodeMessageReceiver.Stop();
        _controlMessageServer.Stop();
    }

    public string GetClusterState()
    {
        var lastIndexes = _nodeStore.GetNodes()
            .Select(x => (x.NodeName, _nodeStore.GetNextIndex(x.NodeName)))
            .ToArray();
        return string.Join(',', lastIndexes);
    }
}