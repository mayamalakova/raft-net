using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class RequestVoteService : RequestForVoteSvc.RequestForVoteSvcBase, INodeService
{
    private readonly INodeStateStore _stateStore;
    private readonly IRaftNode _node;

    public RequestVoteService(INodeStateStore stateStore, IRaftNode node)
    {
        _stateStore = stateStore;
        _node = node;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return RequestForVoteSvc.BindService(this);
    }

    public override Task<RequestForVoteReply> RequestVote(RequestForVoteMessage request, ServerCallContext context)
    {
        // 1. Reply false if term < currentTerm
        if (request.Term < _stateStore.CurrentTerm)
        {
            Log.Information("Vote request (from: {candidateId}, term: {requestTerm}) - result: Rejecting as currentTerm is higher (currentTerm={currentTerm})", 
                request.CandidateId, 
                request.Term, 
                _stateStore.CurrentTerm);
            return SendRejected();
        }

        // 2. If term > currentTerm, update term, become follower and grant vote 
        if (request.Term > _stateStore.CurrentTerm)
        {
            Log.Information("Vote request (from: {candidateId}, term: {requestTerm}) - result: Granting vote, as request term is higher than current term {currentTerm}",
                request.CandidateId, request.Term, _stateStore.CurrentTerm);
            _stateStore.VotedFor = request.CandidateId;
            _stateStore.LastVoteTerm = _stateStore.CurrentTerm;
            
            _node.BecomeFollower(null, _stateStore.CurrentTerm);
            
            return SendGranted();       
        }
        
        // 3. Reply false if already voted for this term
        if (_stateStore.LastVoteTerm == request.Term && _stateStore.VotedFor != null)
        {
            Log.Information("Vote request (from: {candidateId}, term: {requestTerm}) - result: Rejecting as already voted for {votedFor} in term {lastVotedTerm}", 
                request.CandidateId, 
                request.Term, 
                _stateStore.VotedFor, 
                _stateStore.LastVoteTerm);
            return SendRejected();       
        }

        // 4. Grant vote and update vote state
        Log.Information("Vote request (from: {candidateId}, term: {requestTerm}) - result: Granting vote", request.CandidateId, request.Term);
        _stateStore.VotedFor = request.CandidateId;
        _stateStore.LastVoteTerm = _stateStore.CurrentTerm;

        return SendGranted();
    }

    private Task<RequestForVoteReply> SendGranted()
    {
        var reply = new RequestForVoteReply
        {
            Term = _stateStore.CurrentTerm,
            VoteGranted = true
        };

        return Task.FromResult(reply);
    }

    private Task<RequestForVoteReply> SendRejected()
    {
        var rejectReply = new RequestForVoteReply
        {
            Term = _stateStore.CurrentTerm,
            VoteGranted = false
        };
        return Task.FromResult(rejectReply);
    }
}