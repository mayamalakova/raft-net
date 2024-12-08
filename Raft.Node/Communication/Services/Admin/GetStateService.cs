using Grpc.Core;
using Raft.Communication.Contract;
using Raft.Store;

namespace Raft.Node.Communication.Services.Admin;

public class GetStateService(INodeStateStore stateStore) : GetStateSvc.GetStateSvcBase, INodeService
{
    public ServerServiceDefinition GetServiceDefinition()
    {
        return GetStateSvc.BindService(this);
    }

    public override Task<GetStateReply> GetState(GetStateMassage request, ServerCallContext context)
    {
        var currentState = stateStore.StateMachine.GetCurrent();
        var errors = currentState.Errors.ToArray();
        return Task.FromResult(new GetStateReply()
        {
            Value = currentState.Value.ToString(),
            Errors = { errors }
        });
    }
}