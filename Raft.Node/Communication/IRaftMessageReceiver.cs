namespace Raft.Node.Communication;

public interface IRaftMessageReceiver<in TMessage, out TReply>
{
    TReply ReceiveMessage(TMessage message);

    void Start();
}