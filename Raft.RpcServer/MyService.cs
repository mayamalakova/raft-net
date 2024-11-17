using Grpc.Core;
using Shared;

namespace Raft;

public class MyService: Svc.SvcBase
{
    public override Task<CalculateReply> Calculate(CalculateRequest request, ServerCallContext context)
    {
        Console.WriteLine($"Calculate request: {request.X} {request.Op} {request.Y} ");
        long result = -1;
        switch (request.Op)
        {
            case "+":
                result = request.X + request.Y;
                break;
            case "-":
                result = request.X - request.Y;
                break;
            case "*":
                result = request.X * request.Y;
                break;
            case "/":
                if (request.Y != 0)
                {
                    result = (long)request.X / request.Y;
                }
                break;
            default:
                break;
        }
        Console.WriteLine($"Result: {result}");
        return Task.FromResult(new CalculateReply { Result = result });
    }
}