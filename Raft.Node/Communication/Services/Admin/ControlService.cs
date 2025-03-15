using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Node.HeartBeat;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Communication.Services.Admin;

/// <summary>
/// Can connect and disconnect a node from the cluster
/// </summary>
/// <param name="heartBeatRunner"></param>
/// <param name="raftServer"></param>
/// <param name="stateStore"></param>
public class ControlService(HeartBeatRunner? heartBeatRunner, IClusterMessageReceiver raftServer, INodeStateStore stateStore) : ControlSvc.ControlSvcBase, INodeService
{
    public override Task<DisconnectReply> DisconnectNode(DisconnectMessage request, ServerCallContext context)
    {
        heartBeatRunner?.StopBeating();
        Log.Information("Stopping heartbeat");

        raftServer.DisconnectFromCluster();
        Log.Information("Disconnecting");
        
        return Task.FromResult(new DisconnectReply {Reply = "Node disconnected."});
    }

    public override Task<ReconnectReply> ReconnectNode(ReconnectMessage request, ServerCallContext context)
    {
        Log.Information("Reconnecting");
        raftServer.ReconnectToCluster();

        if (stateStore.Role == NodeType.Leader)
        {
            Log.Information("Starting heartbeat");
            heartBeatRunner?.StartBeating();
        }

        return Task.FromResult(new ReconnectReply {Reply = "Node reconnected."});
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return ControlSvc.BindService(this);
    }

}
