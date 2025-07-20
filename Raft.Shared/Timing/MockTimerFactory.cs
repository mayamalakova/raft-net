using Raft.Node.Timing;
using ITimer = Raft.Node.Timing.ITimer;

namespace Raft.IntegrationTests;

public class MockTimerFactory(ITimer timer) : ITimerFactory
{
    public ITimer CreateTimer()
    {
        return timer;
    }
}