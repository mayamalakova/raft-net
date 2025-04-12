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

/// <summary>
/// A GRPC service that processes command requests at a leader node.
/// If the node is a follower, forwards the command to the leader.
/// If the node is a leader:
///  - appends the command to the log
///  - sends it to followers
///  - updates the commit index depending on the replies from the followers
///  - applies any committed commands
///  - replies with the updated state of the state machine
/// </summary>
/// <param name="stateStore"></param>
/// <param name="clusterStore"></param>
/// <param name="clientPool"></param>
/// <param name="logReplicator"></param>
/// <param name="replicationStateManager"></param>
/// <param name="heartBeatRunner"></param>
public class CommandProcessingService(
    INodeStateStore stateStore,
    IClusterNodeStore clusterStore,
    IClientPool clientPool,
    RaftLeaderService leaderService,
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
        var newState = leaderService.ReconcileCluster();
        heartBeatRunner.StartBeating();

        return Task.FromResult(new CommandReply()
        {
            Result = $"Success at {stateStore.LeaderInfo?.NodeName} newState={newState}"
        });
    }

    private Task<CommandReply> ForwardCommand(CommandRequest request)
    {
        if (stateStore.LeaderInfo == null)
        {
            return Task.FromResult(new CommandReply()
            {
                Result = "Failed to forward to leader"
            });
        }

        var commandClient = clientPool.GetCommandServiceClient(stateStore.LeaderInfo.NodeAddress);
        Log.Information($"Forwarding command {request}");
        return commandClient.ApplyCommandAsync(request).ResponseAsync;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return CommandSvc.BindService(this);
    }
}