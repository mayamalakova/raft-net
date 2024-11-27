using Raft.Store.Domain;
using Raft.Store.Extensions;

namespace Raft.Node.Extensions;

public static class RequestExtensions
{
    public static Command ToCommand(this CommandRequest commandRequest)
    {
        return new Command(commandRequest.Variable, commandRequest.Operation.ToOperationType(), commandRequest.Literal);
    }
}