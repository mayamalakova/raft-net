using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using Raft.Node.Communication.Services;
using Raft.Node.Extensions;
using Raft.Store;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
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
        _nodeStateStore.AppendLogEntry(CreateLogEntry("B", "=", 1, termAtIndex));
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
    public void FollowerShouldReplyFailIfCurrentTermHigherThanRequestTerm()
    {
        var entry = CreateLogEntryMessage("A", "=", 1, 0);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            Entries = { new[] { entry } }
        };
        _nodeStateStore.CurrentTerm = 1;
        
        var reply = _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        reply.Result.Success.ShouldBeFalse();
        _nodeStateStore.LogLength.ShouldBe(0);
    }

    [Test]
    public void FollowerShouldRemoveConflictingEntries()
    {
        var oldEntry = CreateLogEntry("B", "=", 2, 1);
        _nodeStateStore.AppendLogEntry(oldEntry);
        var newEntry = CreateLogEntryMessage("A", "=", 1, 0);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            Entries = { new[] { newEntry } }
        };
        
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.LogLength.ShouldBe(1);
        var lastLogEntry = _nodeStateStore.EntryAtIndex(0);
        lastLogEntry.ShouldBe(newEntry.FromMessage());
    }

    [Test]
    public void FollowerShouldAppendEntriesIfSuccessful()
    {
        var entry = CreateLogEntryMessage("A", "=", 1, 0);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            Entries = { new[] { entry } }
        };
        
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.LogLength.ShouldBe(1);
        var lastLogEntry = _nodeStateStore.EntryAtIndex(0);
        lastLogEntry.ShouldNotBeNull();
        lastLogEntry.Term.ShouldBe(0);
        lastLogEntry.Command.ShouldBe(new Command("A", CommandOperation.Assignment, 1));
    }

    [Test]
    public void FollowerShouldOverrideExistingEntriesThatTakeNewEntriesPlace()
    {
        var oldEntry = CreateLogEntry("B", "=", 2, 1);
        _nodeStateStore.AppendLogEntry(oldEntry);
        var newLogEntry = CreateLogEntryMessage("A", "=", 1, 1);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 1,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            Entries = { new[] { newLogEntry } }
        };
        
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.LogLength.ShouldBe(1);
        var lastLogEntry = _nodeStateStore.EntryAtIndex(0);
        lastLogEntry.ShouldBe(newLogEntry.FromMessage());
        // lastLogEntry.Term.ShouldBe(1);
        // lastLogEntry.Command.ShouldBe(new Command("A", CommandOperation.Assignment, 1));
    }

    [Test]
    public void FollowerShouldUpdateCommitIndex()
    {
        var entry = CreateLogEntryMessage("A", "=", 1, 0);

        var appendEntriesRequest = new AppendEntriesRequest()
        {
            Term = 0,
            PrevLogIndex = -1,
            PrevLogTerm = -1,
            LeaderCommit = 0,
            Entries = { new[] { entry } }
        };
        _service.AppendEntries(appendEntriesRequest, Substitute.For<ServerCallContext>());

        _nodeStateStore.CommitIndex.ShouldBe(0);
    }

    private static LogEntryMessage CreateLogEntryMessage(string variable, string operation, int literal, int term)
    {
        
        var command = new CommandRequest() {Variable = variable, Operation = operation, Literal = literal };
        return new LogEntryMessage()
        {
            Command = command,
            Term = term
        };
    }

    private LogEntry CreateLogEntry(string variable, string operation, int literal, int term)
    {
        var command = new Command(variable, operation.ToOperationType(), literal);
        return new LogEntry(command, term);
    }
}