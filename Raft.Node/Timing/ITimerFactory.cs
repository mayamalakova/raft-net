namespace Raft.Node.Timing;

public interface ITimerFactory
{
    ITimer CreateTimer();
}