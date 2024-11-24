using Grpc.Core;
using Raft.Store.Domain;

namespace Raft.Node.Communication;

public class NodeCommunicationClient(NodeAddress targetAddress)
{
    public NodeAddress GetLeader()
    {
        var channel = new Channel(targetAddress.Host, targetAddress.Port, ChannelCredentials.Insecure);  
        var client = new LeaderDiscoverySvc.LeaderDiscoverySvcClient(channel);
        var reply = client.GetLeader(new LeaderQueryRequest());
        
        return new NodeAddress(reply.Host, reply.Port);
    }
}