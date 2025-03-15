using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;

namespace Raft.Node.Communication.Services.Cluster;

/// <summary>
/// Replies to queries asking for the current leader of the cluster
/// </summary>
/// <param name="stateStore"></param>
public class LeaderDiscoveryService(INodeStateStore stateStore) : LeaderDiscoverySvc.LeaderDiscoverySvcBase, INodeService
{
    public override Task<LeaderQueryReply> GetLeader(LeaderQueryRequest request, ServerCallContext context)
    {
        var leaderAddress = stateStore.LeaderAddress;
        if (leaderAddress == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Leader address not initialized"));
        }
        return Task.FromResult(new LeaderQueryReply
        {
            Host = leaderAddress.Host, 
            Port = leaderAddress.Port
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return LeaderDiscoverySvc.BindService(this);
    }
}