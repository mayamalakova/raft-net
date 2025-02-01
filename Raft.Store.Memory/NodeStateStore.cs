using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Store.Memory;

public class NodeStateStore : INodeStateStore
{
    private readonly ReplicationLog _log = new();

    public NodeType Role { get; set; }
    public NodeAddress? LeaderAddress { get; set; }
    public int CurrentTerm { get; set; } = 0;
    public int CommitIndex { get; set; } = -1;
    public int LastApplied { get; set; } = -1;

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
            var entryToApply = _log.GetItemAt(LastApplied + 1)!;
            state = StateMachine.ApplyCommands([entryToApply.Command]);
            LastApplied++;
        }
        return state;
    }
}