using Raft.Communication.Contract;
using Raft.Node.Communication;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _messageReceiver;
    private readonly NodeCommunicationClient _nodeClient;
    private readonly NodeStateStore _stateStore;

    private readonly string _nodeName;
    private readonly string _clusterHost;
    private readonly int _clusterPort;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
    {
        _nodeName = nodeName;
        _clusterHost = clusterHost;
        _clusterPort = clusterPort;
        _messageReceiver = new RaftMessageReceiver(port);
        _nodeClient = new NodeCommunicationClient(_clusterHost, _clusterPort);
        _stateStore = new NodeStateStore {Role = role};
    }

    public void Start() 
    {
        var leaderAddress = _stateStore.Role == NodeType.Follower 
            ? AskForLeader() 
            : (host: _clusterHost, port: _clusterPort);
        _stateStore.LeaderAddress = new NodeAddress(leaderAddress.host, leaderAddress.port);
        _messageReceiver.Start([
            LeaderDiscoveryService.GetServiceDefinition(_stateStore),
            PingReplyService.GetServiceDefinition(_nodeName),
            NodeInfoService.GetServiceDefinition(_nodeName, _stateStore),
        ]);
    }

    private (string host, int port) AskForLeader()
    {
        var leader = _nodeClient.GetLeader();

        Console.WriteLine($"{_nodeName} found leader: {leader}");
        
        return (leader.host, leader.port);
    }
    
}