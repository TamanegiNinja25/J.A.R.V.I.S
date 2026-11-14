using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Jarvis;

// Every provider speaks the OpenAI Chat Completions format. Providers are tried in order
// until one answers, so a free provider hitting its daily limit falls through to the next.
// Configuration is environment variables only (README, section 3).
public sealed class AiService
{
    private const string SystemPrompt = """
        You are JARVIS, a precise, courteous and calm personal AI assistant running on Windows.
        Answer concisely with British-inspired phrasing. Never claim to have performed an action
        unless a verified tool result confirms it. Ask before sensitive, destructive, financial,
        account or administrator-level operations.
        """;

    // Default model "" means the user must set <NAME>_MODEL, because those IDs change often.
    private static readonly Provider[] Known =
    [
        new("groq", "https://api.groq.com/openai/v1/chat/completions", "GROQ_API_KEY", "openai/gpt-oss-120b"),
        new("gemini", "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "GEMINI_API_KEY", ""),
        new("openrouter", "https://openrouter.ai/api/v1/chat/completions", "OPENROUTER_API_KEY", ""),
        new("openai", "https://api.openai.com/v1/chat/completions", "OPENAI_API_KEY", ""),
        new("ollama", "http://localhost:11434/v1/chat/completions", null, "qwen2.5:1.5b"),
    ];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web); // camelCase

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly List<Endpoint> _chain = [];
    private readonly List<string> _problems = [];

    public AiService()
    {
        // JARVIS_PROVIDER, e.g. "groq,gemini". Unset: every provider with a key, in table order; else Ollama.
        var requested = Env("JARVIS_PROVIDER");
        var names = requested != ""
            ? requested.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : Known.Where(p => p.KeyVariable is { } key && Env(key) != "").Select(p => p.Name).DefaultIfEmpty("ollama");

        foreach (var name in names)
        {
            var provider = Known.FirstOrDefault(p => p.Name == name);
            if (provider is null)
            {
                _problems.Add($"Unknown provider '{name}'.");
                continue;
            }
            var key = provider.KeyVariable is { } keyVariable ? Env(keyVariable) : "";
            var model = Env($"{name.ToUpperInvariant()}_MODEL") is { Length: > 0 } m ? m : provider.DefaultModel;
            var url = name == "ollama" && Env("OLLAMA_URL") is { Length: > 0 } custom ? custom : provider.Url;

            if (provider.KeyVariable is not null && key == "") _problems.Add($"{provider.KeyVariable} is not set.");
            else if (model == "") _problems.Add($"{name.ToUpperInvariant()}_MODEL is not set.");
            else _chain.Add(new Endpoint(name, url, key, model));
        }
    }

    /// <summary>Null when at least one provider is usable; otherwise why none is.</summary>
    public string? ConfigError => _chain.Count > 0 ? null : string.Join(" ", _problems);

    /// <summary>Requested providers that were skipped because of missing settings.</summary>
    public IReadOnlyList<string> Problems => _problems;

    public string Mode => _chain.Count == 0
        ? "NONE"
        : string.Join(" → ", _chain.Select(e => $"{e.Name.ToUpperInvariant()} ({e.Model})"));

    public async Task<AiReply> AskAsync(string prompt, IReadOnlyList<ChatLine> history)
    {
        if (ConfigError is { } error) throw new InvalidOperationException(error);
        var messages = history.Prepend(new ChatLine("system", SystemPrompt)).Append(new ChatLine("user", prompt)).ToList();
        var failures = new List<string>();
        foreach (var endpoint in _chain)
        {
            try
            {
                return new AiReply(await CompleteAsync(endpoint, messages), endpoint.Name);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                          or InvalidOperationException or JsonException or KeyNotFoundException)
            {
                failures.Add($"{endpoint.Name}: {ex.Message}");
            }
        }
        throw new InvalidOperationException("No AI provider answered. " + string.Join(" | ", failures));
    }

    private async Task<string> CompleteAsync(Endpoint endpoint, List<ChatLine> messages)
    {
        var body = JsonSerializer.Serialize(new { model = endpoint.Model, messages }, Json);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (endpoint.Key != "") request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Key);

        using var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {json[..Math.Min(json.Length, 300)]}");

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(text) ? "The model returned no text." : text.Trim();
    }

    private static string Env(string name) => Environment.GetEnvironmentVariable(name)?.Trim() ?? "";

    private sealed record Provider(string Name, string Url, string? KeyVariable, string DefaultModel);

    private sealed record Endpoint(string Name, string Url, string Key, string Model);
}

public sealed record ChatLine(string Role, string Content);

public sealed record AiReply(string Text, string Provider);
