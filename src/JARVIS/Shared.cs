namespace Jarvis;

internal static class Shared
{
    public static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    /// <summary>Reads an environment variable; settings live there, never in source (README, section 3).</summary>
    public static string Env(string name, string fallback = "") =>
        Environment.GetEnvironmentVariable(name)?.Trim() is { Length: > 0 } value ? value : fallback;

    public static string Shorten(string text, int max = 300) => text.Length <= max ? text : text[..max] + "…";
}
