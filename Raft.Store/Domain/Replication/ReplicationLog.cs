namespace Raft.Store.Domain.Replication;

public class ReplicationLog
{
    public List<LogEntry> Entries { get; } = new();

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

    public void RemoveFrom(int index)
    {
        Entries.RemoveRange(index, Entries.Count - index);
    }

    public IList<LogEntry> GetFrom(int startIndex)
    {
        return Entries.TakeLast(Entries.Count - startIndex).ToList();
    }
}