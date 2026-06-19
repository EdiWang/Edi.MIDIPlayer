namespace Edi.MIDIPlayer.Models;

internal sealed class ActiveNoteTracker
{
    private readonly Dictionary<ActiveNoteKey, int> activeNotes = [];

    public int ActiveCount => activeNotes.Count;

    public void NoteOn(int channel, int noteNumber)
    {
        var key = new ActiveNoteKey(channel, noteNumber);
        activeNotes.TryGetValue(key, out var count);
        activeNotes[key] = count + 1;
    }

    public void NoteOff(int channel, int noteNumber)
    {
        var key = new ActiveNoteKey(channel, noteNumber);
        if (!activeNotes.TryGetValue(key, out var count))
        {
            return;
        }

        if (count <= 1)
        {
            activeNotes.Remove(key);
            return;
        }

        activeNotes[key] = count - 1;
    }

    private readonly record struct ActiveNoteKey(int Channel, int NoteNumber);
}
