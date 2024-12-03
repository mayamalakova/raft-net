namespace Raft.Communication.Contract;

public interface IMessageReceiver
{
    void Start();
    void Stop();
}