using System.Net.Http.Headers;
using System.Text.Json;

namespace Jarvis;

// Groq Whisper speech-to-text (free plan: 20 requests/min, 2,000/day).
public sealed class SpeechToText
{
    private const string Url = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const double MaxNoSpeechProbability = 0.5;

    private readonly string _key = Shared.Env("GROQ_API_KEY");
    private readonly string _model = Shared.Env("GROQ_STT_MODEL", "whisper-large-v3-turbo");
    private readonly string _language = Shared.Env("JARVIS_LANGUAGE", "en");

    public bool IsConfigured => _key != "";

    public async Task<string> TranscribeAsync(byte[] pcm)
    {
        var audio = new ByteArrayContent(Wav(pcm));
        audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        using var form = new MultipartFormDataContent
        {
            { audio, "file", "speech.wav" },
            { new StringContent(_model), "model" },
            { new StringContent(_language), "language" },
            { new StringContent("verbose_json"), "response_format" },
            { new StringContent("0"), "temperature" },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);

        using var response = await Shared.Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Speech recognition HTTP {(int)response.StatusCode}: {Shared.Shorten(json)}");

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("segments", out var segments))
            return doc.RootElement.GetProperty("text").GetString()?.Trim() ?? "";
        // Whisper invents text for noise ("Thank you."); keep only segments it rates as real speech.
        var spoken = segments.EnumerateArray()
            .Where(s => !s.TryGetProperty("no_speech_prob", out var p) || p.GetDouble() < MaxNoSpeechProbability)
            .Select(s => s.GetProperty("text").GetString()?.Trim());
        return string.Join(" ", spoken).Trim();
    }

    /// <summary>Wraps 16-bit mono PCM in a WAV header.</summary>
    public static byte[] Wav(byte[] pcm)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8); w.Write(36 + pcm.Length); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1);
        w.Write(MicrophoneListener.SampleRate); w.Write(MicrophoneListener.SampleRate * 2);
        w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(pcm.Length); w.Write(pcm);
        w.Flush();
        return stream.ToArray();
    }
}
