using CommandLine;
// ReSharper disable ClassNeverInstantiated.Global

namespace Raft.Cli;

#pragma warning disable CS8618
public class RaftClientOptions
{
    [Option('a', "node-address", Required = true, HelpText = "Host:port of any node already on the cluster.")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string NodeAddress { get; set; }
}