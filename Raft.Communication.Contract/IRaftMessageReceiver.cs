using Grpc.Core;

namespace Raft.Communication.Contract;

public interface IRaftMessageReceiver
{
    void Start(IEnumerable<ServerServiceDefinition> services);
}