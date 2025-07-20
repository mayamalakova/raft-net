using Raft.Store.Domain;

namespace Raft.Node.Communication.Client;

public interface IClientPool
{
    CommandSvc.CommandSvcClient GetCommandServiceClient(NodeAddress targetAddress);
    LeaderDiscoverySvc.LeaderDiscoverySvcClient GetLeaderDiscoveryClient(NodeAddress targetAddress);
    RegisterNodeSvc.RegisterNodeSvcClient GetRegisterNodeClient(NodeAddress targetAddress);
    AppendEntriesSvc.AppendEntriesSvcClient GetAppendEntriesClient(NodeAddress targetAddress);
    
    RequestForVoteSvc.RequestForVoteSvcClient GetRequestForVoteClient(NodeAddress targetAddress);
}