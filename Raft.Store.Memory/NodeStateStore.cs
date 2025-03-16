using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Store.Memory;

public class NodeStateStore : INodeStateStore
{
    private readonly ReplicationLog _log = new();

    public NodeType Role { get; set; }
    public NodeInfo? LeaderInfo { get; set; }
    public int CurrentTerm { get; set; }
    public int CommitIndex { get; set; } = -1; // -1 means no items committed
    public int LastApplied { get; set; } = -1; // -1 means no items applied

    public StateMachine StateMachine { get; init; } = new();
    public int LogLength => _log.Length;

    public void AppendLogEntry(LogEntry entry)
    {
        _log.Append(entry);
    }

    public string PrintLog()
    {
        return _log.ToString();
    }

    public int GetTermAtIndex(int lastLogIndex)
    {
        return _log.GetItemAt(lastLogIndex)?.Term ?? -1;
    }

    public LogEntry? EntryAtIndex(int index)
    {
        return _log.GetItemAt(index);
    }

    public void RemoveLogEntriesFrom(int index)
    {
        _log.RemoveFrom(index);
    }

    public IList<LogEntry> GetLastEntries(int entriesCount)
    {
        return _log.GetLastEntries(entriesCount);
    }

    public State ApplyCommitted()
    {
        var state = StateMachine.CurrentState;
        while (LastApplied < CommitIndex)
        {
            LastApplied++;
            var entryToApply = _log.GetItemAt(LastApplied)!; 
            state = StateMachine.ApplyCommands([entryToApply.Command]);
        }
        return state;
    }
}