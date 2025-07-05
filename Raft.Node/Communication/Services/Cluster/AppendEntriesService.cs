using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.Election;
using Raft.Node.Extensions;
using Raft.Store;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class AppendEntriesService(INodeStateStore stateStore, IRaftNode node, ILeaderPresenceTracker leaderPresenceTracker, string nodeName)
    : AppendEntriesSvc.AppendEntriesSvcBase, INodeService
{
    public override Task<AppendEntriesReply> AppendEntries(AppendEntriesRequest request, ServerCallContext context)
    {
        if (stateStore.CurrentTerm < request.Term)
        {
            node.BecomeFollower(request.LeaderId, request.Term);
            return Fail();
        }

        leaderPresenceTracker.Reset();
        Log.Debug($"Appending {request.Entries.Count} entries {request}");
        // 1. reply false if term < currentTerm
        // 2. reply false if log doesn't contain an entry at prevLogIndex whose term matches prevLogTerm
        if (stateStore.CurrentTerm > request.Term ||
            stateStore.GetTermAtIndex(request.PrevLogIndex) != request.PrevLogTerm)
        {
            return Fail();
        }

        // 3. if there are entries already on the places where the new ones should be appended - remove them
        RemoveConflictingEntries(request);
        // 4. append all new entries
        foreach (var entryMessage in request.Entries)
        {
            stateStore.AppendLogEntry(entryMessage.FromMessage());
        }

        // 5. update commitIndex
        if (request.LeaderCommit > stateStore.CommitIndex)
        {
            Log.Information(
                $"{nodeName} updating commit index from {stateStore.CommitIndex} to {request.LeaderCommit}");
            stateStore.CommitIndex = Math.Min(request.LeaderCommit, stateStore.LogLength - 1);
        }

        stateStore.ApplyCommitted();
        return Success();
    }

    /// <summary>
    /// This deviates from the original raft definition - it says only to remove from an entry that has conflicting
    /// term on and then add only new entries that aren't already in the log and then append new entries that are not
    /// already in the log.
    /// This removes all entries that are after prevLogIndex regardless of their term and adds all new ones.
    /// The latter approach may do more remove and add operations, but hopefully the state of the log at the end should
    /// be the same
    /// TODO This will not keep the log consistent if the node crashes after removing
    /// </summary>
    private void RemoveConflictingEntries(AppendEntriesRequest request)
    {
        stateStore.RemoveLogEntriesFrom(request.PrevLogIndex + 1);
    }

    private Task<AppendEntriesReply> Success()
    {
        return Task.FromResult(new AppendEntriesReply
        {
            Term = stateStore.CurrentTerm,
            Success = true
        });
    }

    private Task<AppendEntriesReply> Fail()
    {
        return Task.FromResult(new AppendEntriesReply
        {
            Term = stateStore.CurrentTerm,
            Success = false
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return AppendEntriesSvc.BindService(this);
    }
}