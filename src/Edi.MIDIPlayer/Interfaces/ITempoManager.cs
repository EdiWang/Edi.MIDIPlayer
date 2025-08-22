using Edi.MIDIPlayer.Models;

namespace Edi.MIDIPlayer.Interfaces;

public interface ITempoManager
{
    List<TempoChange> BuildTempoMap(List<MidiEventInfo> allEvents);
    TimeSpan TicksToTimeSpan(long ticks, List<TempoChange> tempoMap, int ticksPerQuarterNote);
}