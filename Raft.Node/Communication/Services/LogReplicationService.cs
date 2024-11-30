using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Node.HeatBeat;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Node.Communication.Services;

public class LogReplicationService(
    INodeStateStore stateStore,
    IClientPool clientPool,
    LogReplicator logReplicator,
    HeartBeatRunner heartBeatRunner) : CommandSvc.CommandSvcBase, INodeService
{
    public override Task<CommandReply> ApplyCommand(CommandRequest request, ServerCallContext context)
    {
        if (stateStore.Role == NodeType.Follower)
        {
            return ForwardCommand(request);
        }

        var command = request.FromMessage();
        stateStore.AppendLogEntry(new LogEntry(command, stateStore.CurrentTerm));
        Console.WriteLine($"{command} appended in term={stateStore.CurrentTerm}. log is {stateStore.PrintLog()}");

        heartBeatRunner.StopBeating();
        logReplicator.ReplicateToFollowers();
        heartBeatRunner.StartBeating();

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

        var commandClient = clientPool.GetCommandServiceClient(stateStore.LeaderAddress);
        Console.WriteLine($"Forwarding command {request}");
        return commandClient.ApplyCommandAsync(request).ResponseAsync;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}