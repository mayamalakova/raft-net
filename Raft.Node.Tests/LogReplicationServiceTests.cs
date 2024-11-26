using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication;
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
    }

    [Test]
    public void FollowerShouldForwardToLeader()
    {
        _mockStateStore.Role.Returns(NodeType.Follower);
        var leaderAddress = new NodeAddress("localhost", 5001);
        var mockCommandClient = Substitute.For<CommandSvc.CommandSvcClient>();
        _channelPool.GetCommandServiceClient(leaderAddress).Returns(mockCommandClient);
        _mockStateStore.LeaderAddress.Returns(leaderAddress);
        var mockCallContext = Substitute.For<ServerCallContext>();
        mockCallContext.Host.Returns("localhost");
        var commandRequest = new CommandRequest() { Variable = "A", Operation = "=", Literal = 5 };
        mockCommandClient.ApplyCommandAsync(commandRequest).Returns(
            ClientMockHelpers.CreateAsyncUnaryCall(new CommandReply() { Result = "Success at leader" }));

        var reply = _logReplicationService.ApplyCommand(commandRequest, mockCallContext);
        
        reply.Result.ShouldBe(new CommandReply() { Result = "Success at leader" });
    }
    

}