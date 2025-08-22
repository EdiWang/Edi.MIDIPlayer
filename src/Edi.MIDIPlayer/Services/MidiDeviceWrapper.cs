using Edi.MIDIPlayer.Interfaces;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class MidiDeviceWrapper : IMidiDeviceWrapper
{
    private readonly MidiOut _midiOut;

    public MidiDeviceWrapper(int deviceId = 0)
    {
        _midiOut = new MidiOut(deviceId);
    }

    public int NumberOfDevices => MidiOut.NumberOfDevices;

    public void Send(int rawData)
    {
        _midiOut.Send(rawData);
    }

    public void Dispose()
    {
        _midiOut?.Dispose();
    }
}