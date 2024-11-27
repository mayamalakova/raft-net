using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
[Verb("log-info", HelpText = "Get information about the log of the current node.")]
public class LogInfoOptions;