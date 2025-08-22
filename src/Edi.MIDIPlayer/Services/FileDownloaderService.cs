using Edi.MIDIPlayer.Interfaces;

namespace Edi.MIDIPlayer.Services;

public class FileDownloaderService : IFileDownloader
{
    private readonly HttpClient _httpClient;

    public FileDownloaderService()
    {
        _httpClient = new HttpClient();
    }

    public FileDownloaderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> DownloadAsync(string url, TimeSpan timeout)
    {
        _httpClient.Timeout = timeout;
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}