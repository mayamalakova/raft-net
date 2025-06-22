using CommandLine;
using Raft.Node.Timing;
using Raft.Shared;
using Raft.Store.Domain;
using Serilog;

namespace Raft.Node;

public static class Program
{
    private static RaftNode? _node;

    public static void Main(string[] args)
    {
        try {
            Logger.ConfigureLogger();
            var result = Parser.Default.ParseArguments<AddOptions>(args)
                .MapResult(
                    opts =>
                    {
                        _node = AddNode(opts);
                        return 0;
                    },
                    errs =>
                    {
                        Log.Information($"Error: {string.Join(Environment.NewLine, errs)}");
                        return 1;
                    });
            if (result != 0)
            {
                Log.Information("Please provide valid command line options.");
                return;
            }

            Log.Information("Press any key to terminate");
            Console.ReadKey();

            _node?.Stop();
        } finally {
            Logger.Close();
        }
    }

    private static RaftNode AddNode(AddOptions addOptions)
    {
        return addOptions.Role switch
        {
            "leader" => AddLeaderNode(addOptions),
            "follower" => AddFollowerNode(addOptions),
            _ => throw new ArgumentException($"Unknown role: {addOptions.Role}")
        };
    }

    private static RaftNode AddFollowerNode(AddOptions addOptions)
    {
        var port = int.Parse(addOptions.Port);
        if (addOptions.ClusterHost == null)
        {
            throw new ArgumentException("ClusterHost is required for follower nodes.");
        }

        if (!addOptions.ClusterHost.Contains(':'))
        {
            throw new ArgumentException("ClusterHost option should be in the form of Host:port.");
        }

        var clusterHost = addOptions.ClusterHost.Split(":");
        var follower = new RaftNode(NodeType.Follower, addOptions.Name, port, clusterHost[0], int.Parse(clusterHost[1]),
            addOptions.TimeoutSeconds, 3, new SystemTimerFactory());

        follower.Start();
        Log.Information($"Created follower node {addOptions.Name} listening on port {port}.");

        return follower;
    }

    private static RaftNode AddLeaderNode(AddOptions addOptions)
    {
        var port = int.Parse(addOptions.Port);
        var leader = new RaftNode(NodeType.Leader, addOptions.Name, port, "localhost", port, addOptions.TimeoutSeconds, 3, new SystemTimerFactory());

        leader.Start();
        Log.Information($"Created leader node {addOptions.Name} listening on port {port}");

        return leader;
    }
}