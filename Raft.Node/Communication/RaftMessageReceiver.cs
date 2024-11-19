using Grpc.Core;
using Shared;

namespace Raft.Node.Communication;

public class RaftMessageReceiver<TMessage, TReply>(int port) : IRaftMessageReceiver<TMessage, TReply>
{
    private readonly Server _server = new()
    {
        Services = { Svc.BindService(new MessageProcessingService()) },
        Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
    };

    public TReply ReceiveMessage(TMessage message)
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        _server.Start();
        
    }
}