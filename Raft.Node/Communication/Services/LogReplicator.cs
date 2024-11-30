using Grpc.Core;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Node.Communication.Services;

public class LogReplicator(
    INodeStateStore stateStore,
    IClientPool clientPool,
    IClusterNodeStore nodesStore,
    string nodeName,
    int timeoutSeconds)
{
    public IAppendEntriesRequestFactory EntriesRequestFactory { get; init; } =
        new AppendEntriesRequestFactory(nodesStore, stateStore, nodeName);

    public void ReplicateToFollowers()
    {
        var replies = SendAppendEntriesRequestsAndWaitForResults().Result;
        UpdateNextLogIndex(replies, 1);
    }

    private async Task<IDictionary<string, AppendEntriesReply?>> SendAppendEntriesRequestsAndWaitForResults()
    {
        var tasks = nodesStore.GetNodes().ToDictionary(
            node => node.NodeName,
            node => TrySendAppendEntriesRequest(node, GetEntriesToSendToNode(node))
        );

        await Task.WhenAll(tasks.Values.ToArray<Task>());

        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.IsCompletedSuccessfully ? kvp.Value.Result : null
        );
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

    private async Task<AppendEntriesReply?> TrySendAppendEntriesRequest(NodeInfo node, IList<LogEntry> entries)
    {
        try
        {
            return await SendAppendEntriesRequestAsync(node, entries.Select(e => e.ToMessage())
                .ToArray(), TimeSpan.FromSeconds(timeoutSeconds));
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

    private IList<LogEntry> GetEntriesToSendToNode(NodeInfo node)
    {
        var nextIndex = nodesStore.GetNextIndex(node.NodeName);
        return stateStore.GetEntriesFromIndex(nextIndex)
            .ToArray();
    }
}