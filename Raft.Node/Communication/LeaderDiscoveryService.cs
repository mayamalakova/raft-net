using Grpc.Core;

namespace Raft.Node.Communication;

public class LeaderDiscoveryService(NodeStateStore stateStore) : LeaderDiscoverySvc.LeaderDiscoverySvcBase
{
    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        var leaderAddress = stateStore.LeaderAddress;
        return Task.FromResult(new LeaderQueryReply
        {
            Host = leaderAddress.Host, 
            Port = leaderAddress.Port,
        });
    }

    public static ServerServiceDefinition GetServiceDefinition(NodeStateStore stateStore)
    {
        return LeaderDiscoverySvc.BindService(new LeaderDiscoveryService(stateStore));
    }
}