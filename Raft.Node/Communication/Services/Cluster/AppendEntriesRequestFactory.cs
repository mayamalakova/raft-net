using Raft.Store;

namespace Raft.Node.Communication.Services.Cluster;

public class AppendEntriesRequestFactory(IClusterNodeStore nodeStore, INodeStateStore stateStore, string leaderName)
    : IAppendEntriesRequestFactory
{
    public AppendEntriesRequest CreateRequest(string nodeName, IList<LogEntryMessage> entries)
    {
        var lastLogIndex = nodeStore.GetNextIndex(nodeName) - 1;
        return new AppendEntriesRequest()
        {
            Term = stateStore.CurrentTerm,
            LeaderCommit = stateStore.CommitIndex,
            LeaderId = leaderName,
            PrevLogIndex = lastLogIndex,
            PrevLogTerm = stateStore.GetTermAtIndex(lastLogIndex),
            Entries = { entries }
        };
    }
}