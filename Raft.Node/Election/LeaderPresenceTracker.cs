using System.Timers;
using Raft.Node.Timing;
using ITimer = Raft.Node.Timing.ITimer;

namespace Raft.Node.Election;

public class LeaderPresenceTracker
{
    private readonly IRaftNode _node;
    private readonly ITimer _timer;
    private readonly Lock _lock = new();

    public LeaderPresenceTracker(IRaftNode node, ITimerFactory timerFactory)
    {
        _node = node;
        _timer = timerFactory.CreateTimer();
        _timer.Elapsed += OnTimerExpired;
    }

    public double Start()
    {
        lock (_lock)
        {
            if (_timer.Enabled) return _timer.Interval;
            var timerInterval = new Random().Next(150, 300);
            _timer.Interval = timerInterval;
            _timer.Start();
            return timerInterval;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _timer.Stop();
        }
    }

    public double Reset()
    {
        lock (_lock) {
            _timer.Stop();
            _timer.Start();
            return _timer.Interval;
        }
    }

    private void OnTimerExpired(object? sender, ElapsedEventArgs e)
    {
        _node.BecomeCandidate();
    }

    public bool IsStarted()
    {
        lock (_lock)
        {
            return _timer.Enabled;
        }
    }
}