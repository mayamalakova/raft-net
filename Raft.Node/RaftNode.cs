using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication;

namespace Raft.Node;

public class RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
{
    private readonly IRaftMessageReceiver _messageReceiver = new RaftMessageReceiver(port);

    public void Start() 
    {
        var leaderAddress = role == NodeType.Follower 
            ? AskForLeader() 
            : (host: clusterHost, port: clusterPort);
        _messageReceiver.Start([new LeaderDiscoveryService(leaderAddress.host, leaderAddress.port)]);
    }

    private (string host, int port) AskForLeader()
    {
        var channel = new Channel(clusterHost, clusterPort, ChannelCredentials.Insecure);  
        var client = new Svc.SvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        Console.WriteLine($"{nodeName} found leader: {reply}");
        
        return (reply.Host, reply.Port);
    }
    
}