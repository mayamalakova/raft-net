using Raft.Communication.Contract;
using Raft.Node.Communication;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Memory;

namespace Raft.Node;

public class RaftNode
{
    private readonly IRaftMessageReceiver _messageReceiver;
    private readonly INodeStateStore _stateStore;
    private readonly IEnumerable<INodeService> _nodeServices;

    private readonly string _nodeName;
    private readonly NodeAddress _peerAddress;

    public RaftNode(NodeType role, string nodeName, int port, string clusterHost, int clusterPort)
    {
        _nodeName = nodeName;
        _peerAddress = new NodeAddress(clusterHost, clusterPort);
        _messageReceiver = new RaftMessageReceiver(port);
        _stateStore = new NodeStateStore {Role = role};
        _nodeServices =
        [
            new LeaderDiscoveryService(_stateStore),
            new PingReplyService(_nodeName),
            new NodeInfoService(_nodeName, _stateStore)
        ];
    }

    public void Start() 
    {
        _stateStore.LeaderAddress = _stateStore.Role == NodeType.Follower 
            ? AskForLeader() 
            : _peerAddress;
        _messageReceiver.Start(_nodeServices.Select(x => x.GetServiceDefinition()));
    }

    private NodeAddress AskForLeader()
    {
        var client = new NodeCommunicationClient(_peerAddress);
        
        var leader = client.GetLeader();
        Console.WriteLine($"{_nodeName} found leader: {leader}");
        return leader;
    }

    public void Stop()
    {
        _messageReceiver.Stop();
    }
}