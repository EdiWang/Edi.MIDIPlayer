using NAudio.Midi;

namespace Edi.MIDIPlayer.Interfaces;

public interface INoteProcessor
{
    string GetNoteName(int noteNumber);
    ConsoleColor GetNoteColor(int noteNumber);
    void SendNotePreview(int noteNumber, int velocity, int channel, double delayMs);
    void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount);
    void DisplayNoteOff(string timestamp, NoteEvent noteEvent, int activeNotesCount);
    void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount);
}