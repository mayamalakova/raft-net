namespace Raft.Communication.Contract;

public interface IRaftMessageReceiver
{
    void Start(IEnumerable<Svc.SvcBase> services);
}