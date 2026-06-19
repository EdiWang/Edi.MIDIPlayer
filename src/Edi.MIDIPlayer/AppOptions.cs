namespace Edi.MIDIPlayer;

internal enum DisplayMode
{
    Web,
    Console
}

internal sealed record AppOptions(
    DisplayMode DisplayMode,
    string[] MidiArgs,
    string[] HostArgs,
    string? WebUrls,
    bool PauseOnExit,
    bool ShowHelp)
{
    public static AppOptions Parse(string[] args)
    {
        var displayMode = DisplayMode.Web;
        var midiArgs = new List<string>();
        var hostArgs = new List<string>();
        string? webUrls = null;
        var pauseOnExit = false;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (IsHelpOption(arg))
            {
                showHelp = true;
                continue;
            }

            if (TryReadInlineMode(arg, out var inlineMode))
            {
                displayMode = ParseDisplayMode(inlineMode);
                continue;
            }

            if (IsModeOption(arg))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"{arg} requires a value: web or console.");
                }

                displayMode = ParseDisplayMode(args[++i]);
                continue;
            }

            if (arg.Equals("--web", StringComparison.OrdinalIgnoreCase))
            {
                displayMode = DisplayMode.Web;
                continue;
            }

            if (arg.Equals("--console", StringComparison.OrdinalIgnoreCase))
            {
                displayMode = DisplayMode.Console;
                continue;
            }

            if (arg.Equals("--pause-on-exit", StringComparison.OrdinalIgnoreCase))
            {
                pauseOnExit = true;
                continue;
            }

            if (IsKnownHostOptionWithValue(arg))
            {
                var option = arg.Split('=', 2)[0];
                string? optionValue = null;

                hostArgs.Add(arg);
                if (arg.Contains('='))
                {
                    optionValue = arg.Split('=', 2)[1];
                }
                else if (i + 1 < args.Length)
                {
                    optionValue = args[++i];
                    hostArgs.Add(optionValue);
                }

                if (option.Equals("--urls", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(optionValue))
                    {
                        throw new ArgumentException("--urls requires a value.");
                    }

                    webUrls = optionValue;
                }

                continue;
            }

            hostArgs.Add(arg);

            if (!arg.StartsWith('-'))
            {
                midiArgs.Add(arg);
            }
        }

        return new AppOptions(displayMode, [.. midiArgs], [.. hostArgs], webUrls, pauseOnExit, showHelp);
    }

    private static bool IsHelpOption(string arg) =>
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("/?", StringComparison.OrdinalIgnoreCase);

    private static bool IsModeOption(string arg) =>
        arg.Equals("--display", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--ui", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadInlineMode(string arg, out string value)
    {
        foreach (var option in new[] { "--display=", "--mode=", "--ui=" })
        {
            if (arg.StartsWith(option, StringComparison.OrdinalIgnoreCase))
            {
                value = arg[option.Length..];
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool IsKnownHostOptionWithValue(string arg)
    {
        var option = arg.Split('=', 2)[0];
        return option.Equals("--urls", StringComparison.OrdinalIgnoreCase) ||
               option.Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
               option.Equals("--contentRoot", StringComparison.OrdinalIgnoreCase) ||
               option.Equals("--webroot", StringComparison.OrdinalIgnoreCase) ||
               option.Equals("--applicationName", StringComparison.OrdinalIgnoreCase);
    }

    private static DisplayMode ParseDisplayMode(string value) =>
        value.ToLowerInvariant() switch
        {
            "web" or "browser" or "signalr" => DisplayMode.Web,
            "console" or "terminal" or "cli" or "cmd" or "commandline" or "terminal-ui" => DisplayMode.Console,
            _ => throw new ArgumentException($"Unknown display mode '{value}'. Use web or console.")
        };
}
