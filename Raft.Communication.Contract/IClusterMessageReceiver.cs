namespace Raft.Communication.Contract;

/// <summary>
/// Processes messages from the cluster
/// </summary>
public interface IClusterMessageReceiver: IMessageReceiver
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