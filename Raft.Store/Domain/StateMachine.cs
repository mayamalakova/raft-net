namespace Raft.Store.Domain;

public class State
{
    public int Value { get; set; }
    public ICollection<string> Errors { get; set; } = new List<string>();
}
public class StateMachine
{
    public State Calculate(IEnumerable<Command> commands)
    {
        var varToValue = new Dictionary<string, int>();
        var result = new State();
        foreach (var command in commands)
        {
            if (command.Operation == CommandOperation.Assignment)
            {
                varToValue[command.Variable] = command.Literal;
                result.Value = command.Literal;
            } else if (varToValue.TryGetValue(command.Variable, out var value))
            {
                varToValue[command.Variable] = command.Operation == CommandOperation.Plus
                    ? value + command.Literal
                    : value - command.Literal;
                result.Value = varToValue[command.Variable];
            }
            else
            {
                result.Errors.Add(
                    $"Tried to do arithmetic operation {command.Operation} on unassigned variable {command.Variable}.");
            }
        }
        return result;
    }
}