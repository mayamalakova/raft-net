using System.Timers;

namespace Raft.Shared.Timing;

public interface ITimer
{
    event ElapsedEventHandler Elapsed;
    bool Enabled { get; }
    double Interval { get; set; }
    void Start();
    void Stop();
}