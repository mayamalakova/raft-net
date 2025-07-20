namespace Raft.Shared.Timing;

public interface ITimerFactory
{
    ITimer CreateTimer();
}