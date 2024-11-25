using Grpc.Core;

namespace Raft.Cli;

public class RaftClient
{
    private readonly PingSvc.PingSvcClient _pingClient;
    private readonly NodeInfoSvc.NodeInfoSvcClient _infoClient;
    private readonly CommandSvc.CommandSvcClient _commandClient;

    public RaftClient(string host, int port)
    {
        var channel = new Channel(host, port, ChannelCredentials.Insecure);
        _pingClient = new PingSvc.PingSvcClient(channel);
        _infoClient = new NodeInfoSvc.NodeInfoSvcClient(channel);
        _commandClient = new CommandSvc.CommandSvcClient(channel);
    }

    public string Ping()
    {
        var reply = _pingClient.Ping(new PingRequest());

        return reply.ToString();
    }

    public string Info()
    {
        var reply = _infoClient.GetInfo(new NodeInfoRequest());

        return reply.ToString();
    }

    public string Command(CommandOptions opts)
    {
        if (opts.Operation != "=" && opts.Operation != "-" && opts.Operation != "+")
        {
            return "Error: the command contained an invalid operation";
        }

        if (int.TryParse(opts.Var, out var val))
        {
            var commandRequest = new CommandRequest()
            {
                Variable = opts.Var,
                Operation = opts.Operation,
                Literal = val
            };
            var reply = _commandClient.ApplyCommand(commandRequest);
            return reply.ToString();
        }

        return "Error: the command contained an invalid integer literal";
    }
}