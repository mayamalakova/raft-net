using Grpc.Core;
using Raft.Node.Communication;
using Shared;

namespace Raft.Node;

public class RaftNode(NodeType nodeType, string nodeId, int port)
{
    private readonly RaftMessageSender<IHelloMessage, IHelloReply> _messageSender = new();
    private readonly RaftMessageReceiver<IHelloMessage, IHelloReply> _messageReceiver = new(port);

    public void Start()
    {
        if (nodeType == NodeType.Leader)
        {
            _messageReceiver.Start();
        }
    }

    public void SendMessage(string message)
    {
        //TODO: do not hardcode destination host and port
        var channel = new Channel("localhost", 5001, ChannelCredentials.Insecure);  
        var client = new Svc.SvcClient(channel);
        var reply = client.SendMessage(new MessageRequest()
        {
            Message = message
        });
        Console.WriteLine($"Got reply: {reply.Reply}");
    }
    
}