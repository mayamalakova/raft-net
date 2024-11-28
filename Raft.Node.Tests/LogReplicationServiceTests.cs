using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Client;
using Raft.Node.Communication.Services;
using Raft.Node.Tests.MockHelpers;
using Raft.Store;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.Node.Tests;

public class LogReplicationServiceTests
{
    private INodeStateStore _mockStateStore;
    private LogReplicationService _logReplicationService;
    private IClientPool _channelPool;
    private IClusterNodeStore _nodeStore;
    private IAppendEntriesRequestFactory _appendEntriesRequestFactory;

    [SetUp]
    public void SetUp()
    {
        _mockStateStore = Substitute.For<INodeStateStore>();
        _channelPool = Substitute.For<IClientPool>();
        _nodeStore = Substitute.For<IClusterNodeStore>();
        _appendEntriesRequestFactory = Substitute.For<IAppendEntriesRequestFactory>();
        _logReplicationService = new LogReplicationService(_mockStateStore, _channelPool, _nodeStore, "lead1")
        {
            EntriesRequestFactory = _appendEntriesRequestFactory
        };
    }

    [Test]
    public void LeaderShouldApplyCommand()
    {
        _mockStateStore.Role.Returns(NodeType.Leader);
        var mockCallContext = CreateMockCallContext();

        var reply = _logReplicationService.ApplyCommand(
            new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 }, mockCallContext);

        reply.Result.ShouldBe(new CommandReply() { Result = "Success at localhost" });
        _mockStateStore.Received().AppendLogEntry(new Command("A", CommandOperation.Assignment, 5), 0);
    }

    [Test]
    public void LeaderShouldSendAppendEntriesRequestToFollowers()
    {
        var followerAddress = new NodeAddress("someHost", 666);
        _nodeStore.GetNodes().Returns([new NodeInfo("someNode", followerAddress)]);
        var mockFollowerClient = SetUpMockAppendEntriesClient(followerAddress);
        var mockCallContext = CreateMockCallContext();

        _logReplicationService.ApplyCommand(
            new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 }, mockCallContext);

        mockFollowerClient.Received().AppendEntries(Arg.Any<AppendEntriesRequest>());
    }

    [Test]
    public void FollowerShouldForwardToLeader()
    {
        var leaderAddress = new NodeAddress("localhost", 5001);
        _mockStateStore.Role.Returns(NodeType.Follower);
        _mockStateStore.LeaderAddress.Returns(leaderAddress);

        var commandRequest = new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 };
        CreateMockCommandClient(leaderAddress, commandRequest,
            new CommandReply() { Result = "Success at leader" });

        var reply = _logReplicationService.ApplyCommand(commandRequest, Substitute.For<ServerCallContext>());

        reply.Result.ShouldBe(new CommandReply() { Result = "Success at leader" });
        _mockStateStore.DidNotReceive().AppendLogEntry(Arg.Any<Command>(), Arg.Any<int>());
    }

    private static ServerCallContext CreateMockCallContext()
    {
        var mockCallContext = Substitute.For<ServerCallContext>();
        mockCallContext.Host.Returns("localhost");
        return mockCallContext;
    }

    private AppendEntriesSvc.AppendEntriesSvcClient SetUpMockAppendEntriesClient(NodeAddress followerAddress)
    {
        var mockFollowerClient = Substitute.For<AppendEntriesSvc.AppendEntriesSvcClient>();
        mockFollowerClient.AppendEntries(Arg.Any<AppendEntriesRequest>()).Returns(new AppendEntriesReply()
        {
            Success = true
        });
        _channelPool.GetAppendEntriesClient(followerAddress).Returns(mockFollowerClient);
        return mockFollowerClient;
    }

    private void CreateMockCommandClient(NodeAddress targetAddress, CommandRequest command,
        CommandReply reply)
    {
        var mockCommandClient = Substitute.For<CommandSvc.CommandSvcClient>();
        mockCommandClient.ApplyCommandAsync(command)
            .Returns(ClientMockHelpers.CreateAsyncUnaryCall(reply));
        _channelPool.GetCommandServiceClient(targetAddress).Returns(mockCommandClient);
    }
}