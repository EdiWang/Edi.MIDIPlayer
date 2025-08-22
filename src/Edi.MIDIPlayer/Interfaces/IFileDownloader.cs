namespace Edi.MIDIPlayer.Interfaces;

public interface IFileDownloader
{
    Task<byte[]> DownloadAsync(string url, TimeSpan timeout);
}