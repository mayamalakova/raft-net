using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;
[Verb("get-state", HelpText = "Get the current state of the state machine on the node.")]
public class GetStateOptions;