using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Services;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Extensions;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Node.Tests;

public class AppendEntriesServiceTests
{
    private INodeStateStore _nodeStateStore;
    private AppendEntriesService _service;

    [SetUp]
    public void SetUp()
    {
        _nodeStateStore = new NodeStateStore();
        _service = new AppendEntriesService(_nodeStateStore);
    }
    
    [TestCase(1, 1, true)]
    [TestCase(2, 1, false)]
    public void FollowerShouldReplySuccessOrFailBasedOnWhetherLastIndexMatchesLastTerm(int prevLogTerm, int termAtIndex, bool expectedResult)
    {
        _nodeStateStore.AppendLogEntry(CreateLogEntryCommand("B", "=", 1), termAtIndex);
        var appendEntriesRequest = new AppendEntriesRequest()
        {
            PrevLogIndex = 0,
            PrevLogTerm = prevLogTerm,
        };
        
        var reply = _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        var appendEntriesReply = reply.Result;
        appendEntriesReply.Success.ShouldBe(expectedResult);
    }

    [Test]
    public void FollowerShouldAppendEntriesIfSuccessful()
    {
        var entry = CreateLogEntryCommandRequest("A", "=", 1);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            EntryCommands = { new[] { entry } }
        };
        
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.LogLength.ShouldBe(1);
        var lastLogEntry = _nodeStateStore.EntryAtIndex(0);
        lastLogEntry.ShouldNotBeNull();
        lastLogEntry.Term.ShouldBe(0);
        lastLogEntry.Command.ShouldBe(new Command("A", CommandOperation.Assignment, 1));
    }

    [Test]
    public void FollowerShouldReplyFailIfCurrentTermHigherThanRequestTerm()
    {
        var entry = CreateLogEntryCommandRequest("A", "=", 1);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            EntryCommands = { new[] { entry } }
        };
        _nodeStateStore.CurrentTerm = 1;
        
        var reply = _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        reply.Result.Success.ShouldBeFalse();
        _nodeStateStore.LogLength.ShouldBe(0);
    }

    [Test]
    public void FollowerShouldUpdateCommitIndex()
    {
        var entry = CreateLogEntryCommandRequest("A", "=", 1);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            LeaderCommit = 0,
            EntryCommands = { new[] { entry } }
        };
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.CommitIndex.ShouldBe(0);
    }

    private static CommandRequest CreateLogEntryCommandRequest(string variable, string operation, int literal)
    {
        return new CommandRequest() {Variable = variable, Operation = operation, Literal = literal };
    }

    private Command CreateLogEntryCommand(string variable, string operation, int literal)
    {
        return new Command(variable, operation.ToOperationType(), literal);
    }
}