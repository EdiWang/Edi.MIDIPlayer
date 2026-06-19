using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Models;
using Edi.MIDIPlayer.Services;
using Moq;
using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class TempoManagerServiceTests
{
    [Fact]
    public void TicksToTimeSpan_UsesDefaultTempo()
    {
        var tempoManager = CreateTempoManager();
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }
        };

        var result = tempoManager.TicksToTimeSpan(480, tempoMap, ticksPerQuarterNote: 480);

        Assert.Equal(TimeSpan.FromMilliseconds(500), result);
    }

    [Fact]
    public void TicksToTimeSpan_AppliesTempoChanges()
    {
        var tempoManager = CreateTempoManager();
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 },
            new() { Tick = 480, MicrosecondsPerQuarterNote = 1000000 }
        };

        var result = tempoManager.TicksToTimeSpan(960, tempoMap, ticksPerQuarterNote: 480);

        Assert.Equal(TimeSpan.FromMilliseconds(1500), result);
    }

    [Fact]
    public void BuildTempoMap_ReturnsDefaultTempoWhenNoTempoEventsExist()
    {
        var tempoManager = CreateTempoManager();

        var result = tempoManager.BuildTempoMap([]);

        var tempoChange = Assert.Single(result);
        Assert.Equal(0, tempoChange.Tick);
        Assert.Equal(500000, tempoChange.MicrosecondsPerQuarterNote);
    }

    private static TempoManagerService CreateTempoManager()
    {
        return new TempoManagerService(Mock.Of<IDisplayService>());
    }
}
