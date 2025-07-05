using Grpc.Core;
using Raft.Communication.Contract;
using Serilog;

namespace Raft.Node.Communication.Services.Cluster;

public class RequestVoteService: RequestForVoteSvc.RequestForVoteSvcBase, INodeService
{
    public ServerServiceDefinition GetServiceDefinition()
    {
        return RequestForVoteSvc.BindService(this);
    }

    public override Task<RequestForVoteReply> RequestVote(RequestForVoteMessage request, ServerCallContext context)
    {
        Log.Information($"Received vote request from {request.CandidateId}");
        return Task.FromResult(new RequestForVoteReply());
    }
}