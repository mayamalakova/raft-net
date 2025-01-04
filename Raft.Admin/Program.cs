using CommandLine;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Raft.Cli;

public static class Program
{
    public static void Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<RaftClientOptions>(args)
            .MapResult(
                StartRaftClient,
                _ => 1);
        if (result != 0)
        {
            Console.WriteLine("Please provide valid command line options.");
        }
    }

    private static int StartRaftClient(RaftClientOptions opts)
    {
        var parts = opts.NodeAddress.Split(":");
        if (parts.Length < 2)
        {
            throw new ArgumentException("Invalid node address");
        }

        var raftClient = new RaftClient(parts[0], int.Parse(parts[1]));
        Console.WriteLine(
            $"Client is ready to send requests to node {opts.NodeAddress}. Enter a command line or type 'quit' to exit.");

        while (true)
        {
            Console.Write("> ");
            var command = Console.ReadLine();
            if (command is null or "quit" or "exit" or "q")
            {
                return 0;
            }

            Parser.Default
                .ParseArguments<PingOptions, InfoOptions, CommandOptions, LogInfoOptions, DisconnectOptions,
                    ReconnectOptions, GetStateOptions>(command.Split(' ').Select(x => x.Trim()))
                .WithParsed<PingOptions>(_ => Console.WriteLine(raftClient.Ping()))
                .WithParsed<InfoOptions>(_ => Console.WriteLine(raftClient.Info()))
                .WithParsed<CommandOptions>(c => Console.WriteLine(raftClient.Command(c)))
                .WithParsed<LogInfoOptions>(_ => Console.WriteLine(raftClient.LogInfo()))
                .WithParsed<DisconnectOptions>(_ => Console.WriteLine(raftClient.Disconnect()))
                .WithParsed<ReconnectOptions>(_ => Console.WriteLine(raftClient.Reconnect()))
                .WithParsed<GetStateOptions>(_ => Console.WriteLine(raftClient.GetState()))
                .WithNotParsed(errors => { Console.WriteLine(errors.ToString()); });
        }
    }
}