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
        if (stateStore.GetTermAtIndex(request.PrevLogIndex) == request.PrevLogTerm)
        {
            foreach (var entry in request.EntryCommands)
            {
                stateStore.AppendLogEntry(entry.ToCommand(), request.Term);
            }
            return Task.FromResult(new AppendEntriesReply
            {
                Term = stateStore.CurrentTerm,
                Success = true
            });
        }
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