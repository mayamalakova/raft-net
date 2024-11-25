namespace Raft.Store.Domain;

public class LogEntry
{
    public Command Command { get; set; }
    public int Term { get; set; }
}