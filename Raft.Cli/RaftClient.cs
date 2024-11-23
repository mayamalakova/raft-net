using CommandLine;

namespace Raft.Cli;



public class RaftClient
{
    public void Ping(string host, int port)
    {
        Console.WriteLine("Pong");
    }
}