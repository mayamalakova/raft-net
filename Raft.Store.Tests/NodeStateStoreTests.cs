using NUnit.Framework;
using Raft.Store.Domain;
using Raft.Store.Domain.Replication;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Store.Tests;

public class NodeStateStoreTests
{
    [Test]
    public void ShouldGetLastEntries()
    {
        var nodeStateStore = new NodeStateStore();
        var entry1 = new LogEntry(new Command("a", CommandOperation.Assignment, 1), 0);
        var entry2 = new LogEntry(new Command("b", CommandOperation.Assignment, 1), 0);
        nodeStateStore.AppendLogEntry(entry1);
        nodeStateStore.AppendLogEntry(entry2);
        
        nodeStateStore.GetLastEntries(2).ShouldBe([entry1, entry2]);
        nodeStateStore.GetLastEntries(1).ShouldBe([entry2]);
    }
}