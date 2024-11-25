using Raft.Store.Domain;

namespace Raft.Store.Extensions;

public static class CommandExtensions
{
    public static CommandOperation ToOperationType(this string operation)
    {
        switch (operation)
        {
            case "+":
            case "plus":
                return CommandOperation.Plus;
            case "-":
            case "minus":
                return CommandOperation.Minus;
            case "=":
                return CommandOperation.Assignment;
            default:
                throw new ArgumentException($"Unknown operation: {operation}");
        }
    }

    public static string ToPrintable(this CommandOperation operation)
    {
        switch (operation)
        {
            case CommandOperation.Assignment:
                return "=";
            case CommandOperation.Minus:
                return "-";
            case CommandOperation.Plus:
                return "+";
            default:
                throw new ArgumentException($"Unknown operation: {operation}");
        }
    }
}