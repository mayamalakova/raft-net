using System.Collections.Concurrent;

namespace Raft.Store.Domain;

public record State
{
    public int Value { get; set; }
    public ICollection<string> Errors { get; set; } = new List<string>();

    public override string ToString()
    {
        return $"value={Value} errors={string.Join(',', Errors)}";
    }
}
public class StateMachine
{
    private readonly ConcurrentDictionary<string, int> _varToValue = new();
    public State CurrentState { get; } = new();
    private readonly Lock _lock = new();

    public State Calculate(IEnumerable<Command> commands)
    {
        foreach (var command in commands)
        {
            ApplyCommand(command);
        }
        return CurrentState;
    }

    private void ApplyCommand(Command command)
    {
        lock (_lock)
        {
            UpdateState(command);
        }
    }

    private void UpdateState(Command command)
    {
        if (command.Operation == CommandOperation.Assignment)
        {
            _varToValue[command.Variable] = command.Literal;
            CurrentState.Value = command.Literal;
        } else if (_varToValue.TryGetValue(command.Variable, out var value))
        {
            _varToValue[command.Variable] = command.Operation == CommandOperation.Plus
                ? value + command.Literal
                : value - command.Literal;
            CurrentState.Value = _varToValue[command.Variable];
        }
        else
        {
            CurrentState.Errors.Add(
                $"Tried to do arithmetic operation {command.Operation} on unassigned variable {command.Variable}.");
        }
    }

    public State ApplyCommands(IEnumerable<Command> commands)
    {
        foreach (var command in commands)
        {
            ApplyCommand(command);
        }
        return CurrentState;
    }

    public State GetCurrent()
    {
        return CurrentState;
    }
}