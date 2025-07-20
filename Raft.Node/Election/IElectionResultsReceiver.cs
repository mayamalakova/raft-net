namespace Raft.Node.Election;

public interface IElectionResultsReceiver
{
    void OnElectionWon(int termAtElectionStart);
    void OnElectionLost(int termAtElectionStart);
    void OnHigherTermReceivedWithVoteReply(int oldTerm, int newTerm);
} 