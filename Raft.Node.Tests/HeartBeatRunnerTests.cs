using NUnit.Framework;
using Raft.Node.HeatBeat;
using Shouldly;

namespace Raft.Node.Tests;

class BeatSink
{
    public int BeatsCount { get; set; } = 0;

    public void SendBeat()
    {
        BeatsCount++;
    }
}

public class HeartBeatRunnerTests
{
    private BeatSink _beatSink;
    private HeartBeatRunner _heartBeatRunner;

    [SetUp]
    public void SetUp()
    {
        _beatSink = new BeatSink();
        _heartBeatRunner = new HeartBeatRunner(50, _beatSink.SendBeat);
    }

    [Test]
    public async Task ShouldBeat()
    {
        _heartBeatRunner.StartBeating();
        await Task.Delay(100);
        _beatSink.BeatsCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task ShouldStopBeating()
    {
        _heartBeatRunner.StartBeating();
        _heartBeatRunner.StopBeating();
        var oldBeatCount = _beatSink.BeatsCount;
        await Task.Delay(50);
        _beatSink.BeatsCount.ShouldBe(oldBeatCount);
    }

}