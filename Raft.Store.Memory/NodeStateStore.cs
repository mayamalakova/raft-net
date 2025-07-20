using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Store.Memory;

public class NodeStateStore : INodeStateStore
{
    private readonly object _lock = new();
    private readonly ReplicationLog _log = new();

    private NodeType _role;
    public NodeType Role
    {
        get { lock (_lock) { return _role; } }
        set { lock (_lock) { _role = value; } }
    }

    private NodeInfo? _leaderInfo;
    public NodeInfo? LeaderInfo
    {
        get { lock (_lock) { return _leaderInfo; } }
        set { lock (_lock) { _leaderInfo = value; } }
    }

    private int _currentTerm;
    public int CurrentTerm
    {
        get { lock (_lock) { return _currentTerm; } }
        set { lock (_lock) { _currentTerm = value; } }
    }

    private int _commitIndex = -1;
    public int CommitIndex
    {
        get { lock (_lock) { return _commitIndex; } }
        set { lock (_lock) { _commitIndex = value; } }
    }

    private int _lastApplied = -1;
    public int LastApplied
    {
        get { lock (_lock) { return _lastApplied; } }
        set { lock (_lock) { _lastApplied = value; } }
    }

    public StateMachine StateMachine { get; init; } = new();
    public int LogLength { get { lock (_lock) { return _log.Length; } } }

    private string? _votedFor;
    public string? VotedFor
    {
        get { lock (_lock) { return _votedFor; } }
        set { lock (_lock) { _votedFor = value; } }
    }

    private int _lastVoteTerm = -1;
    public int LastVoteTerm
    {
        get { lock (_lock) { return _lastVoteTerm; } }
        set { lock (_lock) { _lastVoteTerm = value; } }
    }

    public void AppendLogEntry(LogEntry entry)
    {
        lock (_lock) { _log.Append(entry); }
    }

    public string PrintLog()
    {
        lock (_lock) { return _log.ToString(); }
    }

    public int GetTermAtIndex(int lastLogIndex)
    {
        lock (_lock) { return _log.GetItemAt(lastLogIndex)?.Term ?? -1; }
    }

    public LogEntry? EntryAtIndex(int index)
    {
        lock (_lock) { return _log.GetItemAt(index); }
    }

    public void RemoveLogEntriesFrom(int index)
    {
        lock (_lock) { _log.RemoveFrom(index); }
    }

    public IList<LogEntry> GetLastEntries(int entriesCount)
    {
        lock (_lock) { return _log.GetLastEntries(entriesCount); }
    }

    public State ApplyCommitted()
    {
        lock (_lock)
        {
            var state = StateMachine.CurrentState;
            while (_lastApplied < _commitIndex)
            {
                _lastApplied++;
                var entryToApply = _log.GetItemAt(_lastApplied)!;
                state = StateMachine.ApplyCommands([entryToApply.Command]);
            }
            return state;
        }
    }

    public void CheckTermAndRoleAndDo(int expectedTerm, NodeType expectedRole, Action onSuccess, Action onFailure)
    {
        lock (_lock)
        {
            if (_currentTerm == expectedTerm && _role == expectedRole)
            {
                onSuccess();
            }
            else
            {
                onFailure();
            }
        }
    }
    
}