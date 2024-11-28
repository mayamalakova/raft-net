using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Extensions;
using Raft.Store;

namespace Raft.Node.Communication.Services;

public class AppendEntriesService(INodeStateStore stateStore) : AppendEntriesSvc.AppendEntriesSvcBase, INodeService
{
    public override Task<AppendEntriesReply> AppendEntries(AppendEntriesRequest request, ServerCallContext context)
    {
        Console.WriteLine($"Appending entries {request}");
        if (stateStore.CurrentTerm > request.Term || stateStore.GetTermAtIndex(request.PrevLogIndex) != request.PrevLogTerm)
        {
            return Task.FromResult(new AppendEntriesReply
            {
                Term = stateStore.CurrentTerm,
                Success = false
            });
        }

        foreach (var entry in request.EntryCommands)
        {
            stateStore.AppendLogEntry(entry.ToCommand(), request.Term);
        }

        if (request.LeaderCommit > stateStore.CommitIndex)
        {
            stateStore.CommitIndex = Math.Min(request.LeaderCommit, stateStore.LogLength - 1);
        }
        return Task.FromResult(new AppendEntriesReply
        {
            Term = stateStore.CurrentTerm,
            Success = true
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return AppendEntriesSvc.BindService(this);
    }
}