namespace Raft.Store.Domain.Replication;

public class LogEntry(Command command, int term)
{
    public Command Command { get; } = command;
    public int Term { get; } = term;
}