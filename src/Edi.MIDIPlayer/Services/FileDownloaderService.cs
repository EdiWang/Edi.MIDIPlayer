using Edi.MIDIPlayer.Interfaces;

namespace Edi.MIDIPlayer.Services;

public class FileDownloaderService(HttpClient httpClient) : IFileDownloader
{
    public const int MaxDownloadBytes = 10 * 1024 * 1024;

    private readonly HttpClient _httpClient = httpClient;

    public async Task<byte[]> DownloadAsync(string url, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);

        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength >= MaxDownloadBytes)
            {
                throw new FileDownloadException("Remote MIDI files must be smaller than 10 MB.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var memoryStream = new MemoryStream();
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await responseStream.ReadAsync(buffer, timeoutCts.Token)) > 0)
            {
                if (memoryStream.Length + bytesRead >= MaxDownloadBytes)
                {
                    throw new FileDownloadException("Remote MIDI files must be smaller than 10 MB.");
                }

                memoryStream.Write(buffer, 0, bytesRead);
            }

            return memoryStream.ToArray();
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"The request to {url} timed out after {timeout.TotalSeconds} seconds.");
        }
    }
}

internal sealed class FileDownloadException(string message) : Exception(message);
