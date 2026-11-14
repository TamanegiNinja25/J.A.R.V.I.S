# Architecture

Subsystems, technology choices, hardware constraints and current implementation status. Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`.

## Selected configuration

Capability **complete** · Intelligence **hybrid** · Interface **holographic HUD** · Voice **cinematic British-inspired male** (original voice, not a copy of the film performance).

## Target hardware

Ryzen 9 5900X · 8 GB RAM · 500 GB storage · integrated AMD graphics.

Consequences:
- AI reasoning is **cloud-first**; local models are optional and small (about 1.5B parameters, `num_ctx` 2048).
- Only one local inference model at a time.
- The HUD uses lightweight WPF vector graphics.
- Upgrading to 16 GB RAM (32 GB ideal) is advised before heavy local inference.

## Subsystems

All subsystems are coordinated by the **Orchestrator**: intent detection → tool selection → permission check → execution → verification → report.

| Subsystem | Planned technology | Runs | Phase 1 status |
|---|---|---|---|
| Holographic HUD | WPF / XAML vector animation | Local | ✅ `MainWindow.xaml` |
| Voice: wake word | openWakeWord | Local | ❌ Phase 2 |
| Voice: speech-to-text | whisper.cpp | Local or cloud | ⚠️ Windows `System.Speech` dictation |
| Voice: text-to-speech | Piper or cloud neural TTS | Local or cloud | ⚠️ Windows `System.Speech` |
| AI intelligence | OpenAI-compatible API chain: Groq (free) → Gemini → OpenRouter → OpenAI, or Ollama | Cloud first | ✅ `AiService.cs` (no tool calling yet) |
| Persistent memory | SQLite + optional vector index | Local | ⚠️ Conversation history only (`MemoryStore.cs`) |
| Computer vision | Windows Graphics Capture, Windows OCR, vision models | Hybrid | ❌ Phase 5 |
| System monitor | Win32 APIs, LibreHardwareMonitor | Local | ⚠️ CPU, RAM, disk (`SystemTelemetry.cs`); no GPU/temp/network |
| Automation | Windows UI Automation, typed tool registry | Local | ⚠️ Allowlisted app launch (`WindowsCommandRouter.cs`) |
| Connectivity | HTTP / WebSocket, browser, calendar, smart home | Cloud | ❌ Phase 7 |
| Installer | MSIX | — | ❌ Phase 8 |

## Phase 1 request flow

1. User types or speaks a prompt (`MainWindow.SendPromptAsync`).
2. The last 10 messages are read from SQLite *before* the prompt is saved, so the prompt is not duplicated.
3. `WindowsCommandRouter.TryExecute` handles exact allowlisted phrases locally.
4. Otherwise `AiService.AskAsync` sends `POST …/chat/completions` (OpenAI format) to each provider in the chain until one succeeds. HTTP errors (e.g. 429 daily limit), timeouts, network errors and malformed replies fall through to the next provider. If all fail, every provider's error is shown.
5. The reply is saved, displayed and spoken, and AI MODE shows which provider answered. Speech heard while JARVIS is speaking is ignored, to prevent feedback loops.

## AI providers

| Provider | Endpoint | Default model | Free limits (checked 2026-11-14) |
|---|---|---|---|
| Groq | `api.groq.com/openai/v1` | `openai/gpt-oss-120b` | 30 RPM, 1K RPD, 8K TPM, 200K TPD; no data retention by default |
| Gemini | `generativelanguage.googleapis.com/v1beta/openai` | must be set | Shown only in AI Studio; free-tier input used for product improvement and human review |
| OpenRouter | `openrouter.ai/api/v1` | must be set | `:free` models: 20 RPM, 50 RPD (1K RPD after a $10 top-up) |
| OpenAI | `api.openai.com/v1` | must be set | Paid only |
| Ollama | `localhost:11434/v1` | `qwen2.5:1.5b` | Local |

Only Groq has a default model, because its ID is confirmed on Groq's rate-limit page. The others change often, so the user must set them. Groq also offers free `whisper-large-v3` speech-to-text (20 RPM, 2K RPD), a candidate for Phase 2.

## Configuration

Environment variables only: `<PROVIDER>_API_KEY`, `<PROVIDER>_MODEL`, `JARVIS_PROVIDER` (comma-separated order; default = every provider with a key, in the table order, else Ollama), `OLLAMA_URL`.
