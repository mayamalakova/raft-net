using CommandLine;
using Raft.Cli;

public class PingOptions
{
    [Option('a', "node-address", Required = false, HelpText = "Host:port of any node already on the cluster.")]
    public string NodeAddress { get; set; }

}

public class Program
{
    public static void Main(string[] args)
    {
        var raftClient = new RaftClient();
        var result = Parser.Default.ParseArguments<PingOptions>(args)
            .MapResult(
                opts => Ping(raftClient, opts),
                errs => 1);
        if (result != 0)
        {
            Console.WriteLine("Please provide valid command line options.");
            return;
        }
        
        Console.WriteLine("Press any key to terminate");
        Console.ReadKey();
    }

    private static int Ping(RaftClient raftClient, PingOptions opts)
    {

        var parts = opts.NodeAddress.Split(":");
        if (parts.Length < 2)
        {
            throw new ArgumentException("Invalid node address");
        }
        raftClient.Ping(parts[0], int.Parse(parts[1]));
        
        return 0;
    }
    
}