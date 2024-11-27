using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Extensions;
using Raft.Store;

namespace Raft.Node.Communication.Services;

public class AppendEntriesService: AppendEntriesSvc.AppendEntriesSvcBase, INodeService
{
    public override Task<AppendEntriesReply> AppendEntries(AppendEntriesRequest request, ServerCallContext context)
    {
        Console.WriteLine($"Appending entries {request}");
        return Task.FromResult(new AppendEntriesReply
        {
            Term = request.Term,
            Success = true
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return AppendEntriesSvc.BindService(this);
    }
}