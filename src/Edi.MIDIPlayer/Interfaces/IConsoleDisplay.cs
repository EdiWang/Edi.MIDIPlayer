namespace Edi.MIDIPlayer.Interfaces;

public interface IConsoleDisplay : IDisplayService
{
    string CreateVelocityBar(int velocity);
    Lock GetConsoleLock();
}
