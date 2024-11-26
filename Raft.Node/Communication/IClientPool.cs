using Raft.Store.Domain;

namespace Raft.Node.Communication;

public interface IClientPool
{
    CommandSvc.CommandSvcClient GetCommandServiceClient(NodeAddress targetAddress);
    LeaderDiscoverySvc.LeaderDiscoverySvcClient GetLeaderDiscoveryClient(NodeAddress targetAddress);
}