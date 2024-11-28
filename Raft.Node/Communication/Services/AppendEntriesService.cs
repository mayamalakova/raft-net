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
        // 1. reply false if term < currentTerm
        // 2. reply false if log doesn't contain an entry at prevLogIndex whose term matches prevLogTerm
        if (stateStore.CurrentTerm > request.Term || stateStore.GetTermAtIndex(request.PrevLogIndex) != request.PrevLogTerm)
        {
            return Fail();
        }
        // TODO 3. if an existing entry conflicts with a new one (same index, different term) delete it and all that follow
        // 4. append any new entries not already in the log
        foreach (var entry in request.EntryCommands)
        {
            stateStore.AppendLogEntry(entry.ToCommand(), request.Term);
        }
        // 5. update commitIndex
        if (request.LeaderCommit > stateStore.CommitIndex)
        {
            stateStore.CommitIndex = Math.Min(request.LeaderCommit, stateStore.LogLength - 1);
        }
        return Success();
    }

    private Task<AppendEntriesReply> Success()
    {
        return Task.FromResult(new AppendEntriesReply
        {
            Term = stateStore.CurrentTerm,
            Success = true
        });
    }

    private Task<AppendEntriesReply> Fail()
    {
        return Task.FromResult(new AppendEntriesReply
        {
            Term = stateStore.CurrentTerm,
            Success = false
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return AppendEntriesSvc.BindService(this);
    }
}