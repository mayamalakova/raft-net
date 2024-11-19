using Raft.Node.Communication;
using Raft.Node.Communication.Messages;

namespace Raft.Node;

public class RaftNode
{
    private readonly NodeType _nodeType;
    private readonly string _nodeId;
    private readonly int _port;
    
    private readonly RaftMessageSender<IHelloMessage, IHelloReply> _messageSender;
    private readonly RaftMessageReceiver<IHelloMessage, IHelloReply> _messageReceiver;

    public RaftNode(NodeType nodeType, string nodeId, int port)
    {
        _nodeType = nodeType;
        _nodeId = nodeId;
        _port = port;
        _messageSender = new RaftMessageSender<IHelloMessage, IHelloReply>();
        _messageReceiver = new RaftMessageReceiver<IHelloMessage, IHelloReply>(port);
    }

    public void Start()
    {
        // _messageSender.Start();
        _messageReceiver.Start();
    }

    public void SendMessage(string message)
    {
        _messageSender.Send(new HelloMessage(message));
    }
    
}