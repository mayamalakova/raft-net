using Raft.Node.Communication.Client;
using Raft.Store;
using Serilog;

namespace Raft.Node.Election;

public interface IElectionManager
{
    Task StartElectionAsync(int termAtElectionStart);
}

public class ElectionResult
{
    public int YesVotes { get; }
    public int NoVotes { get; }
    public bool WonElection { get; }
    public bool HigherTerm { get; }

    public int TermReceived { get; }

    private ElectionResult(int yesVotes, int noVotes, bool wonElection, bool higherTerm, int termReceived = 0)
    {
        YesVotes = yesVotes;
        NoVotes = noVotes;
        WonElection = wonElection;
        HigherTerm = higherTerm;
        TermReceived = termReceived;
    }

    public static ElectionResult Lost(int noVotes) => new(0, noVotes, false, false);
    public static ElectionResult Won(int yesVotes) => new(yesVotes, 0, true, false);
    public static ElectionResult HigherTermReceived(int replyTerm) => new(0, 0, false, true, replyTerm);
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
        var totalNodes = _clusterStore.GetNodes().Count() + 1; // +1 for self
        var yesVotes = 0;
        var noVotes = 0;
        var votesNeeded = (totalNodes / 2) + 1; // Majority
        var votesLock = new object();

        var taskCompletionSource = new TaskCompletionSource<ElectionResult>();
        foreach (var node in _clusterStore.GetNodes())
        {
            Task.Run(async () =>
            {
                var request = new RequestForVoteMessage() { CandidateId = _nodeName, Term = termAtElectionStart };
                var client = _clientPool.GetRequestForVoteClient(node.NodeAddress);
                var reply = await client.RequestVoteAsync(request).ResponseAsync;

                if (reply.Term > termAtElectionStart)
                {
                    taskCompletionSource.TrySetResult(ElectionResult.HigherTermReceived(reply.Term));
                    return;
                }

                lock (votesLock)
                {
                    if (reply.VoteGranted)
                    {
                        yesVotes++;
                    }
                    else
                    {
                        noVotes++;
                    }

                    if (yesVotes >= votesNeeded)
                    {
                        taskCompletionSource.TrySetResult(ElectionResult.Won(yesVotes));
                    }
                    else if (noVotes >= votesNeeded)
                    {
                        taskCompletionSource.TrySetResult(ElectionResult.Lost(noVotes));
                    }
                }
            });
        }

        var electionTask = await Task.WhenAny(
            taskCompletionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        if (electionTask == taskCompletionSource.Task)
        {
            var electionResult = taskCompletionSource.Task.Result;
            ProcessResult(termAtElectionStart, electionResult, totalNodes);
        }
        else
        {
            Log.Information("{NodeName} lost election due to election timeout", _nodeName);
            _resultsReceiver.OnElectionLost(termAtElectionStart);
        }
    }

    private void ProcessResult(int termAtElectionStart, ElectionResult electionResult, int totalNodes)
    {
        if (electionResult.HigherTerm)
        {
            _resultsReceiver.OnHigherTermReceivedWithVoteReply(termAtElectionStart, electionResult.TermReceived);
            return;
        }
        if (electionResult.WonElection)
        {
            Log.Information("{NodeName} won election with {YesVotes}/{TotalNodes} votes granted, becoming leader",
                _nodeName, electionResult.YesVotes, totalNodes);
            _resultsReceiver.OnElectionWon(termAtElectionStart);
        }
        else
        {
            Log.Information(
                "{NodeName} lost election with {NoVotes}/{TotalNodes} votes refused, becoming follower",
                _nodeName, electionResult.NoVotes, totalNodes);
            _resultsReceiver.OnElectionLost(termAtElectionStart);
        }
    }
}