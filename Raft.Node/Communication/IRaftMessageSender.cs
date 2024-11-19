namespace Raft.Node.Communication;

public interface IRaftMessageSender<in TMessage, out TReply>
{
    void Start();
    TReply Send(TMessage message);
}