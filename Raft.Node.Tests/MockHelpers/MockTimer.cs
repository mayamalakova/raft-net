
using System.Timers;
using ITimer = Raft.Node.Timing.ITimer;

public class MockTimer : ITimer
{
    private bool _enabled;
    private double _interval;
    private event ElapsedEventHandler? _elapsed;

    public event ElapsedEventHandler Elapsed
    {
        add => _elapsed += value;
        remove => _elapsed -= value;
    }

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public double Interval
    {
        get => _interval;
        set => _interval = value;
    }

    public void Start()
    {
        _enabled = true;
    }

    public void Stop()
    {
        _enabled = false;
    }

    public void SimulateElapsed()
    {
        if (_enabled && _elapsed != null)
        {
            _elapsed.Invoke(this, new ElapsedEventArgs(DateTime.Now));
        }
    }
}