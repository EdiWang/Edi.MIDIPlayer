using NAudio.Midi;

namespace Edi.MIDIPlayer;

public class MidiEventInfo
{
    public long AbsoluteTime { get; set; }
    public MidiEvent Event { get; set; } = null!;
}
