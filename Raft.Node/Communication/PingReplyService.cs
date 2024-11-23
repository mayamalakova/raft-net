using Grpc.Core;

namespace Raft.Node.Communication;

public class PingReplyService(string nodeName) : PingSvc.PingSvcBase
{
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply()
        {
            Reply = $"Pong from {nodeName} at {context.Host}"
        });
    }

    public static ServerServiceDefinition GetServiceDefinition(string nodeName)
    {
        return PingSvc.BindService(new PingReplyService(nodeName));
    }
}