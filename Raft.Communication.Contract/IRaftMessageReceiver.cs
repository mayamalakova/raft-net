namespace Raft.Communication.Contract;

public interface IRaftMessageReceiver: IMessageReceiver
{
    /// <summary>
    /// Stop accepting communication from the cluster
    /// </summary>
    void DisconnectFromCluster();
    /// <summary>
    /// Start accepting communication from the cluster again
    /// </summary>
    void ReconnectToCluster();
}