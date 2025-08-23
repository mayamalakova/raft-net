using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
[Verb("info", HelpText = "Get information about the current node.")]
public class InfoOptions;