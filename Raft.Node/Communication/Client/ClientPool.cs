using System.Collections.Concurrent;
using Grpc.Core;
using Raft.Store.Domain;

namespace Raft.Node.Communication.Client;

public class ClientPool: IClientPool
{
    private readonly ConcurrentDictionary<NodeAddress, Channel> _channels = new();

    private Channel GetChannel(NodeAddress nodeAddress)
    {
        return _channels.GetOrAdd(nodeAddress, (key) => new Channel(key.Host, key.Port, ChannelCredentials.Insecure));
    }

    public CommandSvc.CommandSvcClient GetCommandServiceClient(NodeAddress targetAddress)
    {
        return new CommandSvc.CommandSvcClient(GetChannel(targetAddress));
    }

    public LeaderDiscoverySvc.LeaderDiscoverySvcClient GetLeaderDiscoveryClient(NodeAddress targetAddress)
    {
        return new LeaderDiscoverySvc.LeaderDiscoverySvcClient(GetChannel(targetAddress));
    }

    public AppendEntriesSvc.AppendEntriesSvcClient GetAppendEntriesClient(NodeAddress targetAddress)
    {
        return new AppendEntriesSvc.AppendEntriesSvcClient(GetChannel(targetAddress));
    }
}