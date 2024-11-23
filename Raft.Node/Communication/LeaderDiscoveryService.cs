using Grpc.Core;

namespace Raft.Node.Communication;

public class LeaderDiscoveryService(string leaderHost, int leaderPort) : Svc.SvcBase
{
    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LeaderQueryReply
        {
            Host = leaderHost, 
            Port = leaderPort
        });
    }
}