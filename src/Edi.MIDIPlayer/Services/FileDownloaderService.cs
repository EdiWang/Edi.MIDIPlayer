using Edi.MIDIPlayer.Interfaces;

namespace Edi.MIDIPlayer.Services;

public class FileDownloaderService(HttpClient httpClient) : IFileDownloader
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<byte[]> DownloadAsync(string url, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        
        try
        {
            var response = await _httpClient.GetAsync(url, timeoutCts.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"The request to {url} timed out after {timeout.TotalSeconds} seconds.");
        }
    }
}