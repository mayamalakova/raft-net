using System.Timers;
using Serilog;
using Timer = System.Timers.Timer;

namespace Raft.Node.HeartBeat;

public class HeartBeatRunner
{
    private readonly Action _action;
    private readonly Timer _timer;

    /// <summary>
    /// Create a heartbeat runner that will execute the given action at the given interval.
    /// </summary>
    /// <param name="interval">in milliseconds</param>
    /// <param name="action">action to execute</param>
    public HeartBeatRunner(int interval, Action action)
    {
        _action = action;
        _timer = new Timer(interval); 
        _timer.AutoReset = true; 
        _timer.Elapsed += PerformAction;
    }

    public void StartBeating()
    {
        _timer.Enabled = true;
    }

    public void StopBeating()
    {
        _timer.Enabled = false;
    }
    
    private void PerformAction(object? sender, ElapsedEventArgs e)
    {
        Log.Debug($"HeartBeat at {DateTime.Now:HH:mm:ss.fff}");
        _action();
    }

    public void ResetTimer()
    {
        _timer.Stop();
        _timer.Start();
    }
}