using NAudio.Midi;

namespace Edi.MIDIPlayer.Tests;

public class TempoManagerTests
{
    [Fact]
    public void BuildTempoMap_EmptyEventList_ReturnsDefaultTempo()
    {
        // Arrange
        var emptyEvents = new List<MidiEventInfo>();

        // Act
        var result = TempoManager.BuildTempoMap(emptyEvents);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].Tick);
        Assert.Equal(500000, result[0].MicrosecondsPerQuarterNote); // 120 BPM
    }

    [Fact]
    public void BuildTempoMap_NoTempoEvents_ReturnsOnlyDefaultTempo()
    {
        // Arrange
        var events = new List<MidiEventInfo>
        {
            new() { AbsoluteTime = 100, Event = new NoteOnEvent(0, 1, 60, 100, 0) },
            new() { AbsoluteTime = 200, Event = new NoteOnEvent(0, 1, 60, 100, 0) }
        };

        // Act
        var result = TempoManager.BuildTempoMap(events);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].Tick);
        Assert.Equal(500000, result[0].MicrosecondsPerQuarterNote);
    }

    [Fact]
    public void BuildTempoMap_SingleTempoEvent_ReturnsDefaultPlusTempoChange()
    {
        // Arrange
        var tempoEvent = new TempoEvent(600000, 0); // 100 BPM
        var events = new List<MidiEventInfo>
        {
            new() { AbsoluteTime = 480, Event = tempoEvent }
        };

        // Act
        var result = TempoManager.BuildTempoMap(events);

        // Assert
        Assert.Equal(2, result.Count);
        
        // Default tempo at tick 0
        Assert.Equal(0, result[0].Tick);
        Assert.Equal(500000, result[0].MicrosecondsPerQuarterNote);
        
        // New tempo at tick 480
        Assert.Equal(480, result[1].Tick);
        Assert.Equal(600000, result[1].MicrosecondsPerQuarterNote);
    }

    [Fact]
    public void BuildTempoMap_MultipleTempoEvents_ReturnsAllTempoChanges()
    {
        // Arrange
        var tempo1 = new TempoEvent(600000, 0); // 100 BPM
        var tempo2 = new TempoEvent(400000, 0); // 150 BPM
        var events = new List<MidiEventInfo>
        {
            new() { AbsoluteTime = 480, Event = tempo1 },
            new() { AbsoluteTime = 960, Event = tempo2 },
            new() { AbsoluteTime = 500, Event = new NoteOnEvent(0, 1, 60, 100, 0) } // Non-tempo event
        };

        // Act
        var result = TempoManager.BuildTempoMap(events);

        // Assert
        Assert.Equal(3, result.Count);
        
        Assert.Equal(0, result[0].Tick);
        Assert.Equal(500000, result[0].MicrosecondsPerQuarterNote);
        
        Assert.Equal(480, result[1].Tick);
        Assert.Equal(600000, result[1].MicrosecondsPerQuarterNote);
        
        Assert.Equal(960, result[2].Tick);
        Assert.Equal(400000, result[2].MicrosecondsPerQuarterNote);
    }

    [Fact]
    public void BuildTempoMap_TempoEventAtTickZero_ReplacesDefaultTempo()
    {
        // Arrange
        var tempoEvent = new TempoEvent(750000, 0); // 80 BPM
        var events = new List<MidiEventInfo>
        {
            new() { AbsoluteTime = 0, Event = tempoEvent }
        };

        // Act
        var result = TempoManager.BuildTempoMap(events);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Tick);
        Assert.Equal(500000, result[0].MicrosecondsPerQuarterNote); // Default still added first
        Assert.Equal(0, result[1].Tick);
        Assert.Equal(750000, result[1].MicrosecondsPerQuarterNote); // New tempo at same tick
    }

    [Theory]
    [InlineData(0, 480, 500000, 500)] // 0.5 seconds at 120 BPM
    [InlineData(480, 480, 500000, 1000)] // 1 second at 120 BPM, starting from tick 480
    [InlineData(0, 480, 250000, 250)] // 0.25 seconds at 240 BPM
    [InlineData(0, 480, 1000000, 1000)] // 1 second at 60 BPM
    public void TicksToTimeSpan_SingleTempoSegment_CalculatesCorrectTime(
        long startTick, int ticksPerQuarterNote, int microsecondsPerQuarter, int expectedMilliseconds)
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = microsecondsPerQuarter }
        };

        // Act
        var result = TempoManager.TicksToTimeSpan(startTick + ticksPerQuarterNote, tempoMap, ticksPerQuarterNote);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), result);
    }

    [Fact]
    public void TicksToTimeSpan_ZeroTicks_ReturnsZeroTime()
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }
        };

        // Act
        var result = TempoManager.TicksToTimeSpan(0, tempoMap, 480);

        // Assert
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void TicksToTimeSpan_MultipleTempoChanges_CalculatesCorrectTime()
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }, // 120 BPM
            new() { Tick = 480, MicrosecondsPerQuarterNote = 600000 }, // 100 BPM at tick 480
            new() { Tick = 960, MicrosecondsPerQuarterNote = 400000 }  // 150 BPM at tick 960
        };
        var ticksPerQuarterNote = 480;

        // Act - Calculate time for tick 1440 (3 quarter notes)
        var result = TempoManager.TicksToTimeSpan(1440, tempoMap, ticksPerQuarterNote);

        // Assert
        // First 480 ticks: 480 * (500000 / 480) = 500000 microseconds = 500 ms
        // Next 480 ticks: 480 * (600000 / 480) = 600000 microseconds = 600 ms  
        // Next 480 ticks: 480 * (400000 / 480) = 400000 microseconds = 400 ms
        // Total: 1500 ms
        Assert.Equal(TimeSpan.FromMilliseconds(1500), result);
    }

    [Fact]
    public void TicksToTimeSpan_TicksEndBeforeLastTempoChange_CalculatesPartialTime()
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }, // 120 BPM
            new() { Tick = 480, MicrosecondsPerQuarterNote = 600000 }, // 100 BPM at tick 480
            new() { Tick = 960, MicrosecondsPerQuarterNote = 400000 }  // 150 BPM at tick 960 (not reached)
        };
        var ticksPerQuarterNote = 480;

        // Act - Calculate time for tick 720 (1.5 quarter notes)
        var result = TempoManager.TicksToTimeSpan(720, tempoMap, ticksPerQuarterNote);

        // Assert
        // First 480 ticks: 480 * (500000 / 480) = 500000 microseconds = 500 ms
        // Next 240 ticks: 240 * (600000 / 480) = 300000 microseconds = 300 ms
        // Total: 800 ms
        Assert.Equal(TimeSpan.FromMilliseconds(800), result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TicksToTimeSpan_NegativeTicks_ReturnsZeroTime(long negativeTicks)
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }
        };

        // Act
        var result = TempoManager.TicksToTimeSpan(negativeTicks, tempoMap, 480);

        // Assert
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void TicksToTimeSpan_VeryLargeTicks_HandlesCorrectly()
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 }
        };
        var ticksPerQuarterNote = 480;
        var largeTicks = 480000; // 1000 quarter notes

        // Act
        var result = TempoManager.TicksToTimeSpan(largeTicks, tempoMap, ticksPerQuarterNote);

        // Assert
        // 1000 quarter notes at 120 BPM = 500 seconds
        Assert.Equal(TimeSpan.FromSeconds(500), result);
    }

    [Theory]
    [InlineData(96)]   // Low resolution
    [InlineData(480)]  // Standard resolution
    [InlineData(960)]  // High resolution
    [InlineData(1920)] // Very high resolution
    public void TicksToTimeSpan_DifferentTickResolutions_ScalesCorrectly(int ticksPerQuarterNote)
    {
        // Arrange
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 } // 120 BPM
        };

        // Act - One quarter note worth of ticks
        var result = TempoManager.TicksToTimeSpan(ticksPerQuarterNote, tempoMap, ticksPerQuarterNote);

        // Assert - Should always be 500ms regardless of resolution
        Assert.Equal(TimeSpan.FromMilliseconds(500), result);
    }
}