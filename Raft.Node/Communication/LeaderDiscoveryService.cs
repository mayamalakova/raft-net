using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Memory;

namespace Raft.Node.Communication;

public class LeaderDiscoveryService(INodeStateStore stateStore) : LeaderDiscoverySvc.LeaderDiscoverySvcBase, INodeService
{
    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        var leaderAddress = stateStore.LeaderAddress;
        return Task.FromResult(new LeaderQueryReply
        {
            Host = leaderAddress.Host, 
            Port = leaderAddress.Port,
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return LeaderDiscoverySvc.BindService(this);
    }
}