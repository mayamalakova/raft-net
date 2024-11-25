using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Communication;

public class LogReplicationService(INodeStateStore stateStore) : CommandSvc.CommandSvcBase, INodeService
{
    public override Task<CommandReply> ApplyCommand(CommandRequest request, ServerCallContext context)
    {
        if (stateStore.Role == NodeType.Follower)
        {
            return ForwardCommand(request);
        }

        return Task.FromResult(new CommandReply()
        {
            Result = $"Success at {context.Host}"
        });
    }

    private Task<CommandReply> ForwardCommand(CommandRequest request)
    {
        if (stateStore.LeaderAddress == null)
        {
            return Task.FromResult(new CommandReply()
            {
                Result = "Failed to forward to leader"
            });
        }

        var channel = new Channel(stateStore.LeaderAddress.Host, stateStore.LeaderAddress.Port,
            ChannelCredentials.Insecure);
        var commandClient = new CommandSvc.CommandSvcClient(channel);
        var reply = commandClient.ApplyCommand(request);
        return Task.FromResult(reply);
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}