using CommandLine;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Raft.Node;

#pragma warning disable CS8618
[Verb("add", HelpText = "Add file contents to the index.")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AddOptions
{
    [Option('r', "role", Required = true, HelpText = "Role of the node - leader or follower.")]
    public string Role { get; set; }
    
    [Option('n', "name", Required = true, HelpText = "Name of the node. It needs to be unique within the cluster.")]
    public string Name { get; set; }
    
    [Option('p', "port", Required = true, HelpText = "Port on which the node will communicate with the rest of the cluster.")]
    public string Port { get; set; }
    
    [Option('c', "cluster-address", Required = false, HelpText = "Host:port of any node already on the cluster. Required if the node is added as a follower.")]
    public string ClusterHost { get; set; }
    
    [Option('t', "timeout", Required = false, Default = 5, HelpText = "Number of seconds to wait for reply when sending requests to other nodes.")]
    public int TimeoutSeconds { get; set; }

}