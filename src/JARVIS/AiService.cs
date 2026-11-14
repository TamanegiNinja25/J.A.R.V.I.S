using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Jarvis;

// Provider and model come from environment variables (README, section 3).
public sealed class AiService
{
    private const string SystemPrompt = """
        You are JARVIS, a precise, courteous and calm personal AI assistant running on Windows.
        Answer concisely with British-inspired phrasing. Never claim to have performed an action
        unless a verified tool result confirms it. Ask before sensitive, destructive, financial,
        account or administrator-level operations.
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web); // camelCase

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(90) };
    private readonly string _provider = Env("JARVIS_PROVIDER", "ollama").ToLowerInvariant();
    private readonly string _apiKey = Env("OPENAI_API_KEY", "");
    private readonly string _ollamaUrl = Env("JARVIS_ENDPOINT", "http://localhost:11434/api/chat");
    private readonly string _model;

    public AiService() => _model = Env("JARVIS_MODEL", _provider == "ollama" ? "qwen2.5:1.5b" : "");

    /// <summary>Null when the provider has everything it needs; otherwise what is missing.</summary>
    public string? ConfigError => _provider switch
    {
        "ollama" => null,
        "openai" when _apiKey == "" => "OPENAI_API_KEY is not set.",
        "openai" when _model == "" => "JARVIS_MODEL is not set.",
        "openai" => null,
        _ => $"JARVIS_PROVIDER '{_provider}' is not 'ollama' or 'openai'."
    };

    public string Mode => $"{_provider.ToUpperInvariant()} / {(_model == "" ? "no model" : _model)}";

    public Task<string> AskAsync(string prompt, IReadOnlyList<ChatLine> history)
    {
        if (ConfigError is { } error) throw new InvalidOperationException(error);
        var messages = history.Append(new ChatLine("user", prompt));
        return _provider == "openai" ? AskOpenAiAsync(messages) : AskOllamaAsync(messages);
    }

    // OpenAI Responses API: POST /v1/responses, reply text is in output[type=message].content[type=output_text].
    private async Task<string> AskOpenAiAsync(IEnumerable<ChatLine> messages)
    {
        using var request = Post("https://api.openai.com/v1/responses",
            new { model = _model, instructions = SystemPrompt, input = messages });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        using var doc = await SendAsync(request);

        var text = new StringBuilder();
        foreach (var item in doc.RootElement.GetProperty("output").EnumerateArray())
            if (item.GetProperty("type").GetString() == "message")
                foreach (var part in item.GetProperty("content").EnumerateArray())
                    if (part.GetProperty("type").GetString() == "output_text")
                        text.Append(part.GetProperty("text").GetString());
        return text.Length > 0 ? text.ToString() : "The model returned no text.";
    }

    // Ollama chat API: POST /api/chat, reply text is in message.content.
    private async Task<string> AskOllamaAsync(IEnumerable<ChatLine> messages)
    {
        using var request = Post(_ollamaUrl, new
        {
            model = _model,
            messages = messages.Prepend(new ChatLine("system", SystemPrompt)),
            stream = false,
            options = new { num_ctx = 2048 } // small context keeps RAM use low on 8 GB
        });
        using var doc = await SendAsync(request);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static HttpRequestMessage Post(string url, object body) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json")
    };

    private async Task<JsonDocument> SendAsync(HttpRequestMessage request)
    {
        using var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{_provider} returned {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body);
    }

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}

public sealed record ChatLine(string Role, string Content);
