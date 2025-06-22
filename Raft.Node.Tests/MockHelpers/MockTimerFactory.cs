using NSubstitute;
using Raft.Node.Timing;
using ITimer = Raft.Node.Timing.ITimer;

namespace Raft.Node.Tests.MockHelpers;

public class MockTimerFactory(ITimer timer) : ITimerFactory
{
    public ITimer CreateTimer()
    {
        return timer;
    }
}