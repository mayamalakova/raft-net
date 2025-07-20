using ITimer = Raft.Shared.Timing.ITimer;

namespace Raft.Shared.Timing;

public class MockTimerFactory(ITimer timer) : ITimerFactory
{
    public ITimer CreateTimer()
    {
        return timer;
    }
}