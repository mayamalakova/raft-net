using Raft.Communication.Contract;
using Raft.Node.Communication;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _messageReceiver;
    private readonly NodeCommunicationClient _nodeClient;
    
    private readonly NodeType _role;
    private readonly string _nodeName;
    private readonly string _clusterHost;
    private readonly int _clusterPort;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
    {
        _role = role;
        _nodeName = nodeName;
        _clusterHost = clusterHost;
        _clusterPort = clusterPort;
        _messageReceiver = new RaftMessageReceiver(port);
        _nodeClient = new NodeCommunicationClient(_clusterHost, _clusterPort);
    }

    public void Start() 
    {
        var leaderAddress = _role == NodeType.Follower 
            ? AskForLeader() 
            : (host: _clusterHost, port: _clusterPort);
        _messageReceiver.Start([
            LeaderDiscoveryService.GetServiceDefinition(leaderAddress.host, leaderAddress.port),
            PingReplyService.GetServiceDefinition(_nodeName),
            NodeInfoService.GetServiceDefinition(_nodeName, _role),
        ]);
    }

    private (string host, int port) AskForLeader()
    {
        var leader = _nodeClient.GetLeader();

        Console.WriteLine($"{_nodeName} found leader: {leader}");
        
        return (leader.host, leader.port);
    }
    
}