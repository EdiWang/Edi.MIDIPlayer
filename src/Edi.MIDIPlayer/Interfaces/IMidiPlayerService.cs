namespace Edi.MIDIPlayer.Interfaces;

public interface IMidiPlayerService
{
    Task PlayMidiFileAsync(string fileUrl);
}