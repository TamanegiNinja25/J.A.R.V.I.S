using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text.RegularExpressions;

namespace Jarvis;

// Offline "Hey JARVIS" check that runs on each utterance with the installed Windows English recognizer.
// Only utterances that pass are sent to the cloud, and the cloud transcript must contain the name too.
public sealed partial class WakeWordDetector : IDisposable
{
    private const float MinConfidence = 0.5f;

    private readonly SpeechRecognitionEngine _engine;
    private readonly Lock _lock = new();

    public WakeWordDetector()
    {
        var recognizer = SpeechRecognitionEngine.InstalledRecognizers()
            .FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName == "en")
            ?? throw new InvalidOperationException("No English Windows speech recognizer is installed.");
        _engine = new SpeechRecognitionEngine(recognizer);

        var name = new Choices("hey jarvis", "okay jarvis", "jarvis");
        var nameThenCommand = new GrammarBuilder(name);
        nameThenCommand.AppendWildcard();
        _engine.LoadGrammar(new Grammar(new Choices(new GrammarBuilder(name), nameThenCommand)));
    }

    /// <summary>True if the utterance (16-bit mono PCM) starts with the wake phrase.</summary>
    public bool Detect(byte[] pcm)
    {
        lock (_lock)
        {
            using var stream = new MemoryStream(pcm);
            _engine.SetInputToAudioStream(stream,
                new SpeechAudioFormatInfo(MicrophoneListener.SampleRate, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
            var result = _engine.Recognize();
            _engine.SetInputToNull();
            return result?.Words.Any(w => w.Text.Equals("jarvis", StringComparison.OrdinalIgnoreCase)
                                          && w.Confidence >= MinConfidence) == true;
        }
    }

    /// <summary>Removes the wake phrase from a transcript; null if the transcript does not contain it.</summary>
    public static string? StripWakePhrase(string transcript)
    {
        var match = WakePhrase().Match(transcript);
        return match.Success ? transcript.Remove(match.Index, match.Length).Trim(' ', ',', '.', '!', '?') : null;
    }

    // Whisper spells the name several ways: Jarvis, Jervis, Javis.
    [GeneratedRegex(@"\b(?:(?:hey|hi|okay|ok)[\s,]+)?j[ae]r?v[ie]s\b[\s,.!?]*", RegexOptions.IgnoreCase)]
    private static partial Regex WakePhrase();

    public void Dispose() => _engine.Dispose();
}
