using Grpc.Core;

namespace Raft.Node;

public class NodeCommunicationClient
{
    private readonly string _targetNodeHost;
    private readonly int _targetNodePort;

    public NodeCommunicationClient(string targetNodeHost, int targetNodePort)
    {
        _targetNodeHost = targetNodeHost;
        _targetNodePort = targetNodePort;
    }
    public (string host, int port) GetLeader()
    {
        var channel = new Channel(_targetNodeHost, _targetNodePort, ChannelCredentials.Insecure);  
        var client = new LeaderDiscoverySvc.LeaderDiscoverySvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        return (reply.Host, reply.Port);
    }
}