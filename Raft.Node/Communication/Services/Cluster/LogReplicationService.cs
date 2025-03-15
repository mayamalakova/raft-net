using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Node.HeartBeat;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class LogReplicationService(
    INodeStateStore stateStore,
    IClusterNodeStore clusterStore,
    IClientPool clientPool,
    LogReplicator logReplicator,
    ReplicationStateManager replicationStateManager,
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
        Log.Information($"{command} appended in term={stateStore.CurrentTerm}. log is {stateStore.PrintLog()}");

        heartBeatRunner.StopBeating();
        
        logReplicator.ReplicateToFollowers();
        replicationStateManager.UpdateCommitIndex(clusterStore.GetNodes().ToArray());
        var newState = stateStore.ApplyCommitted();
        
        heartBeatRunner.StartBeating();

        return Task.FromResult(new CommandReply()
        {
            Result = $"Success at {context.Host} newState={newState}"
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
        Log.Information($"Forwarding command {request}");
        return commandClient.ApplyCommandAsync(request).ResponseAsync;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}