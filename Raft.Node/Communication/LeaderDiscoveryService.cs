using Grpc.Core;

namespace Raft.Node.Communication;

public class LeaderDiscoveryService(string leaderHost, int leaderPort) : LeaderDiscoverySvc.LeaderDiscoverySvcBase
{
    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LeaderQueryReply
        {
            Host = leaderHost, 
            Port = leaderPort
        });
    }

    public static ServerServiceDefinition GetServiceDefinition(string leaderHost, int leaderPort)
    {
        return LeaderDiscoverySvc.BindService(new LeaderDiscoveryService(leaderHost, leaderPort));
    }
}