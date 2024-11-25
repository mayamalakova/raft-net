namespace Raft.Store.Domain.Replication;

public class ReplicationLog
{
    public ICollection<LogEntry> Entries { get; set; }

    public void Append(LogEntry entry)
    {
        Entries.Add(entry);
    }
}