using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class AppOptionsTests
{
    [Fact]
    public void Parse_UsesWebDisplayByDefault()
    {
        var options = AppOptions.Parse(["song.mid"]);

        Assert.Equal(DisplayMode.Web, options.DisplayMode);
        Assert.Equal(["song.mid"], options.MidiArgs);
        Assert.Equal(["song.mid"], options.HostArgs);
        Assert.Null(options.WebUrls);
        Assert.False(options.PauseOnExit);
        Assert.False(options.ShowHelp);
    }

    [Theory]
    [InlineData("--console")]
    [InlineData("--display", "console")]
    [InlineData("--mode=terminal")]
    [InlineData("--ui", "cli")]
    public void Parse_RecognizesConsoleAliases(params string[] displayArgs)
    {
        var args = displayArgs.Concat(["song.mid"]).ToArray();

        var options = AppOptions.Parse(args);

        Assert.Equal(DisplayMode.Console, options.DisplayMode);
        Assert.Equal(["song.mid"], options.MidiArgs);
    }

    [Theory]
    [InlineData("--web")]
    [InlineData("--display", "web")]
    [InlineData("--mode=browser")]
    [InlineData("--ui", "signalr")]
    public void Parse_RecognizesWebAliases(params string[] displayArgs)
    {
        var args = displayArgs.Concat(["song.mid"]).ToArray();

        var options = AppOptions.Parse(args);

        Assert.Equal(DisplayMode.Web, options.DisplayMode);
        Assert.Equal(["song.mid"], options.MidiArgs);
    }

    [Theory]
    [InlineData("--urls", "http://localhost:5050")]
    [InlineData("--urls=http://localhost:5050")]
    public void Parse_CapturesWebUrls(params string[] urlArgs)
    {
        var args = urlArgs.Concat(["song.mid"]).ToArray();

        var options = AppOptions.Parse(args);

        Assert.Equal("http://localhost:5050", options.WebUrls);
        Assert.Contains("--urls", options.HostArgs[0]);
        Assert.Equal(["song.mid"], options.MidiArgs);
    }

    [Fact]
    public void Parse_DoesNotTreatHostOptionValueAsMidiArgument()
    {
        var options = AppOptions.Parse(["--environment", "Development", "song.mid"]);

        Assert.Equal(["song.mid"], options.MidiArgs);
        Assert.Equal(["--environment", "Development", "song.mid"], options.HostArgs);
    }

    [Fact]
    public void Parse_CapturesPauseOnExitWithoutForwardingItToHostOrMidiArgs()
    {
        var options = AppOptions.Parse(["--display", "console", "--pause-on-exit", "song.mid"]);

        Assert.True(options.PauseOnExit);
        Assert.Equal(["song.mid"], options.MidiArgs);
        Assert.Equal(["song.mid"], options.HostArgs);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("/?")]
    public void Parse_RecognizesHelp(string helpArg)
    {
        var options = AppOptions.Parse([helpArg]);

        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void Parse_ThrowsForMissingDisplayValue()
    {
        var exception = Assert.Throws<ArgumentException>(() => AppOptions.Parse(["--display"]));

        Assert.Contains("requires a value", exception.Message);
    }

    [Fact]
    public void Parse_ThrowsForMissingUrlsValue()
    {
        var exception = Assert.Throws<ArgumentException>(() => AppOptions.Parse(["--urls"]));

        Assert.Contains("--urls requires a value", exception.Message);
    }

    [Fact]
    public void Parse_ThrowsForUnknownDisplayMode()
    {
        var exception = Assert.Throws<ArgumentException>(() => AppOptions.Parse(["--display", "neon"]));

        Assert.Contains("Unknown display mode", exception.Message);
    }
}
