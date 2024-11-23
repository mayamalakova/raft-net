using CommandLine;
using Grpc.Core;

namespace Raft.Cli;



public class RaftClient(string host, int port)
{
    
    public void Ping()
    {
        var channel = new Channel(host, port, ChannelCredentials.Insecure);  
        var client = new PingSvc.PingSvcClient(channel);
        var reply = client.Ping(new PingRequest());
        
        Console.WriteLine(reply);
    }
}