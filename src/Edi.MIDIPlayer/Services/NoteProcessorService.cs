using Edi.MIDIPlayer.Interfaces;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class NoteProcessorService(IConsoleDisplay consoleDisplay) : INoteProcessor
{
    public void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = MidiDisplayFormatter.GetNoteName(noteEvent.NoteNumber);
        var velocityBar = consoleDisplay.CreateVelocityBar(noteEvent.Velocity);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = MidiDisplayFormatter.GetNoteColor(noteEvent.NoteNumber);
            Console.Write("▲ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("NOTE_ON  ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{noteEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"VEL:0x{noteEvent.Velocity:X2} ");
            Console.ResetColor();

            Console.Write(velocityBar);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public void DisplayNoteOff(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = MidiDisplayFormatter.GetNoteName(noteEvent.NoteNumber);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("▼ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("NOTE_OFF ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{noteEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("VEL:0x00 ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("░░░░░░░░░░");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount)
    {
        var controllerName = MidiDisplayFormatter.GetControllerName(controlEvent.Controller);
        var valueBar = consoleDisplay.CreateVelocityBar(controlEvent.ControllerValue);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("◄ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("CTRL_CHG ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{controllerName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{controlEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"VAL:0x{controlEvent.ControllerValue:X2} ");
            Console.ResetColor();

            Console.Write(valueBar);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ CTRL: 0x{(int)controlEvent.Controller:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public void SendNotePreview(int noteNumber, int velocity, int channel, double delayMs)
    {
        throw new NotImplementedException();
    }
}
