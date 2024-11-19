namespace Raft.Node.Communication;

public class RaftMessageSender<TMessage, TReply>: IRaftMessageSender<TMessage, TReply>
{
    public void Start()
    {
        throw new NotImplementedException();
    }

    public TReply Send(TMessage message)
    {
        throw new NotImplementedException();
    }
}