using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
[Verb("info", HelpText = "Get informatin about the current node.")]
public class InfoOptions;