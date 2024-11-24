using Grpc.Core;

namespace Raft.Communication.Contract;

public interface INodeService
{
    ServerServiceDefinition GetServiceDefinition();
}