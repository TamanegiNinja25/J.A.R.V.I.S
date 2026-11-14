using System.Globalization;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Jarvis;

public partial class MainWindow : Window
{
    private const int HistoryTurns = 10;
    private static readonly TimeSpan FollowUpWindow = TimeSpan.FromSeconds(10);
    private static readonly Brush MicOffBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x50, 0x5F));

    private readonly AiService _ai = new();
    private readonly MemoryStore _memory = new();
    private readonly SystemTelemetry _telemetry = new();
    private readonly ToolRegistry _tools;
    private readonly SpeechSynthesizer _speaker = new();
    private readonly MicrophoneListener _mic = new();
    private readonly SpeechToText _stt = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private WakeWordDetector? _wake;
    private bool _busy, _pushToTalk, _wakeWordOn;
    private DateTime _followUpUntil; // after a bare "Hey JARVIS", the next utterance is the command

    public MainWindow()
    {
        InitializeComponent();
        _tools = new ToolRegistry(BuiltInTools.Create(_telemetry), BuiltInTools.Shortcuts, ApproveAsync, _memory);
        _mic.Utterance += pcm => _ = HandleUtteranceAsync(pcm);
        _mic.Level += level => Dispatcher.BeginInvoke(() => MicLevel.ScaleX = _mic.IsRunning ? level : 0, DispatcherPriority.Background);
        _speaker.SpeakStarted += (_, _) => _mic.Suppressed = true;
        _speaker.SpeakCompleted += (_, _) => _mic.Suppressed = false;

        Loaded += async (_, _) =>
        {
            RingRotation.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(18)) { RepeatBehavior = RepeatBehavior.Forever });
            _timer.Tick += (_, _) => UpdateTelemetry();
            _timer.Start();
            UpdateTelemetry();
            // Prefers an installed British male voice; otherwise Windows keeps the default voice.
            _speaker.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult, 0, CultureInfo.GetCultureInfo("en-GB"));
            _speaker.Rate = -1;
            try { _wake = new WakeWordDetector(); }
            catch (InvalidOperationException) { /* reported when the user turns the wake word on */ }
            await _memory.InitializeAsync();
            ShowStatus();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _mic.Dispose();
            _wake?.Dispose();
            _speaker.Dispose();
        };
    }

    private void ShowStatus()
    {
        var error = _ai.ConfigError;
        ConnectionStatus.Text = error is null ? "AI CONFIGURED" : "LOCAL COMMANDS ONLY";
        StatusLed.Fill = error is null ? Brushes.LimeGreen : Brushes.Goldenrod;
        AiModeValue.Text = _ai.Mode;
        AddMessage("JARVIS", error is null
            ? "Good day. Systems are online."
            : $"Good day. Local commands are available, but AI is offline: {error} See README, section 3.");
        if (error is null && _ai.Problems.Count > 0)
            AddMessage("SYSTEM", "Skipped AI providers: " + string.Join(" ", _ai.Problems));
    }

    private void UpdateTelemetry()
    {
        CpuValue.Text = _telemetry.CpuPercent() is { } cpu ? $"{cpu:0}%" : "Collecting...";
        RamValue.Text = SystemTelemetry.Memory() is var (used, total) ? $"{used:0.0} / {total:0.0} GB" : "Unavailable";
        DiskValue.Text = $"{SystemTelemetry.SystemDriveFreeGb():0.0} GB free";
    }

    // ---- Conversation ----

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendPromptAsync();

    private async void PromptBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await SendPromptAsync();
    }

    private async Task SendPromptAsync(string? spoken = null)
    {
        var prompt = (spoken ?? PromptBox.Text).Trim();
        if (_busy || prompt.Length == 0) return;
        if (spoken is null) PromptBox.Clear();
        AddMessage("YOU", prompt);
        SetBusy(true);
        try
        {
            // Read history before saving the prompt so the prompt is not sent twice.
            var history = await _memory.GetRecentAsync(HistoryTurns);
            await _memory.SaveAsync("user", prompt);
            var reply = await _tools.TryRunShortcutAsync(prompt) ?? await AskAiAsync(prompt, history);
            await _memory.SaveAsync("assistant", reply);
            AddMessage("JARVIS", reply);
            Speak(reply);
        }
        catch (Exception ex)
        {
            AddMessage("SYSTEM", $"Error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<string> AskAiAsync(string prompt, IReadOnlyList<ChatLine> history)
    {
        var reply = await _ai.AskAsync(prompt, history, _tools);
        AiModeValue.Text = $"{_ai.Mode}\nLast answer: {reply.Provider}";
        return reply.Text;
    }

    /// <summary>Approval prompt for Confirm tools. Defaults to No.</summary>
    private Task<bool> ApproveAsync(Tool tool, string arguments) => Dispatcher.InvokeAsync(() =>
    {
        AddMessage("SYSTEM", $"Approval needed: {tool.Name} {arguments}");
        Speak("I need your approval for that.");
        var answer = MessageBox.Show(this,
            $"JARVIS wants to run:\n\n{tool.Name}  {arguments}\n\n{tool.Description}\n\nAllow it?",
            "J.A.R.V.I.S. — approval required", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        return answer == MessageBoxResult.Yes;
    }).Task;

    private void AddMessage(string speaker, string message)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = speaker,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource(speaker == "YOU" ? "MutedBrush" : "AccentBrush")
        });
        panel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Conversation.Children.Add(panel);
        ConversationScroll.ScrollToEnd();
    }

    private void Speak(string text)
    {
        _speaker.SpeakAsyncCancelAll();
        _speaker.SpeakAsync(text);
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SendButton.IsEnabled = !busy;
        RefreshState();
    }

    private void RefreshState() => CoreState.Text =
        _busy ? "PROCESSING"
        : _pushToTalk ? "LISTENING"
        : _wakeWordOn ? "SAY \"HEY JARVIS\""
        : "STANDBY";

    // ---- Voice ----

    private void ListenButton_Click(object sender, RoutedEventArgs e) => SetVoice(!_pushToTalk, _wakeWordOn);

    private void WakeButton_Click(object sender, RoutedEventArgs e) => SetVoice(_pushToTalk, !_wakeWordOn);

    private void SetVoice(bool pushToTalk, bool wakeWord)
    {
        if ((pushToTalk || wakeWord) && !_stt.IsConfigured)
        {
            AddMessage("SYSTEM", "Voice needs GROQ_API_KEY for speech recognition (README, section 3).");
            pushToTalk = wakeWord = false;
        }
        if (wakeWord && _wake is null)
        {
            AddMessage("SYSTEM", "The wake word needs the English Windows speech recognizer: Settings → Time & language → Speech.");
            wakeWord = false;
        }
        try
        {
            if (pushToTalk || wakeWord) _mic.Start();
            else _mic.Stop();
        }
        catch (Exception ex)
        {
            AddMessage("SYSTEM", $"Microphone unavailable: {ex.Message} Check Settings → Privacy & security → Microphone.");
            pushToTalk = wakeWord = false;
        }
        (_pushToTalk, _wakeWordOn) = (pushToTalk, wakeWord);
        ListenButton.Content = pushToTalk ? "■ STOP" : "◉ LISTEN";
        WakeButton.Content = wakeWord ? "HEY JARVIS: ON" : "HEY JARVIS: OFF";
        MicLed.Fill = _mic.IsRunning ? Brushes.Red : MicOffBrush;
        RefreshState();
    }

    /// <summary>Runs on a background thread for each utterance the microphone captures.</summary>
    private async Task HandleUtteranceAsync(byte[] pcm)
    {
        try
        {
            var pushToTalk = _pushToTalk;
            var followUp = DateTime.UtcNow < _followUpUntil;
            // Wake word mode: the offline check runs first, so audio leaves the PC only after "Hey JARVIS".
            if (!pushToTalk && !followUp && _wake?.Detect(pcm) != true) return;

            if (pushToTalk) await Dispatcher.InvokeAsync(() => SetVoice(false, _wakeWordOn));
            await Dispatcher.InvokeAsync(() => CoreState.Text = "TRANSCRIBING");
            var text = await _stt.TranscribeAsync(pcm);
            if (!pushToTalk && !followUp)
            {
                var command = WakeWordDetector.StripWakePhrase(text);
                if (command is null) // the offline check was a false alarm
                {
                    await Dispatcher.InvokeAsync(RefreshState);
                    return;
                }
                if (command.Length == 0) // bare "Hey JARVIS": answer, then treat the next utterance as the command
                {
                    _followUpUntil = DateTime.UtcNow + FollowUpWindow;
                    await Dispatcher.InvokeAsync(() => { RefreshState(); Speak("Yes?"); });
                    return;
                }
                text = command;
            }
            _followUpUntil = default;
            await Dispatcher.InvokeAsync(() =>
            {
                RefreshState();
                if (text.Length > 0) return SendPromptAsync(text);
                if (pushToTalk) Speak("I didn't catch that.");
                return Task.CompletedTask;
            }).Task.Unwrap();
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                AddMessage("SYSTEM", $"Voice error: {ex.Message}");
                RefreshState();
            });
        }
    }
}
