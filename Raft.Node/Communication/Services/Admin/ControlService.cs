using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.HeartBeat;
using Raft.Store;
using Raft.Store.Domain;

namespace Raft.Node.Communication.Services.Admin;

public class ControlService(HeartBeatRunner? heartBeatRunner, IRaftMessageReceiver raftServer, INodeStateStore stateStore) : ControlSvc.ControlSvcBase, INodeService
{
    public override Task<DisconnectReply> DisconnectNode(DisconnectMessage request, ServerCallContext context)
    {
        heartBeatRunner?.StopBeating();
        Console.WriteLine("Stopping heartbeat");

        raftServer.DisconnectFromCluster();
        Console.WriteLine("Disconnecting");
        
        return Task.FromResult(new DisconnectReply {Reply = "Node disconnected."});
    }

    public override Task<ReconnectReply> ReconnectNode(ReconnectMessage request, ServerCallContext context)
    {
        Console.WriteLine("Reconnecting");
        raftServer.ReconnectToCluster();

        if (stateStore.Role == NodeType.Leader)
        {
            Console.WriteLine("Starting heartbeat");
            heartBeatRunner?.StartBeating();
        }

        return Task.FromResult(new ReconnectReply {Reply = "Node reconnected."});
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return ControlSvc.BindService(this);
    }

}
