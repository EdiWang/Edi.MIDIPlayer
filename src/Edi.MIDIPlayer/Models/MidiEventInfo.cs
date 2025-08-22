using NAudio.Midi;

namespace Edi.MIDIPlayer.Models;

public class MidiEventInfo
{
    public long AbsoluteTime { get; set; }
    public MidiEvent Event { get; set; } = null!;
}
