using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

[Verb("disconnect", HelpText = "Disconnect from the cluster.")]
public class DisconnectOptions;