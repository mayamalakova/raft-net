using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services.Cluster;
using Raft.Node.HeartBeat;
using Raft.Node.Tests.MockHelpers;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Shouldly;

namespace Raft.Node.Tests;

public class CommandProcessingServiceTests
{
    private INodeStateStore _stateStore;
    private CommandProcessingService _commandProcessingService;
    private IClientPool _clientPool;
    private IClusterNodeStore _clusterStore;
    private IAppendEntriesRequestFactory _appendEntriesRequestFactory;
    private ReplicationStateManager _replicationStateManager;

    [SetUp]
    public void SetUp()
    {
        _stateStore = Substitute.For<INodeStateStore>();
        _clientPool = Substitute.For<IClientPool>();
        _clusterStore = Substitute.For<IClusterNodeStore>();
        _replicationStateManager = Substitute.For<ReplicationStateManager>(_stateStore, _clusterStore);
        _appendEntriesRequestFactory = Substitute.For<IAppendEntriesRequestFactory>();
        var logReplicator = new LogReplicator(_stateStore, _clientPool, _clusterStore, "lead1", 2)
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };
        var leaderService = new RaftLeaderService(logReplicator, _replicationStateManager, _stateStore, _clusterStore);
        _commandProcessingService = new CommandProcessingService(_stateStore, _clientPool, leaderService,
            new HeartBeatRunner(50, _stateStore, () => { }));
    }

    [Test]
    public void LeaderShouldApplyCommand()
    {
        _stateStore.Role.Returns(NodeType.Leader);
        _stateStore.LeaderInfo.Returns(new NodeInfo("A", new NodeAddress("localhost", 5001)));
        _stateStore.ApplyCommitted().Returns(new State() {Value = 666});
        var mockCallContext = CreateMockCallContext();

        var reply = _commandProcessingService.ApplyCommand(
            new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 }, mockCallContext);

        reply.Result.ShouldBe(new CommandReply() { Result = "Success at node A newState=value=666 errors=" });
        _stateStore.Received().AppendLogEntry(new LogEntry(new Command("A", CommandOperation.Assignment, 5), 0));
    }

    [Test]
    public void LeaderShouldSendAppendEntriesRequestToFollowers()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        _clusterStore.GetNodes().Returns([new NodeInfo("someNode", followerAddress)]);
        var mockFollowerClient = SetUpMockAppendEntriesClient(followerAddress);
        var mockCallContext = CreateMockCallContext();

        _commandProcessingService.ApplyCommand(
            new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 }, mockCallContext);

        mockFollowerClient.Received().AppendEntriesAsync(Arg.Any<AppendEntriesRequest>(), Arg.Any<CallOptions>());
    }

    [Test]
    public void FollowerShouldForwardToLeader()
    {
        var leaderAddress = new NodeAddress("localhost", 5001);
        _stateStore.Role.Returns(NodeType.Follower);
        _stateStore.LeaderInfo.Returns(new NodeInfo("leader", leaderAddress));

        var commandRequest = new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 };
        CreateMockCommandClient(leaderAddress, commandRequest,
            new CommandReply() { Result = "Success at leader" });

        var reply = _commandProcessingService.ApplyCommand(commandRequest, Substitute.For<ServerCallContext>());

        reply.Result.ShouldBe(new CommandReply() { Result = "Success at leader" });
        _stateStore.DidNotReceive().AppendLogEntry(Arg.Any<LogEntry>());
    }

    private static ServerCallContext CreateMockCallContext()
    {
        var mockCallContext = Substitute.For<ServerCallContext>();
        mockCallContext.Host.Returns("localhost");
        return mockCallContext;
    }

    private AppendEntriesSvc.AppendEntriesSvcClient SetUpMockAppendEntriesClient(NodeAddress followerAddress,
        bool success = true)
    {
        var mockFollowerClient = Substitute.For<AppendEntriesSvc.AppendEntriesSvcClient>();
        var appendEntriesReply = new AppendEntriesReply()
        {
            Success = success
        };
        mockFollowerClient.AppendEntriesAsync(Arg.Any<AppendEntriesRequest>(), Arg.Any<CallOptions>())
            .Returns(ClientMockHelpers.CreateAsyncUnaryCall(appendEntriesReply));
        _clientPool.GetAppendEntriesClient(followerAddress).Returns(mockFollowerClient);
        return mockFollowerClient;
    }

    private void CreateMockCommandClient(NodeAddress targetAddress, CommandRequest command,
        CommandReply reply)
    {
        var mockCommandClient = Substitute.For<CommandSvc.CommandSvcClient>();
        mockCommandClient.ApplyCommandAsync(command)
            .Returns(ClientMockHelpers.CreateAsyncUnaryCall(reply));
        _clientPool.GetCommandServiceClient(targetAddress).Returns(mockCommandClient);
    }
}