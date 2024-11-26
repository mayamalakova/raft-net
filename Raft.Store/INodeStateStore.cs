using Raft.Store.Domain;
// ReSharper disable UnusedMemberInSuper.Global

namespace Raft.Store;

public interface INodeStateStore
{
    NodeType Role { get; set; }
    NodeAddress? LeaderAddress { get; set; }
    int CurrentTerm { get; set; }
    int CommitIndex { get; set; }

    void AppendLogEntry(Command command, int term);
    string PrintLog();
    int GetTermAtIndex(int lastLogIndex);
}