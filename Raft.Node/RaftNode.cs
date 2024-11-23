using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication;

namespace Raft.Node;

public class RaftNode
{
    private readonly NodeType _role;
    private readonly string _nodeName;
    private readonly string _clusterHost;
    private readonly int _clusterPort;
    
    private readonly IRaftMessageReceiver _messageReceiver;
    
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
            ? AskForLeader(_clusterHost, _clusterPort) 
            : (host: _clusterHost, port: _clusterPort);
        _messageReceiver.Start([new MessageProcessingService(leaderAddress.host, leaderAddress.port)]);
    }

    private (string host, int port) AskForLeader(string clusterHost, int clusterPort)
    {
        var channel = new Channel(clusterHost, clusterPort, ChannelCredentials.Insecure);  
        var client = new Svc.SvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        Console.WriteLine($"Leader found: {reply}");
        
        return (reply.Host, reply.Port);
    }
    
}