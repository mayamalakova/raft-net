using Grpc.Core;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;

namespace Raft.Node.Communication.Services.Cluster;

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
        UpdateNextLogIndex(replies);
        Console.WriteLine($"Replicated to followers - nextIndex: {nodesStore.GetNextIndexesPrintable()}");
    }

    private async Task<IDictionary<string, AppendEntriesReply?>> SendAppendEntriesRequestsAndWaitForResults()
    {
        var tasks = nodesStore.GetNodes()
            .ToDictionary(
                node => node.NodeName,
                node => TrySendAppendEntriesRequest(node, GetEntriesToSendToNode(node.NodeName))
            );

        await Task.WhenAll(tasks.Values.ToArray<Task>());

        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.IsCompletedSuccessfully ? kvp.Value.Result : null
        );
    }

    public bool IsReplicationComplete(string nodeName)
    {
        var nextIndex = nodesStore.GetNextIndex(nodeName);
        return nextIndex == stateStore.LogLength;
    }

    private void UpdateNextLogIndex(IDictionary<string, AppendEntriesReply?> replies)
    {
        foreach (var (nodeName, reply) in replies)
        {
            Console.WriteLine($"{nodeName}: {reply}");
            if (reply == null)
            {
                continue;
            }

            var entriesCount = GetNumberOfEntriesToReplicate(nodeName);
            if (reply.Success)
            {
                nodesStore.IncreaseNextLogIndex(nodeName, entriesCount);
                nodesStore.IncreaseMatchingIndex(nodeName, entriesCount);
            }
            else
            {
                nodesStore.DecreaseNextLogIndex(nodeName);
                nodesStore.DecreaseMatchingIndex(nodeName);
            }
        }
    }

    private async Task<AppendEntriesReply?> TrySendAppendEntriesRequest(NodeInfo node, IList<LogEntry> entries)
    {
        Console.WriteLine($"Sending append entries to node {node.NodeName} - {entries.Count} entries");
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
        catch (RpcException ex)
        {
            Console.WriteLine($"Could not connect to {node.NodeName}: {ex.GetType()}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred for node {node.NodeName}: {ex.GetType()} {ex.StackTrace}");
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

    private IList<LogEntry> GetEntriesToSendToNode(string nodeName)
    {
        var entriesCount = GetNumberOfEntriesToReplicate(nodeName);
        return stateStore.GetLastEntries(entriesCount);
    }

    private int GetNumberOfEntriesToReplicate(string nodeName)
    {
        var nextIndex = nodesStore.GetNextIndex(nodeName);
        var logLength = stateStore.LogLength;
        return logLength - nextIndex;
    }
    
}