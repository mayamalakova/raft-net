using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class RequestVoteService : RequestForVoteSvc.RequestForVoteSvcBase, INodeService
{
    private readonly INodeStateStore _stateStore;

    public RequestVoteService(INodeStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return RequestForVoteSvc.BindService(this);
    }

    public override Task<RequestForVoteReply> RequestVote(RequestForVoteMessage request, ServerCallContext context)
    {
        Log.Information($"Received vote request from {request.CandidateId} for term {request.Term}");

        // 1. Reply false if term < currentTerm
        if (request.Term < _stateStore.CurrentTerm)
        {
            Log.Information($"Rejecting vote request from {request.CandidateId} - request term {request.Term} < current term {_stateStore.CurrentTerm}");
            return SendRejected();
        }

        // 2. Reply false if already voted for this term
        if (_stateStore.LastVoteTerm == _stateStore.CurrentTerm && _stateStore.VotedFor != null)
        {
            Log.Information($"Rejecting vote request from {request.CandidateId} - already voted for {_stateStore.VotedFor} in term {_stateStore.CurrentTerm}");
            return SendRejected();       
        }

        // 3. If term > currentTerm, update term (vote state will be set below)
        if (request.Term > _stateStore.CurrentTerm)
        {
            Log.Information($"Received vote request with higher term {request.Term} > {_stateStore.CurrentTerm}, updating term");
            _stateStore.CurrentTerm = request.Term;
        }

        // 4. Grant vote and update vote state
        Log.Information($"Granting vote to {request.CandidateId} for term {request.Term}");
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

        Log.Information($"Sending vote reply: Term={reply.Term}, VoteGranted={reply.VoteGranted}");
        return Task.FromResult(reply);
    }

    private Task<RequestForVoteReply> SendRejected()
    {
        var rejectReply = new RequestForVoteReply
        {
            Term = _stateStore.CurrentTerm,
            VoteGranted = false
        };
        Log.Information($"Sending vote reply: Term={rejectReply.Term}, VoteGranted={rejectReply.VoteGranted}");
        return Task.FromResult(rejectReply);
    }
}