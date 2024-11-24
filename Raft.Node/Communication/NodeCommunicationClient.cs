using Grpc.Core;
using Raft.Store.Domain;

namespace Raft.Node.Communication;

public class NodeCommunicationClient
{
    private readonly string _targetNodeHost;
    private readonly int _targetNodePort;

    public NodeCommunicationClient(string targetNodeHost, int targetNodePort)
    {
        _targetNodeHost = targetNodeHost;
        _targetNodePort = targetNodePort;
    }
    public NodeAddress GetLeader()
    {
        var channel = new Channel(_targetNodeHost, _targetNodePort, ChannelCredentials.Insecure);  
        var client = new LeaderDiscoverySvc.LeaderDiscoverySvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        return new NodeAddress(reply.Host, reply.Port);
    }
}