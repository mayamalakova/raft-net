using Grpc.Core;
using Shared;

namespace Raft.Node.Communication;

public class MessageProcessingService: Svc.SvcBase
{
    private readonly string _leaderHost;
    private readonly int _leaderPort;

    public MessageProcessingService(string leaderHost, int leaderPort)
    {
        _leaderHost = leaderHost;
        _leaderPort = leaderPort;
    }

    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LeaderQueryReply() {Host = _leaderHost, Port = _leaderPort});
    }
}