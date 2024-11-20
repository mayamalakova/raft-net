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
    
    private IRaftMessageReceiver _messageReceiver;
    
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
            var leader = AskForLeader(_clusterHost, _clusterPort);
            _messageReceiver = new RaftMessageReceiver(_port, leader.host, leader.port);
            Console.WriteLine($"{_nodeName} got leader reply: {leader.host} {leader.port}");
        }
        else
        {
            _messageReceiver = new RaftMessageReceiver(_port, _clusterHost, _clusterPort);
        }
        _messageReceiver.Start();
    }

    private (string host, int port) AskForLeader(string clusterHost, int clusterPort)
    {
        var channel = new Channel(clusterHost, clusterPort, ChannelCredentials.Insecure);  
        var client = new Svc.SvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        return (reply.Host, reply.Port);
    }
    
}