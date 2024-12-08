using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

[Verb("reconnect", HelpText = "Reconnect to the cluster.")]
public class ReconnectOptions;