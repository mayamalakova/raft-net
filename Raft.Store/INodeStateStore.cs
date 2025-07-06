using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

// ReSharper disable UnusedMemberInSuper.Global

namespace Raft.Store;

public interface INodeStateStore
{
    NodeType Role { get; set; }
    NodeInfo? LeaderInfo { get; set; }
    
    int CurrentTerm { get; set; }
    int CommitIndex { get; set; }
    int LogLength { get; }

    void AppendLogEntry(LogEntry entry);
    string PrintLog();
    int GetTermAtIndex(int lastLogIndex);
    LogEntry? EntryAtIndex(int index);
    void RemoveLogEntriesFrom(int index);
    IList<LogEntry> GetLastEntries(int entriesCount);
    public int LastApplied { get; set; }
    public StateMachine StateMachine { get; init; }
    State ApplyCommitted();
    
    string? VotedFor { get; set; }
    int LastVoteTerm { get; set; }

    void CheckTermAndRole(int expectedTerm, NodeType expectedRole, Action onSuccess);
}