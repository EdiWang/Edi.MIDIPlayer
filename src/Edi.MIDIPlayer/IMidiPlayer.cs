
namespace Edi.MIDIPlayer
{
    public interface IMidiPlayer
    {
        static abstract Task PlayMidiFileAsync(string fileUrl);
    }
}