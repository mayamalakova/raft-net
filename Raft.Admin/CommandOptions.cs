using CommandLine;
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Raft.Cli;

[Verb("command", HelpText = "Update the raft state machine state.")]
public class CommandOptions
{
    [Option('v', "var", Required = true, HelpText = "Variable to update or initialize.")]
    public string Var { get; set; }
    [Option('o', "operation", Required = true, HelpText = "Operation to apply to the variable - one of +, - or =.")]
    public string Operation { get; set; }
    [Option('l', "literal", Required = true, HelpText = "Literal to use as an argument to the operation.")]
    public int Literal { get; set; }
}