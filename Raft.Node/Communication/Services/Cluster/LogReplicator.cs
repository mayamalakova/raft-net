using Grpc.Core;
using Raft.Node.Communication.Client;
using Raft.Node.Extensions;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class LogReplicator(
    INodeStateStore stateStore,
    IClientPool clientPool,
    IClusterNodeStore clusterStore,
    string nodeName,
    int replicationTimeoutSeconds)
{
    public IAppendEntriesRequestFactory EntriesRequestFactory { get; init; } =
        new AppendEntriesRequestFactory(clusterStore, stateStore, nodeName);

    public void ReplicateToFollowers()
    {
        if (!clusterStore.GetNodes().Any()) return;
        var replies = SendAppendEntriesRequestsAndWaitForResults().Result;
        UpdateClusterState(replies);
        Log.Debug($"Replicated to followers - nextIndex: {clusterStore.GetNextIndexesPrintable()}");
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
            Log.Debug($"Reply from {nodeName}: {reply}");
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

    private async Task<AppendEntriesReply?> TrySendAppendEntriesRequest(NodeInfo node, IList<LogEntry> entries)
    {
        Log.Debug($"Sending append entries to node {node.NodeName} - {entries.Count} entries, commitIndex={stateStore.CommitIndex}");
        try
        {
            return await SendAppendEntriesRequestAsync(node, entries.Select(e => e.ToMessage())
                .ToArray(), TimeSpan.FromSeconds(replicationTimeoutSeconds));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            Log.Information($"Timeout occurred for node {node.NodeName}: {ex.GetType()} {ex.Message}");
            return null;
        }
        catch (RpcException ex)
        {
            Log.Information($"Could not connect to {node.NodeName}: {ex.GetType()} {ex.Message} {ex.StackTrace}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Information($"Error occurred for node {node.NodeName}: {ex.GetType()} {ex.StackTrace} {ex.GetBaseException()} {ex.InnerException}");
            return null;
        }
    }

    private async Task<AppendEntriesReply> SendAppendEntriesRequestAsync(NodeInfo follower,
        IList<LogEntryMessage> entries, TimeSpan timeout)
    {
        var appendEntriesRequest = EntriesRequestFactory.CreateRequest(follower.NodeName, entries);
        var deadline = DateTime.UtcNow.Add(timeout);
        Log.Debug($"awaiting GetAppendEntriesClient {follower.NodeName}");
        var reply = await clientPool.GetAppendEntriesClient(follower.NodeAddress)
            .AppendEntriesAsync(appendEntriesRequest, new CallOptions(deadline: deadline));
        Log.Debug($"got reply to GetAppendEntriesClient from {follower.NodeName}");
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