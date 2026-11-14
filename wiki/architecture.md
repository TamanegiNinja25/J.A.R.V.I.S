# Architecture

Subsystems, technology choices, hardware constraints and how a request flows through the code. Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`. Feature-level status is in [capabilities.md](capabilities.md).

## Selected configuration

Capability **complete** · Intelligence **hybrid** · Interface **holographic HUD** · Voice **cinematic British-inspired male** (original voice, not a copy of the film performance).

## Target hardware

Ryzen 9 5900X · 8 GB RAM · 500 GB storage · integrated AMD graphics.

Consequences:
- AI reasoning and speech recognition are **cloud-first** on free tiers; local models are optional and small (about 1.5B parameters).
- Always-on components must be cheap: the voice activity detector is a plain energy threshold, and the wake-word check runs only on detected speech.
- The HUD uses lightweight WPF vector graphics.
- Upgrading to 16 GB RAM (32 GB ideal) is advised before heavy local inference.

## Subsystems

| Subsystem | Technology | Runs | Status | Code |
|---|---|---|---|---|
| Holographic HUD | WPF / XAML | Local | ✅ | `MainWindow.xaml` |
| Voice: capture + VAD | NAudio `WaveIn`, energy VAD with adaptive noise floor | Local | ✅ | `MicrophoneListener.cs` |
| Voice: wake word | Windows `System.Speech` English recognizer, grammar "hey/okay jarvis" + wildcard | Local | ✅ | `WakeWordDetector.cs` |
| Voice: speech-to-text | Groq `whisper-large-v3-turbo` | Cloud (free) | ✅ | `SpeechToText.cs` |
| Voice: text-to-speech | Windows `System.Speech` (en-GB male if installed) | Local | ⚠️ Piper/cloud TTS planned | `MainWindow.xaml.cs` |
| AI intelligence | OpenAI-compatible chain: Groq 120b → Groq 20b → Gemini → OpenRouter → OpenAI, or Ollama | Cloud first | ✅ | `AiService.cs` |
| Orchestrator / tools | Chat Completions tool calling, typed tool registry, approval prompt, audit log | Local | ✅ core | `AiService.cs`, `ToolRegistry.cs`, `BuiltInTools.cs` |
| Persistent memory | SQLite | Local | ⚠️ History + audit only | `MemoryStore.cs` |
| System monitor | Win32 `GetSystemTimes`, `GlobalMemoryStatusEx` | Local | ⚠️ CPU, RAM, disk | `SystemTelemetry.cs` |
| Computer vision | Windows Graphics Capture, OCR | Hybrid | ❌ Phase 5 | |
| Connectivity | Tavily / Wikipedia search; calendar, smart home later | Cloud | ⚠️ Search only | `BuiltInTools.cs` |

## Request flow

1. **Input.** Typed text, or a voice utterance (see the voice flow below), reaches `MainWindow.SendPromptAsync`.
2. **History.** The last 10 messages are read from SQLite *before* the prompt is saved, so the prompt is not duplicated.
3. **Shortcut.** `ToolRegistry.TryRunShortcutAsync` runs exact phrases ("open notepad", "mute") straight through the tool registry, with no AI and no network.
4. **Agent loop.** Otherwise `AiService.AskAsync` sends the conversation plus tool definitions to the provider chain. If the reply has `tool_calls`, each call goes through `ToolRegistry.RunAsync` (permission → run → audit), the results are appended as `role: tool` messages, and the loop repeats, up to 8 steps, until the model answers in text.
5. **Fallback.** Each completion tries the endpoints in order; HTTP errors (e.g. 429 daily limit), timeouts and malformed replies fall through to the next one.
6. **Output.** The reply is saved, displayed and spoken. AI MODE shows which provider and model answered.

## Voice flow

1. `MicrophoneListener` captures 16 kHz mono audio in 30 ms frames. A frame is speech when its RMS is above 3× the adaptive noise floor (minimum 250). An utterance keeps 300 ms of audio before speech starts and ends after 800 ms of silence (bursts under 250 ms are ignored; maximum length 15 s).
2. **Push-to-talk (◉ LISTEN):** the next utterance goes straight to Whisper, then the mic closes.
3. **Wake word (HEY JARVIS: ON):** `WakeWordDetector.Detect` checks the utterance offline. If it passes, Whisper transcribes it, and the transcript must also contain the name (`StripWakePhrase`; accepts Jarvis/Jervis/Javis). The rest of the sentence is the command. A bare "Hey JARVIS" gets "Yes?", and the next utterance within 10 s is the command.
4. Whisper segments with `no_speech_prob ≥ 0.5` are dropped (Whisper invents text such as "Thank you." for noise).
5. The mic is suppressed while JARVIS speaks.

Verified 2026-11-14 with synthesized speech: both "Hey Jarvis …" phrases were detected and transcribed exactly; "What is the weather like in London today?" and "Open the garage door please." were rejected offline and never sent.

## AI providers

| Provider | Endpoint | Default model | Free limits (checked 2026-11-14) |
|---|---|---|---|
| Groq | `api.groq.com/openai/v1` | `openai/gpt-oss-120b,openai/gpt-oss-20b` | Per model: 30 RPM, 1K RPD, 8K TPM, 200K TPD. Whisper: 20 RPM, 2K RPD. No data retention by default. |
| Gemini | `generativelanguage.googleapis.com/v1beta/openai` | must be set | Shown only in AI Studio; free-tier input used for product improvement and human review |
| OpenRouter | `openrouter.ai/api/v1` | must be set | `:free` models: 20 RPM, 50 RPD (1K RPD after a $10 top-up) |
| OpenAI | `api.openai.com/v1` | must be set | Paid only |
| Ollama | `localhost:11434/v1` | `qwen2.5:1.5b` | Local |

Only Groq has default models, because those IDs are confirmed on Groq's rate-limit page. The others change often, so the user must set them.

**Rejected:** Groq's built-in `browser_search`. It worked on the free plan, but one query used about 372K tokens (more than a model's 200K daily budget) and returned an outdated answer. Web search uses Tavily (1,000 free searches/month, no card) or Wikipedia's REST API (no key) instead.

## Configuration

Environment variables only:
- `<PROVIDER>_API_KEY` and `<PROVIDER>_MODEL` (comma-separated list = one fallback per model)
- `JARVIS_PROVIDER` (comma-separated order; default = every provider with a key, in the table order, else Ollama)
- `OLLAMA_URL`
- `GROQ_STT_MODEL` (default `whisper-large-v3-turbo`)
- `JARVIS_LANGUAGE` (Whisper language, default `en`)
- `TAVILY_API_KEY`
