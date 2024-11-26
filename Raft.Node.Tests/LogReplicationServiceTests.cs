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

    [SetUp]
    public void SetUp()
    {
        _mockStateStore = Substitute.For<INodeStateStore>();
        _channelPool = Substitute.For<IClientPool>();
        _logReplicationService = new LogReplicationService(_mockStateStore, _channelPool);
    }

    [Test]
    public void LeaderShouldApplyCommand()
    {
        _mockStateStore.Role.Returns(NodeType.Leader);
        var mockCallContext = Substitute.For<ServerCallContext>();
        mockCallContext.Host.Returns("localhost");

        var reply = _logReplicationService.ApplyCommand(
            new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 }, mockCallContext);
        
        reply.Result.ShouldBe(new CommandReply() { Result = "Success at localhost" });
        _mockStateStore.Received().AppendLogEntry(new Command("A", CommandOperation.Assignment, 5), 0);
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

    private void CreateMockCommandClient(NodeAddress targetAddress, CommandRequest command,
        CommandReply reply)
    {
        var mockCommandClient = Substitute.For<CommandSvc.CommandSvcClient>();
        mockCommandClient.ApplyCommandAsync(command)
            .Returns(ClientMockHelpers.CreateAsyncUnaryCall(reply));
        _channelPool.GetCommandServiceClient(targetAddress).Returns(mockCommandClient);
    }
}