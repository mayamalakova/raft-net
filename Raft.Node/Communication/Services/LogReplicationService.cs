using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Node.Communication.Services;

public class LogReplicationService(
    INodeStateStore stateStore,
    IClientPool clientPool,
    IClusterNodeStore nodesStore,
    string nodeName,
    int timeoutInterval
    ) : CommandSvc.CommandSvcBase, INodeService
{
    public IAppendEntriesRequestFactory EntriesRequestFactory { get; init; } =
        new AppendEntriesRequestFactory(nodesStore, stateStore, nodeName);

    public override Task<CommandReply> ApplyCommand(CommandRequest request, ServerCallContext context)
    {
        if (stateStore.Role == NodeType.Follower)
        {
            return ForwardCommand(request);
        }

        var command = request.FromMessage();
        stateStore.AppendLogEntry(new LogEntry(command, stateStore.CurrentTerm));
        Console.WriteLine($"{command} appended in term={stateStore.CurrentTerm}. log is {stateStore.PrintLog()}");

        var replies = SendAppendEntriesRequestsAndWaitForResults(
            TimeSpan.FromSeconds(timeoutInterval)).Result;
        UpdateNextLogIndex(replies, 1);

        return Task.FromResult(new CommandReply()
        {
            Result = $"Success at {context.Host}"
        });
    }

    private async Task<IDictionary<string, AppendEntriesReply?>> SendAppendEntriesRequestsAndWaitForResults(TimeSpan timeout)
    {
        var tasks = nodesStore.GetNodes().ToDictionary(
            node => node.NodeName,
            node => TrySendAppendEntriesRequest(node, GetEntriesToSendToNode(node), timeout)
        );

        await Task.WhenAll(tasks.Values.ToArray<Task>());

        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.IsCompletedSuccessfully ? kvp.Value.Result : null
        );
    }

    private IList<LogEntry> GetEntriesToSendToNode(NodeInfo node)
    {
        var nextIndex = nodesStore.GetNextIndex(node.NodeName);
        return stateStore.GetEntriesFromIndex(nextIndex)
            .ToArray();
    }

    private async Task<AppendEntriesReply?> TrySendAppendEntriesRequest(NodeInfo node, IList<LogEntry> entries, TimeSpan timeout)
    {
        try
        {
            return await SendAppendEntriesRequestAsync(node, entries.Select(e => e.ToMessage())
                .ToArray(), timeout);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            Console.WriteLine($"Timeout occurred for node {node.NodeName}: {ex.GetType()} {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred for node {node.NodeName}: {ex.GetType()} {ex.Message}");
            return null;
        }
    }

    private async Task<AppendEntriesReply> SendAppendEntriesRequestAsync(NodeInfo follower,
        IList<LogEntryMessage> entries, TimeSpan timeout)
    {
        var appendEntriesRequest = EntriesRequestFactory.CreateRequest(follower.NodeName, entries);
        var deadline = DateTime.UtcNow.Add(timeout);
        var reply = await clientPool.GetAppendEntriesClient(follower.NodeAddress)
            .AppendEntriesAsync(appendEntriesRequest, new CallOptions(deadline: deadline));
        return reply;
    }

    private void UpdateNextLogIndex(IDictionary<string, AppendEntriesReply?> replies, int entriesCount)
    {
        foreach (var (nodeName, reply) in replies)
        {
            Console.WriteLine($"{nodeName}: {reply}");
            if (reply == null)
            {
                continue;
            }

            if (reply.Success)
            {
                nodesStore.IncreaseLastLogIndex(nodeName, entriesCount);
            }
            else
            {
                nodesStore.DecreaseLastLogIndex(nodeName);
            }
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

public interface IAppendEntriesRequestFactory
{
    AppendEntriesRequest CreateRequest(string nodeName, IList<LogEntryMessage> entries);
}

public class AppendEntriesRequestFactory(IClusterNodeStore nodeStore, INodeStateStore stateStore, string leaderName)
    : IAppendEntriesRequestFactory
{
    public AppendEntriesRequest CreateRequest(string nodeName, IList<LogEntryMessage> entries)
    {
        var lastLogIndex = nodeStore.GetNextIndex(nodeName) - 1;
        return new AppendEntriesRequest()
        {
            Term = stateStore.CurrentTerm,
            LeaderCommit = stateStore.CommitIndex,
            LeaderId = leaderName,
            PrevLogIndex = lastLogIndex,
            PrevLogTerm = stateStore.GetTermAtIndex(lastLogIndex),
            Entries = { entries }
        };
    }
}