namespace Raft.Store.Domain.Replication;

public class ReplicationLog
{
    public IList<LogEntry> Entries { get; set; } = new List<LogEntry>();

    public void Append(LogEntry entry)
    {
        Entries.Add(entry);
    }

    /// <summary>
    /// Get the item at the given index or null if index is negative
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public LogEntry? GetItemAt(int index)
    {
        return index < 0 ? null : Entries[index];
    }
}