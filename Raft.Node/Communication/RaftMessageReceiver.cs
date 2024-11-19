using Grpc.Core;
using Shared;

namespace Raft.Node.Communication;

public class RaftMessageReceiver<TMessage, TReply>: IRaftMessageReceiver<TMessage, TReply>
{
    private Server _server;

    public RaftMessageReceiver(int port)
    {
        _server = new Server()
        {
            Services = { Svc.BindService(new MyService()) },
            Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
        };
        
    }
    
    public TReply ReceiveMessage(TMessage message)
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        _server.Start();
    }
}