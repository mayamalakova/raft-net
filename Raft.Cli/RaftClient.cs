using Grpc.Core;

namespace Raft.Cli;

public class RaftClient
{
    private readonly PingSvc.PingSvcClient _pingClient;
    private readonly NodeInfoSvc.NodeInfoSvcClient _infoClient;

    public RaftClient(string host, int port)
    {
        var channel = new Channel(host, port, ChannelCredentials.Insecure);
        _pingClient = new PingSvc.PingSvcClient(channel);
        _infoClient = new NodeInfoSvc.NodeInfoSvcClient(channel);
    }

    public void Ping()
    {
        var reply = _pingClient.Ping(new PingRequest());

        Console.WriteLine(reply);
    }

    public void Info()
    {
        var reply = _infoClient.GetInfo(new NodeInfoRequest());

        Console.WriteLine(reply);
    }
}