using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;

namespace Raft.Node.Communication;

public class LogReplicationService(INodeStateStore stateStore) : CommandSvc.CommandSvcBase, INodeService
{
    public override Task<CommandReply> ApplyCommand(CommandRequest request, ServerCallContext context)
    {
        
        return Task.FromResult(new CommandReply()
        {
            Result = "Success"
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}