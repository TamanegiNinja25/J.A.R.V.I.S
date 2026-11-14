using System.Globalization;
using System.Speech.Recognition;
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
    private const float MinSpeechConfidence = 0.65f;

    private readonly AiService _ai = new();
    private readonly MemoryStore _memory = new();
    private readonly SystemTelemetry _telemetry = new();
    private readonly SpeechSynthesizer _speaker = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private SpeechRecognitionEngine? _recognizer;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
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
            await _memory.InitializeAsync();
            ShowStatus();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _recognizer?.Dispose();
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
    }

    private void UpdateTelemetry()
    {
        CpuValue.Text = _telemetry.CpuPercent() is { } cpu ? $"{cpu:0}%" : "Collecting...";
        RamValue.Text = SystemTelemetry.Memory() is var (used, total) ? $"{used:0.0} / {total:0.0} GB" : "Unavailable";
        DiskValue.Text = $"{SystemTelemetry.SystemDriveFreeGb():0.0} GB free";
    }

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
            var reply = WindowsCommandRouter.TryExecute(prompt) ?? await _ai.AskAsync(prompt, history);
            await _memory.SaveAsync("assistant", reply);
            AddMessage("JARVIS", reply);
            _speaker.SpeakAsyncCancelAll();
            _speaker.SpeakAsync(reply);
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

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SendButton.IsEnabled = !busy;
        CoreState.Text = busy ? "PROCESSING" : _recognizer is null ? "STANDBY" : "LISTENING";
    }

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

    private void ListenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recognizer is not null)
        {
            StopListening();
            return;
        }
        try
        {
            _recognizer = new SpeechRecognitionEngine(); // Windows default speech language
            _recognizer.LoadGrammar(new DictationGrammar());
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += (_, args) =>
            {
                // Ignore what the microphone hears while JARVIS itself is speaking.
                if (args.Result.Confidence >= MinSpeechConfidence && _speaker.State != SynthesizerState.Speaking)
                    Dispatcher.InvokeAsync(() => SendPromptAsync(args.Result.Text));
            };
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            ListenButton.Content = "■ STOP";
            CoreState.Text = "LISTENING";
        }
        catch (Exception ex)
        {
            StopListening();
            AddMessage("SYSTEM", $"Microphone unavailable: {ex.Message} Check the Windows microphone privacy setting and installed speech languages.");
        }
    }

    private void StopListening()
    {
        _recognizer?.RecognizeAsyncCancel();
        _recognizer?.Dispose();
        _recognizer = null;
        ListenButton.Content = "◉ LISTEN";
        CoreState.Text = "STANDBY";
    }
}
