using Grpc.Core;
using Shared;

namespace Raft.Node.Communication;

public class MessageProcessingService: Svc.SvcBase
{
    public override Task<MessageReply> SendMessage(MessageRequest request, ServerCallContext context)
    {
        Console.WriteLine($"Got message: {request.Message}");
        return Task.FromResult(new MessageReply() { Reply = $"Message received! {request.Message}" });
    }
}