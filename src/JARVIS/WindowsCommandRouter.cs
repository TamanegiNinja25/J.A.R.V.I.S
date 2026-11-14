using System.Diagnostics;

namespace Jarvis;

// Exact-phrase allowlist, checked before the AI. Never pass user or model text to cmd.exe or PowerShell.
public static class WindowsCommandRouter
{
    private static readonly Dictionary<string, string> Apps = new()
    {
        ["open notepad"] = "notepad.exe",
        ["open calculator"] = "calc.exe",
        ["open explorer"] = "explorer.exe",
        ["open file explorer"] = "explorer.exe",
        ["open task manager"] = "taskmgr.exe",
        ["open paint"] = "mspaint.exe",
    };

    /// <summary>Returns a reply if the input is an allowlisted command; otherwise null.</summary>
    public static string? TryExecute(string input)
    {
        // Speech recognition adds punctuation ("Open notepad."), so strip it before matching.
        var text = input.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();
        if (text.StartsWith("launch ")) text = "open " + text["launch ".Length..];

        if (Apps.TryGetValue(text, out var exe))
        {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            return $"Certainly. Launching {Path.GetFileNameWithoutExtension(exe)}.";
        }

        return text switch
        {
            "what time is it" or "tell me the time" => $"It is {DateTime.Now:dddd, h:mm tt}.",
            "system status" => $"Running on {Environment.OSVersion} ({Environment.MachineName}) with "
                + $"{Environment.ProcessorCount} logical processors. Live telemetry is on the dashboard.",
            _ => null
        };
    }
}
