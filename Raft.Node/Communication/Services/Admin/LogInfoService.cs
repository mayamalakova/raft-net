using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;

namespace Raft.Node.Communication.Services.Admin;

public class LogInfoService(INodeStateStore stateStore) : LogInfoSvc.LogInfoSvcBase, INodeService
{
    public override Task<LogInfoReply> GetLogLInfo(LogInfoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LogInfoReply()
        {
            Entries = stateStore.PrintLog(),
        });
    }

    public ServerServiceDefinition GetServiceDefinition()
    {
        return LogInfoSvc.BindService(this);
    }
}