using NUnit.Framework;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.Store.Tests;

public class StateMachineTests
{
    [Test]
    public void ShouldCalculateState()
    {
        Command[] commands = [
            new Command("A", CommandOperation.Assignment, 5)
        ];
        var result = new StateMachine().Calculate(commands);
        
        result.Value.ShouldBe(5);
        result.Errors.ShouldBeEmpty();
    }

    [Test]
    public void ShouldApplyArithmeticOperations()
    {
        Command[] commands = [
            new Command("A", CommandOperation.Assignment, 5),
            new Command("A", CommandOperation.Plus, 1),
            new Command("A", CommandOperation.Minus, 3)
        ];
        var result = new StateMachine().Calculate(commands);
        
        result.Value.ShouldBe(3);
        result.Errors.ShouldBeEmpty();
    }

    [Test]
    public void ShouldReturnErrorWhenVariableIsMissing()
    {
        Command[] commands = [
            new Command("A", CommandOperation.Plus, 1),
        ];
        var result = new StateMachine().Calculate(commands);
        
        result.Errors.ShouldContain("Tried to do arithmetic operation Plus on unassigned variable A.");
    }
}