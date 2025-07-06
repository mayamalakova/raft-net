using Raft.Node.Communication.Client;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Election;

public interface IElectionManager
{
    void StartElection();
}

public class ElectionManager : IElectionManager
{
    private readonly string nodeName;
    private readonly IClusterNodeStore clusterStore;
    private readonly INodeStateStore stateStore;
    private readonly IClientPool clientPool;
    private readonly IElectionResultsReceiver _resultsReceiver;

    public ElectionManager(
        string nodeName,
        IClusterNodeStore clusterStore,
        INodeStateStore stateStore,
        IClientPool clientPool,
        IElectionResultsReceiver resultsReceiver)
    {
        this.nodeName = nodeName;
        this.clusterStore = clusterStore;
        this.stateStore = stateStore;
        this.clientPool = clientPool;
        this._resultsReceiver = resultsReceiver;
    }

    public void StartElection()
    {
        var tasks = clusterStore.GetNodes()
            .ToDictionary(
                node => node.NodeName,
                node => SendRequestForVote(node, stateStore.CurrentTerm)
            );
        
        var collectVotesWithinElectionTimeout = Task.WhenAny(
            Task.WhenAll(tasks.Values),
            Task.Delay(TimeSpan.FromSeconds(5))
        );
        
        collectVotesWithinElectionTimeout.Wait();
        
        var (repliesReceived, higherTermReceived) = CountAndLogVotes(tasks);
        Log.Information("{NodeName} received {RepliesReceived} replies out of {TasksCount} nodes", nodeName, repliesReceived, tasks.Count);
        
        // If we received a higher term, step down immediately
        if (higherTermReceived)
        {
            var highestTerm = tasks
                .Where(t => t.Value.IsCompletedSuccessfully)
                .Max(t => t.Value.Result.Term);
            _resultsReceiver.OnHigherTermReceivedWithVoteReply(highestTerm);
            return;
        }
        
        ProcessElectionResult(repliesReceived, tasks.Count + 1);
    }

    private void ProcessElectionResult(int votesReceived, int totalNodes)
    {
        var totalVotes = votesReceived + 1; // +1 for self
        var votesNeeded = (totalNodes / 2) + 1; // Majority
        
        Log.Information("{NodeName} election result: {TotalVotes}/{TotalNodes} votes (need {VotesNeeded} for majority)", nodeName, totalVotes, totalNodes, votesNeeded);
        if (totalVotes >= votesNeeded)
        {
            Log.Information("{NodeName} won election with {TotalVotes}/{TotalNodes} votes", nodeName, totalVotes, totalNodes);
            _resultsReceiver.OnElectionWon();
        }
        else
        {
            Log.Information("{NodeName} lost election with {TotalVotes}/{TotalNodes} votes, becoming follower", nodeName, totalVotes, totalNodes);
            _resultsReceiver.OnElectionLost();
        }
    }

    private (int votesReceived, bool higherTermReceived) CountAndLogVotes(Dictionary<string, Task<RequestForVoteReply>> taskEntries)
    {
        var failed = taskEntries.Where(t => !t.Value.IsCompletedSuccessfully);
        var notGranted = taskEntries.Where(t => t.Value is { IsCompletedSuccessfully: true, Result.VoteGranted: false });
        var granted = taskEntries.Where(t => t.Value is { IsCompletedSuccessfully: true, Result.VoteGranted: true }).ToArray();
        
        failed.ToList().ForEach(t => Log.Information("Failed to get vote response for vote to {NodeName}", t.Key));
        notGranted.ToList().ForEach(t => Log.Information("Vote not granted for {NodeName}", t.Key));
        granted.ToList().ForEach(t => Log.Information("Vote granted by {NodeName}", t.Key));

        // Check if any reply contained a higher term than our current term
        var higherTermReceived = taskEntries.Any(t => 
            t.Value.IsCompletedSuccessfully && 
            t.Value.Result.Term > stateStore.CurrentTerm);

        return (granted.Count(), higherTermReceived);
    }

    private Task<RequestForVoteReply> SendRequestForVote(NodeInfo node, int term)
    {
        var request = new RequestForVoteMessage() { CandidateId = nodeName, Term = term};
        var client = clientPool.GetRequestForVoteClient(node.NodeAddress);
        return client.RequestVoteAsync(request).ResponseAsync;
    }
}