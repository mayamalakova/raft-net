using Raft.Store.Extensions;

namespace Raft.Store.Domain;

public enum CommandOperation
{
    Assignment,
    Plus,
    Minus
}

public record Command(string Variable, CommandOperation Operation, int Literal)
{
    public string Variable { get; set; } = Variable;
    public CommandOperation Operation { get; set; } = Operation;
    public int Literal { get; set; } = Literal;

    public override string ToString()
    {
        return $"({Variable}{Operation.ToPrintable()}{Literal})";
    }
}