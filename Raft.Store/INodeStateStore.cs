using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

// ReSharper disable UnusedMemberInSuper.Global

namespace Raft.Store;

public interface INodeStateStore
{
    NodeType Role { get; set; }
    NodeAddress? LeaderAddress { get; set; }
    int CurrentTerm { get; set; }
    int CommitIndex { get; set; }
    int LogLength { get; }

    void AppendLogEntry(Command command, int term);
    string PrintLog();
    int GetTermAtIndex(int lastLogIndex);
    LogEntry? EntryAtIndex(int index);
}