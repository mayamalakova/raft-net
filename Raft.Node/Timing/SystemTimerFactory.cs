namespace Raft.Node.Timing;

public class SystemTimerFactory : ITimerFactory
{
    public ITimer CreateTimer()
    {
        return new SystemTimer();
    }
}