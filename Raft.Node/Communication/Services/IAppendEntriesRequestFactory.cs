namespace Raft.Node.Communication.Services;

public interface IAppendEntriesRequestFactory
{
    AppendEntriesRequest CreateRequest(string nodeName, IList<LogEntryMessage> entries);
}