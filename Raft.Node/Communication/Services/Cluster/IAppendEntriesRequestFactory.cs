namespace Raft.Node.Communication.Services.Cluster;

public interface IAppendEntriesRequestFactory
{
    AppendEntriesRequest CreateRequest(string nodeName, IList<LogEntryMessage> entries);
}