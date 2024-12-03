using Grpc.Core;

namespace Raft.Cli;

public class RaftClient
{
    private readonly PingSvc.PingSvcClient _pingClient;
    private readonly NodeInfoSvc.NodeInfoSvcClient _infoClient;
    private readonly CommandSvc.CommandSvcClient _commandClient;
    private readonly LogInfoSvc.LogInfoSvcClient _logInfoClient;
    private readonly ControlSvc.ControlSvcClient _controlClient;

    public RaftClient(string host, int port)
    {
        var channel = new Channel(host, port, ChannelCredentials.Insecure);
        _pingClient = new PingSvc.PingSvcClient(channel);
        _infoClient = new NodeInfoSvc.NodeInfoSvcClient(channel);
        _commandClient = new CommandSvc.CommandSvcClient(channel);
        _logInfoClient = new LogInfoSvc.LogInfoSvcClient(channel);
        _controlClient = new ControlSvc.ControlSvcClient(channel);
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

        var commandRequest = new CommandRequest()
        {
            Variable = opts.Var,
            Operation = opts.Operation,
            Literal = opts.Literal
        };
        var reply = _commandClient.ApplyCommand(commandRequest);
        return reply.ToString();
    }

    public string LogInfo()
    {
        var logInfo = _logInfoClient.GetLogLInfo(new LogInfoRequest());
        return logInfo.ToString();
    }

    public string Disconnect()
    {
        var disconnectReply = _controlClient.DisconnectNode(new DisconnectMessage());
        return disconnectReply.ToString();
    }

    public string Reconnect()
    {
        var reconnectReply = _controlClient.ReconnectNode(new ReconnectMessage());
        return reconnectReply.ToString();
    }
}