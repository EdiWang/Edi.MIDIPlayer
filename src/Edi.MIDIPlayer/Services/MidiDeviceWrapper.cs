using Edi.MIDIPlayer.Interfaces;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class MidiDeviceWrapper(int deviceId = 0) : IMidiDeviceWrapper
{
    private readonly MidiOut _midiOut = new(deviceId);

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