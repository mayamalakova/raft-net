using Grpc.Core;
using Raft.Node.Communication;
using Shared;

namespace Raft.Node;

public class RaftNode
{
    private readonly NodeType _role;
    private readonly string _nodeName;
    private readonly int _port;
    private readonly string _clusterHost;
    private readonly int _clusterPort;
    
    private RaftMessageReceiver<IHelloMessage, IHelloReply> _messageReceiver;
    
    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
    {
        _role = role;
        _nodeName = nodeName;
        _port = port;

        _clusterHost = clusterHost;
        _clusterPort = clusterPort;
    }

    public void Start()
    {
        
        if (_role == NodeType.Follower)
        {
            var leader = AskForLeader();
            _messageReceiver = new RaftMessageReceiver<IHelloMessage, IHelloReply>(_port, leader.host, leader.port);
        }
        else
        {
            _messageReceiver = new RaftMessageReceiver<IHelloMessage, IHelloReply>(_port, _clusterHost, _clusterPort);
        }
        _messageReceiver.Start();
    }

    private (string host, int port) AskForLeader()
    {
        var channel = new Channel(_clusterHost, _clusterPort, ChannelCredentials.Insecure);  
        var client = new Svc.SvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        Console.WriteLine($"{_nodeName} got leader reply: {reply.Host} {reply.Port}");
        
        return (reply.Host, reply.Port);
    }
    
}