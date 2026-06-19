using Edi.MIDIPlayer.Services;
using System.Diagnostics;
using System.Net;
using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class FileDownloaderServiceTests
{
    [Fact]
    public async Task DownloadAsync_ReturnsResponseBytes()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        using var httpClient = CreateHttpClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            };
            response.Content.Headers.ContentLength = payload.Length;
            return Task.FromResult(response);
        });
        var downloader = new FileDownloaderService(httpClient);

        var result = await downloader.DownloadAsync("https://example.com/song.mid", TimeSpan.FromSeconds(1));

        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task DownloadAsync_RejectsContentLengthAtLimit()
    {
        using var httpClient = CreateHttpClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([])
            };
            response.Content.Headers.ContentLength = FileDownloaderService.MaxDownloadBytes;
            return Task.FromResult(response);
        });
        var downloader = new FileDownloaderService(httpClient);

        var exception = await Assert.ThrowsAsync<FileDownloadException>(
            () => downloader.DownloadAsync("https://example.com/song.mid", TimeSpan.FromSeconds(1)));

        Assert.Contains("smaller than 10 MB", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_RejectsStreamingResponseAtLimit()
    {
        using var httpClient = CreateHttpClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[FileDownloaderService.MaxDownloadBytes])
            };
            return Task.FromResult(response);
        });
        var downloader = new FileDownloaderService(httpClient);

        var exception = await Assert.ThrowsAsync<FileDownloadException>(
            () => downloader.DownloadAsync("https://example.com/song.mid", TimeSpan.FromSeconds(1)));

        Assert.Contains("smaller than 10 MB", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_ConvertsCancellationFromTimeoutToTimeoutException()
    {
        using var httpClient = CreateHttpClient(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new UnreachableException();
        });
        var downloader = new FileDownloaderService(httpClient);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => downloader.DownloadAsync("https://example.com/song.mid", TimeSpan.FromMilliseconds(10)));

        Assert.Contains("timed out", exception.Message);
    }

    private static HttpClient CreateHttpClient(Func<CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        return new HttpClient(new StubHttpMessageHandler(sendAsync));
    }

    private sealed class StubHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            sendAsync(cancellationToken);
    }
}
