using Raft.Node.Communication.Client;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Election;

public interface IElectionManager
{
    Task StartElectionAsync(int termAtElectionStart);
}

public class ElectionManager : IElectionManager
{
    private readonly string _nodeName;
    private readonly IClusterNodeStore _clusterStore;
    private readonly IClientPool _clientPool;
    private readonly IElectionResultsReceiver _resultsReceiver;

    public ElectionManager(
        string nodeName,
        IClusterNodeStore clusterStore,
        IClientPool clientPool,
        IElectionResultsReceiver resultsReceiver)
    {
        _nodeName = nodeName;
        _clusterStore = clusterStore;
        _clientPool = clientPool;
        _resultsReceiver = resultsReceiver;
    }

    public async Task StartElectionAsync(int termAtElectionStart)
    {
        var tasks = _clusterStore.GetNodes()
            .ToDictionary(
                node => node.NodeName,
                node => SendRequestForVote(node, termAtElectionStart)
            );
        
        await Task.WhenAny(
            Task.WhenAll(tasks.Values),
            Task.Delay(TimeSpan.FromSeconds(5))
        );
        
        var (repliesReceived, higherTermReceived) = CountAndLogVotes(tasks, termAtElectionStart);
        Log.Information("{NodeName} received {RepliesReceived} replies out of {TasksCount} nodes", _nodeName, 
            repliesReceived, tasks.Count);
        
        // If we received a higher term, step down immediately
        if (higherTermReceived)
        {
            var highestTerm = tasks
                .Where(t => t.Value.IsCompletedSuccessfully)
                .Max(t => t.Value.Result.Term);
            _resultsReceiver.OnHigherTermReceivedWithVoteReply(termAtElectionStart, highestTerm);
            return;
        }
        
        ProcessElectionResult(repliesReceived, tasks.Count + 1, termAtElectionStart);
    }

    private void ProcessElectionResult(int votesReceived, int totalNodes, int term)
    {
        var totalVotes = votesReceived + 1; // +1 for self
        var votesNeeded = (totalNodes / 2) + 1; // Majority
        
        Log.Information("{NodeName} election result: {TotalVotes}/{TotalNodes} votes (need {VotesNeeded} for majority)", 
            _nodeName, totalVotes, totalNodes, votesNeeded);
        if (totalVotes >= votesNeeded)
        {
            Log.Information("{NodeName} won election with {TotalVotes}/{TotalNodes} votes", 
                _nodeName, totalVotes, totalNodes);
            _resultsReceiver.OnElectionWon(term);
        }
        else
        {
            Log.Information("{NodeName} lost election with {TotalVotes}/{TotalNodes} votes, becoming follower", _nodeName, totalVotes, totalNodes);
            _resultsReceiver.OnElectionLost(term);
        }
    }

    private (int votesReceived, bool higherTermReceived) CountAndLogVotes(Dictionary<string, Task<RequestForVoteReply>> taskEntries, int term)
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
            t.Value.Result.Term > term);

        return (granted.Count(), higherTermReceived);
    }

    private Task<RequestForVoteReply> SendRequestForVote(NodeInfo node, int term)
    {
        var request = new RequestForVoteMessage() { CandidateId = _nodeName, Term = term};
        var client = _clientPool.GetRequestForVoteClient(node.NodeAddress);
        return client.RequestVoteAsync(request).ResponseAsync;
    }
}