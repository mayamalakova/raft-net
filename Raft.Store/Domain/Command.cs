namespace Raft.Store.Domain;

public enum CommandOperation
{
    Assignment,
    Plus,
    Minus
}

public class Command
{
    public string Variable { get; set; }
    public CommandOperation Operation { get; set; }
    public int Literal { get; set; }

    public Command(string variable, CommandOperation operation, int literal)
    {
        Variable = variable;
        Operation = operation;
        Literal = literal;
    }
}