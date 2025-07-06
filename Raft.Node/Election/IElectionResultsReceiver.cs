namespace Raft.Node.Election;

public interface IElectionResultsReceiver
{
    void OnElectionWon();
    void OnElectionLost();
    void OnHigherTermReceivedWithVoteReply(int newTerm);
} 