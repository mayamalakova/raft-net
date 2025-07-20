using System.Timers;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;
using Timer = System.Timers.Timer;

namespace Raft.Node.HeartBeat;

public class HeartBeatRunner
{
    private readonly INodeStateStore _stateStore;
    private readonly Action _action;
    private readonly string _nodeName;
    private readonly Timer _timer;

    /// <summary>
    /// Create a heartbeat runner that will execute the given action at the given interval.
    /// </summary>
    /// <param name="interval">in milliseconds</param>
    /// <param name="stateStore"></param>
    /// <param name="action">action to execute</param>
    /// <param name="nodeName"></param>
    public HeartBeatRunner(int interval, INodeStateStore stateStore, Action action, string nodeName)
    {
        _action = action;
        _nodeName = nodeName;
        _stateStore = stateStore;
        _timer = new Timer(interval); 
        _timer.AutoReset = true; 
        _timer.Elapsed += OnInterval;
    }

    public void StartBeating()
    {
        Log.Information("{node} Starting heartbeat", _nodeName);
        SendBeat();
        _timer.Start();
    }

    private void SendBeat()
    {
        if (_stateStore.Role != NodeType.Leader)
        {
            Log.Information("The node is no longer a leader. Skipping heartbeat.");
            return;
        }
        Log.Debug($"HeartBeat at {DateTime.Now:HH:mm:ss.fff}");
        _action();
    }

    public void StopBeating()
    {
        Log.Information("{node} Stopping heartbeat", _nodeName);
        _timer.Stop();
    }
    
    private void OnInterval(object? sender, ElapsedEventArgs e)
    {
        SendBeat();
    }

    public void ResetTimer()
    {
        _timer.Stop();
        _timer.Start();
    }
}