using System.Timers;
using Timer = System.Timers.Timer;

namespace Raft.Node.HeatBeat;

public class HeartBeatRunner
{
    private readonly Action _action;
    private readonly Timer _timer;

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
        Console.WriteLine($"Action executed at {DateTime.Now:HH:mm:ss.fff}");
        _action();
    }
}