namespace Edi.MIDIPlayer.Models;

public class TempoChange
{
    public long Tick { get; set; }
    public int MicrosecondsPerQuarterNote { get; set; }
}