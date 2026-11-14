using NAudio.Wave;

namespace Jarvis;

// Captures 16 kHz mono audio and cuts it into utterances with an energy-based voice activity detector.
public sealed class MicrophoneListener : IDisposable
{
    public const int SampleRate = 16000;
    private const int FrameMs = 30;
    private const int PreRollMs = 300;       // keep audio from just before speech starts
    private const int EndSilenceMs = 800;    // this much silence ends an utterance
    private const int MinSpeechMs = 250;     // shorter bursts are clicks or bumps
    private const int MaxUtteranceMs = 15000;
    private const double MinSpeechRms = 250; // raise the Windows mic level if speech never registers

    private readonly WaveIn _wave = new() { WaveFormat = new WaveFormat(SampleRate, 16, 1), BufferMilliseconds = FrameMs };
    private readonly Queue<byte[]> _preRoll = new();
    private readonly MemoryStream _utterance = new();
    private double _noiseFloor = 150;        // RMS of room noise, adapts while nobody speaks
    private int _speechMs, _silenceMs;
    private bool _inSpeech;

    /// <summary>A finished utterance as raw 16-bit PCM. Raised on a background thread.</summary>
    public event Action<byte[]>? Utterance;

    /// <summary>Input level from 0 to 1 for the HUD meter. Raised on a background thread.</summary>
    public event Action<double>? Level;

    public bool IsRunning { get; private set; }

    /// <summary>While true (JARVIS is speaking) audio is discarded so JARVIS does not hear itself.</summary>
    public bool Suppressed { get; set; }

    public MicrophoneListener() => _wave.DataAvailable += OnData;

    public void Start()
    {
        if (IsRunning) return;
        _wave.StartRecording();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _wave.StopRecording();
        IsRunning = false;
        Reset();
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var frame = e.Buffer.AsSpan(0, e.BytesRecorded).ToArray();
        var rms = Rms(frame);
        Level?.Invoke(Math.Min(1, rms / 3000));
        if (Suppressed)
        {
            Reset();
            return;
        }

        var ms = frame.Length / 2 * 1000 / SampleRate;
        var voiced = rms > Math.Max(_noiseFloor * 3, MinSpeechRms);
        if (!_inSpeech)
        {
            _preRoll.Enqueue(frame);
            if (_preRoll.Count > PreRollMs / FrameMs) _preRoll.Dequeue();
            if (!voiced)
            {
                _noiseFloor = 0.95 * _noiseFloor + 0.05 * rms;
                return;
            }
            _inSpeech = true;
            _speechMs = ms;
            _silenceMs = 0;
            foreach (var buffered in _preRoll) _utterance.Write(buffered);
            _preRoll.Clear();
            return;
        }

        _utterance.Write(frame);
        if (voiced) (_speechMs, _silenceMs) = (_speechMs + ms, 0);
        else _silenceMs += ms;

        var totalMs = _utterance.Length / 2 * 1000 / SampleRate;
        if (_silenceMs < EndSilenceMs && totalMs < MaxUtteranceMs) return;
        var pcm = _speechMs >= MinSpeechMs ? _utterance.ToArray() : null;
        Reset();
        if (pcm is not null) Utterance?.Invoke(pcm);
    }

    private void Reset()
    {
        _inSpeech = false;
        _utterance.SetLength(0);
        _preRoll.Clear();
    }

    private static double Rms(byte[] pcm)
    {
        double sum = 0;
        for (var i = 0; i + 1 < pcm.Length; i += 2)
        {
            double sample = BitConverter.ToInt16(pcm, i);
            sum += sample * sample;
        }
        return pcm.Length < 2 ? 0 : Math.Sqrt(sum / (pcm.Length / 2));
    }

    public void Dispose()
    {
        Stop();
        _wave.Dispose();
    }
}
