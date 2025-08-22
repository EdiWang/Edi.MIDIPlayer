using Edi.MIDIPlayer.Interfaces;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class TempoManagerService : ITempoManager
{
    private readonly IConsoleDisplay _consoleDisplay;

    public TempoManagerService(IConsoleDisplay consoleDisplay)
    {
        _consoleDisplay = consoleDisplay;
    }

    public List<TempoChange> BuildTempoMap(List<MidiEventInfo> allEvents)
    {
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 } // Default 120 BPM
        };

        foreach (var eventInfo in allEvents)
        {
            if (eventInfo.Event is MetaEvent meta && meta.MetaEventType == MetaEventType.SetTempo)
            {
                var tempoEvent = (TempoEvent)meta;
                tempoMap.Add(new TempoChange
                {
                    Tick = eventInfo.AbsoluteTime,
                    MicrosecondsPerQuarterNote = tempoEvent.MicrosecondsPerQuarterNote
                });

                var bpm = 60000000.0 / tempoEvent.MicrosecondsPerQuarterNote;
                _consoleDisplay.WriteMessage("TEMPO", $"BPM: {bpm:F1} (0x{tempoEvent.MicrosecondsPerQuarterNote:X} ¦Ìs/quarter)", ConsoleColor.Magenta);
            }
        }

        return tempoMap;
    }

    public TimeSpan TicksToTimeSpan(long ticks, List<TempoChange> tempoMap, int ticksPerQuarterNote)
    {
        var totalMicroseconds = 0.0;
        var currentTick = 0L;

        for (int i = 0; i < tempoMap.Count; i++)
        {
            var tempoChange = tempoMap[i];
            var nextTick = (i + 1 < tempoMap.Count) ? tempoMap[i + 1].Tick : ticks;

            if (nextTick > ticks)
                nextTick = ticks;

            if (nextTick > currentTick)
            {
                var ticksInThisSegment = nextTick - currentTick;
                var microsecondsPerTick = (double)tempoChange.MicrosecondsPerQuarterNote / ticksPerQuarterNote;
                totalMicroseconds += ticksInThisSegment * microsecondsPerTick;
            }

            currentTick = nextTick;
            if (currentTick >= ticks)
                break;
        }

        return TimeSpan.FromMilliseconds(totalMicroseconds / 1000.0);
    }
}