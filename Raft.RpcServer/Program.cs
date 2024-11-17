using Grpc.Core;
using Shared;

namespace Raft;

class Program
{
    static void Main(string[] args)
    {
        var port = int.Parse(args[0]);
        var server = new Server
        {
            Services = { Svc.BindService(new MyService()) },
            Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
        };
        server.Start();
        Console.WriteLine($"Server listening at port {port}. Press any key to terminate");
        Console.ReadKey();
    }
}