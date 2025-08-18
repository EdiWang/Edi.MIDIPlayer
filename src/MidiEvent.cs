namespace Edi.MIDIPlayer;

public class MidiEvent
{
    public int Ticks { get; set; }
    public byte EventType { get; set; }
    public byte[] Data { get; set; } = [];
}