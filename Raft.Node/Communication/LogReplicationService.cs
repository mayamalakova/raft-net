using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Communication;

public class LogReplicationService(INodeStateStore stateStore) : CommandSvc.CommandSvcBase, INodeService
{
    private readonly Dictionary<NodeAddress, Channel> _channels = new();
    
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

        // TODO does this need some thread safety ?
        var channel = _channels.TryGetValue(stateStore.LeaderAddress, out var existingChannel)
            ? existingChannel
            : new Channel(stateStore.LeaderAddress.Host, stateStore.LeaderAddress.Port,
                ChannelCredentials.Insecure);
        _channels[stateStore.LeaderAddress] = channel;
        
        var commandClient = new CommandSvc.CommandSvcClient(channel);
        return commandClient.ApplyCommandAsync(request).ResponseAsync;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}