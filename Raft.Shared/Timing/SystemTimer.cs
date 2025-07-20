using System.Timers;
using ITimer = Raft.Shared.Timing.ITimer;
using Timer = System.Timers.Timer;

namespace Raft.Shared.Timing;

public class SystemTimer : ITimer
{
    private readonly Timer _timer;

    public SystemTimer()
    {
        _timer = new Timer();
    }

    public event ElapsedEventHandler Elapsed
    {
        add => _timer.Elapsed += value;
        remove => _timer.Elapsed -= value;
    }

    public bool Enabled => _timer.Enabled;

    public double Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
}