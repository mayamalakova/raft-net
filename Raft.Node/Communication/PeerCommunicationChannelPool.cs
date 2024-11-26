using System.Collections.Concurrent;
using Grpc.Core;
using Raft.Store.Domain;
using Channel = Grpc.Core.Channel;

namespace Raft.Node.Communication;

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
}

public interface IClientPool
{
    CommandSvc.CommandSvcClient GetCommandServiceClient(NodeAddress targetAddress);
}