using System.Collections.Concurrent;
using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Extensions;

namespace Raft.Node.Communication.Services;

public class LogReplicationService(INodeStateStore stateStore, IClientPool clientPool, IClusterNodeStore nodesStore, string nodeName) : CommandSvc.CommandSvcBase, INodeService
{

    public override Task<CommandReply> ApplyCommand(CommandRequest request, ServerCallContext context)
    {
        if (stateStore.Role == NodeType.Follower)
        {
            return ForwardCommand(request);
        }

        var command = new Command(request.Variable, request.Operation.ToOperationType(), request.Literal);
        stateStore.AppendLogEntry(command, stateStore.CurrentTerm);
        Console.WriteLine($"{command} appended in term={stateStore.CurrentTerm }. log is {stateStore.PrintLog()}");
        
        SendAppendEntriesRequestsAndWaitForResults([request]);

        return Task.FromResult(new CommandReply()
        {
            Result = $"Success at {context.Host}"
        });
    }

    private void SendAppendEntriesRequestsAndWaitForResults(IList<CommandRequest> entries)
    {
        ConcurrentDictionary<string, AppendEntriesReply> results = new();
        Parallel.ForEach(nodesStore.GetNodes(), follower =>
        {
            var lastLogIndex = nodesStore.GetLastLogIndex(follower.NodeName);
            var appendEntriesRequest = new AppendEntriesRequest()
            {
                Term = stateStore.CurrentTerm,
                LeaderCommit = stateStore.CommitIndex,
                LeaderId = nodeName,
                PrevLogIndex = lastLogIndex,
                PrevLogTerm = stateStore.GetTermAtIndex(lastLogIndex),
                EntryCommands = { entries }
            };
            var reply = clientPool.GetAppendEntriesClient(follower.NodeAddress).AppendEntries(appendEntriesRequest);
            results[follower.NodeName] = reply;
        });
        foreach (var result in results)
        {
            Console.WriteLine($"{result.Key}: {result.Value}");
        }
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