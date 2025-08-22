namespace Edi.MIDIPlayer.Interfaces;

public interface IMidiDeviceWrapper : IDisposable
{
    int NumberOfDevices { get; }
    void Send(int rawData);
}