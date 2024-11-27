using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _messageReceiver;
    private readonly INodeStateStore _stateStore;
    private readonly IEnumerable<INodeService> _nodeServices;
    private readonly IClientPool _clientPool;

    private readonly string _nodeName;
    private readonly int _nodePort;
    private readonly NodeAddress _peerAddress;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
    {
        _nodeName = nodeName;
        _nodePort = port;
        _peerAddress = new NodeAddress(clusterHost, clusterPort);
        _messageReceiver = new RaftMessageReceiver(port);
        _stateStore = new NodeStateStore { Role = role };
        _clientPool = new ClientPool();
        IClusterNodeStore nodeStore = new ClusterNodeStore();
        _nodeServices =
        [
            new LeaderDiscoveryService(_stateStore),
            new RegisterNodeService(nodeStore),
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, _stateStore, nodeStore),
            new LogReplicationService(_stateStore, _clientPool, nodeStore, nodeName),
            new AppendEntriesService(_stateStore)
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
                Host = "localhost", //TODO this should be externally visible IP address
                Port = _nodePort
            });
            Console.WriteLine($"Registered with leader {registerReply}");
        }
        _messageReceiver.Start(_nodeServices.Select(x => x.GetServiceDefinition()));
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
        _messageReceiver.Stop();
    }
}