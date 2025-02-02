namespace Raft.Store.Domain.Replication;

public class ReplicationLog
{
    private readonly List<LogEntry> _entries = [];
    
    public int Length => _entries.Count;

    public void Append(LogEntry entry)
    {
        _entries.Add(entry);
    }

    /// <summary>
    /// Get the item at the given index or null if index is negative
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public LogEntry? GetItemAt(int index)
    {
        return index < 0 ? null : _entries[index];
    }

    public void RemoveFrom(int index)
    {
        _entries.RemoveRange(index, _entries.Count - index);
    }

    public IList<LogEntry> GetLastEntries(int count)
    {
        return _entries.TakeLast(count).ToList();
    }
    
    public override string ToString()
    {
        return string.Join(", ", _entries.Select(e => e.Command));
    }
}