using Grpc.Core;
using Raft.Communication.Contract;

namespace Raft.Node.Communication.Services.Control;

public class PingReplyService(string nodeName) : PingSvc.PingSvcBase, INodeService
{
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply
        {
            Reply = $"Pong from {nodeName} at {context.Host}"
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return PingSvc.BindService(this);
    }
}