using CommandLine;

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
        var follower = new RaftNode(NodeType.Follower, addOptions.Name, port);
        
        follower.Start();
        Console.WriteLine($"Created follower node {addOptions.Name} listening on port {port}.");

        follower.SendMessage($"{addOptions.Name} says Hello!");

        return 0;
    }

    private static int AddLeaderNode(AddOptions addOptions)
    {
        var port = int.Parse(addOptions.Port);
        var leader = new RaftNode(NodeType.Leader, addOptions.Name, port);
        
        leader.Start();
        Console.WriteLine($"Created leader node {addOptions.Name} listening on port {port}");
        
        return 0;
    }
}