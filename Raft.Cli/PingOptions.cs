using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
[Verb("ping", HelpText = "Add file contents to the index.")]
public class PingOptions;