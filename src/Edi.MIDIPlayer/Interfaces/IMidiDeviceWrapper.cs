namespace Edi.MIDIPlayer.Interfaces;

public interface IMidiDeviceWrapper : IDisposable
{
    void Send(int rawData);
}
