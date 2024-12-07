using NUnit.Framework;
using Raft.Store.Domain;
using Raft.Store.Memory;
using Shouldly;

namespace Raft.Store.Tests;

[TestFixture]
public class ClusterNodeStoreTests
{
    private ClusterNodeStore _clusterNodeStore;

    [SetUp]
    public void SetUp()
    {
        _clusterNodeStore = new ClusterNodeStore();
    }

    [Test]
    public void ShouldHaveInitialValueForNextIndex()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(0);
    }

    [Test]
    public void ShouldHaveInitialValueForMatchingIndex()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        _clusterNodeStore.GetMatchingIndex("someNode").ShouldBe(-1);
    }
    
    [Test]
    public void ShouldIncreaseNextIndex()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        
        _clusterNodeStore.IncreaseNextLogIndex("someNode", 2);
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(2);
        
        _clusterNodeStore.IncreaseNextLogIndex("someNode", 1);
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(3);
    }

    [Test]
    public void ShouldDecreaseIndex()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        
        _clusterNodeStore.IncreaseNextLogIndex("someNode", 2);
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(2);
        
        _clusterNodeStore.DecreaseNextLogIndex("someNode");
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(1);
    }

    [Test]
    public void ShouldNotDecreaseNextIndexPastZero()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        
        _clusterNodeStore.DecreaseNextLogIndex("someNode");
        
        _clusterNodeStore.GetNextIndex("someNode").ShouldBe(0);
    }

    [Test]
    public void ShouldSetMatchingIndex()
    {
        _clusterNodeStore.AddNode("someNode", new NodeAddress("someHost", 666));
        _clusterNodeStore.SetMatchingIndex("someNode", 1);
        _clusterNodeStore.GetMatchingIndex("someNode").ShouldBe(1);
    }
}