using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication;
using Raft.Store;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.Node.Tests;

public class LogReplicationServiceTests
{
    private INodeStateStore _mockStateStore;
    private LogReplicationService _logReplicationService;

    [SetUp]
    public void SetUp()
    {
        _mockStateStore = Substitute.For<INodeStateStore>();
        _logReplicationService = new LogReplicationService(_mockStateStore);
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


}