using CommandLine;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Raft.Node;

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

}

class Program
{
    public static void Main(string[] args)
    {
        var result = CommandLine.Parser.Default.ParseArguments<AddOptions>(args)
            .MapResult(
                opts => AddNode(opts),
                errs => 1);
        if (result != 0)
        {
            Console.WriteLine("Please provide valid command line options.");
            return;
        }
        
        Console.WriteLine("Press any key to terminate");
        Console.ReadKey();
    }

    private static int AddNode(AddOptions addOptions)
    {
        return addOptions.Role switch
        {
            "leader" => AddLeaderNode(addOptions),
            "follower" => AddFollowerNode(addOptions),
            _ => throw new ArgumentException($"Unknown role: {addOptions.Role}")
        };
    }

    private static int AddFollowerNode(AddOptions addOptions)
    {
        var port = int.Parse(addOptions.Port);
        if (addOptions.ClusterHost == null)
        {
            throw new ArgumentException("ClusterHost is required for follower nodes.");
        }

        if (!addOptions.ClusterHost.Contains(":"))
        {
            throw new ArgumentException("ClusterHost option should be in the form of Host:port.");
        }

        var clusterHost = addOptions.ClusterHost.Split(":");
        var follower = new RaftNode(NodeType.Follower, addOptions.Name, port, clusterHost[0], int.Parse(clusterHost[1]));
        
        follower.Start();
        Console.WriteLine($"Created follower node {addOptions.Name} listening on port {port}.");

        return 0;
    }

    private static int AddLeaderNode(AddOptions addOptions)
    {
        var port = int.Parse(addOptions.Port);
        var leader = new RaftNode(NodeType.Leader, addOptions.Name, port, "localhost", 5001);
        
        leader.Start();
        Console.WriteLine($"Created leader node {addOptions.Name} listening on port {port}");
        
        return 0;
    }
}