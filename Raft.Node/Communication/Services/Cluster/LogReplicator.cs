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
    IClusterNodeStore clusterStore,
    string nodeName,
    int timeoutSeconds)
{
    public IAppendEntriesRequestFactory EntriesRequestFactory { get; init; } =
        new AppendEntriesRequestFactory(clusterStore, stateStore, nodeName);

    public void ReplicateToFollowers()
    {
        var replies = SendAppendEntriesRequestsAndWaitForResults().Result;
        UpdateClusterState(replies);
        UpdateCommitIndex();
        Console.WriteLine($"Replicated to followers - nextIndex: {clusterStore.GetNextIndexesPrintable()}");
    }

    private async Task<IDictionary<string, AppendEntriesReply?>> SendAppendEntriesRequestsAndWaitForResults()
    {
        var tasks = clusterStore.GetNodes()
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
        var nextIndex = clusterStore.GetNextIndex(nodeName);
        return nextIndex == stateStore.LogLength;
    }

    private void UpdateClusterState(IDictionary<string, AppendEntriesReply?> replies)
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
                clusterStore.IncreaseNextLogIndex(nodeName, entriesCount);
                clusterStore.IncreaseMatchingIndex(nodeName, entriesCount);
            }
            else
            {
                clusterStore.DecreaseNextLogIndex(nodeName);
            }
        }
    }

    private void UpdateCommitIndex()
    {
        var nodes = clusterStore.GetNodes().ToArray();
        var nodesCount = nodes.Count();
        var matchingIndexes = nodes.Select(x => clusterStore.GetMatchingIndex(x.NodeName)).ToArray();
        var current = stateStore.CommitIndex;
        while (matchingIndexes.Count(x => x >= current) > nodesCount / 2) current++;
        current--;
        if (current > stateStore.CommitIndex)
        {
            stateStore.CommitIndex = current;
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
        var nextIndex = clusterStore.GetNextIndex(nodeName);
        var logLength = stateStore.LogLength;
        return logLength - nextIndex;
    }
    
}