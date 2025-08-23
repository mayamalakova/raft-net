using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
[Verb("leader", HelpText = "Get information about the cluster leader.")]
public class LeaderOptions;