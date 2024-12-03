using Grpc.Core;

namespace Raft.Communication.Contract;

public interface IRaftMessageReceiver
{
    void Start();
    void Stop();
    void DisconnectFromCluster();
    void ReconnectToCluster();

}