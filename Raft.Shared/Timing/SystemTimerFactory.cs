using ITimer = Raft.Shared.Timing.ITimer;

namespace Raft.Shared.Timing;

public class SystemTimerFactory : ITimerFactory
{
    public ITimer CreateTimer()
    {
        return new SystemTimer();
    }
}