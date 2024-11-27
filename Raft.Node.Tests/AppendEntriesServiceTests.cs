using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Services;
using Raft.Store;
using Raft.Store.Domain;
using Shouldly;

namespace Raft.Node.Tests;

public class AppendEntriesServiceTests
{
    private INodeStateStore _nodeStateStore;
    private AppendEntriesService _service;

    [SetUp]
    public void SetUp()
    {
        _nodeStateStore = Substitute.For<INodeStateStore>();
        _nodeStateStore.GetTermAtIndex(1).Returns(1);
        _service = new AppendEntriesService(_nodeStateStore);
    }
    
    [TestCase(1, 1, true)]
    [TestCase(2, 1, false)]
    public void FollowerShouldReplySuccessOrFailBasedOnWhetherLastIndexMatchesTerm(int prevLogTerm, int termAtIndex, bool expectedResult)
    {
        var appendEntriesRequest = new AppendEntriesRequest()
        {
            PrevLogIndex = 0,
            PrevLogTerm = prevLogTerm,
        };
        _nodeStateStore.GetTermAtIndex(0).Returns(termAtIndex);
        var reply = _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        var appendEntriesReply = reply.Result;
        appendEntriesReply.Success.ShouldBe(expectedResult);
    }

    [Test]
    public void FollowerShouldAppendEntriesIfSuccessful()
    {
        var entry = new CommandRequest() {Variable = "A", Operation = "=", Literal = 1 };

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            EntryCommands = { new[] { entry } }
        };
        _nodeStateStore.GetTermAtIndex(-1).Returns(-1);
        
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.Received(1).AppendLogEntry(new Command("A", CommandOperation.Assignment, 1), 0);
    }
}