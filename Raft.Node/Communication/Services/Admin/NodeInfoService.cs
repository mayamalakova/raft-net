using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node.Communication.Services.Admin;

public class NodeInfoService(string name, NodeAddress nodeAddress, INodeStateStore stateStore, IClusterNodeStore nodeStore) : NodeInfoSvc.NodeInfoSvcBase, INodeService
{
    public override Task<NodeInfoReply> GetInfo(NodeInfoRequest request, ServerCallContext context)
    {
        Log.Information("Got node info request");
        return Task.FromResult(new NodeInfoReply
        {
            Address = nodeAddress.ToString(),
            Name = name,
            Role = stateStore.Role.ToString(), 
            LeaderAddress = stateStore.LeaderInfo?.NodeAddress.ToString(),
            KnownNodes = nodeStore.ToString(),
            CommitIndex = stateStore.CommitIndex.ToString(),
            Term = stateStore.CurrentTerm.ToString(),
        });
    }

    public override Task<LeaderInfoReply> GetLeader(LeaderInfoRequest request, ServerCallContext context)
    {
        Log.Information("Got leader info request");
        return Task.FromResult(new LeaderInfoReply()
        {
            Name = stateStore.LeaderInfo?.NodeName,
            Address = stateStore.LeaderInfo?.NodeAddress.ToString(),
            Term = stateStore.CurrentTerm.ToString()
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return NodeInfoSvc.BindService(this);
    }
}