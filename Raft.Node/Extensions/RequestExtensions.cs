using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Raft.Store.Extensions;

namespace Raft.Node.Extensions;

public static class RequestExtensions
{
    public static Command FromMessage(this CommandRequest commandRequest)
    {
        return new Command(commandRequest.Variable, commandRequest.Operation.ToOperationType(), commandRequest.Literal);
    }

    public static LogEntryMessage ToMessage(this LogEntry logEntry)
    {
        return new LogEntryMessage()
        {
            Command = logEntry.Command.ToMessage(),
            Term = logEntry.Term,
        };
    }

    public static LogEntry FromMessage(this LogEntryMessage logEntry)
    {
        return new LogEntry(logEntry.Command.FromMessage(), logEntry.Term);
    }

    private static CommandRequest ToMessage(this Command command)
    {
        return new CommandRequest()
        {
            Variable = command.Variable,
            Operation = command.Operation.ToPrintable(),
            Literal = command.Literal
        };
    }
}