using Grpc.Core;
using Raft.Store.Domain;

namespace Raft.Node.Communication;

public class NodeCommunicationClient(NodeAddress targetAddress)
{
    private readonly Channel _channel = new(targetAddress.Host, targetAddress.Port, ChannelCredentials.Insecure);

    public NodeAddress GetLeader()
    {
        var client = new LeaderDiscoverySvc.LeaderDiscoverySvcClient(_channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        return new NodeAddress(reply.Host, reply.Port);
    }
}