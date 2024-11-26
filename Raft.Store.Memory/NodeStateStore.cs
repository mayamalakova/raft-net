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

    public void AppendLogEntry(Command command, int term)
    {
        _log.Append(new LogEntry(command, term));
    }

    public string PrintLog()
    {
        return string.Join(", ", _log.Entries.Select(x => x.Command));
    }

    public int GetTermAtIndex(int lastLogIndex)
    {
        return _log.GetItemAt(lastLogIndex)?.Term ?? -1;
    }
}