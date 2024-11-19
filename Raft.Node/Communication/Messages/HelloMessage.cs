namespace Raft.Node.Communication.Messages;

public class HelloMessage(string message) : IHelloMessage
{
    private readonly string _message = message;
}