using Edi.MIDIPlayer.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class SignalRSendObserverTests
{
    [Fact]
    public async Task ObserveAsync_DoesNotLogSuccessfulSend()
    {
        var logger = new TestLogger();

        await SignalRSendObserver.ObserveAsync(Task.CompletedTask, logger, "ReceiveMessage");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ObserveAsync_LogsSendFailureWithoutThrowing()
    {
        var logger = new TestLogger();
        var exception = new InvalidOperationException("send failed");

        await SignalRSendObserver.ObserveAsync(Task.FromException(exception), logger, "ReceiveNoteOn");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Contains("ReceiveNoteOn", entry.Message);
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);
}
