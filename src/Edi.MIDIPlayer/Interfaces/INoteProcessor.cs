using NAudio.Midi;

namespace Edi.MIDIPlayer.Interfaces;

public interface INoteProcessor
{
    string GetNoteName(int noteNumber);
    ConsoleColor GetNoteColor(int noteNumber);
    void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount);
    void DisplayNoteOff(string timestamp, NoteEvent noteEvent, int activeNotesCount);
    void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount);
}