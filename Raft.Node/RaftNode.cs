using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _messageReceiver;
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
    }

    public void Start() 
    {
        var leaderAddress = _role == NodeType.Follower 
            ? AskForLeader() 
            : (host: _clusterHost, port: _clusterPort);
        _messageReceiver.Start([
            LeaderDiscoveryService.GetServiceDefinition(leaderAddress.host, leaderAddress.port),
            PingReplyService.GetServiceDefinition(_nodeName)
        ]);
    }

    private (string host, int port) AskForLeader()
    {
        var channel = new Channel(_clusterHost, _clusterPort, ChannelCredentials.Insecure);  
        var client = new LeaderDiscoverySvc.LeaderDiscoverySvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        Console.WriteLine($"{_nodeName} found leader: {reply}");
        
        return (reply.Host, reply.Port);
    }
    
}