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
| AI intelligence | OpenAI-compatible API + optional Ollama | Cloud first | ✅ `AiService.cs` (no tool calling yet) |
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
4. Otherwise `AiService.AskAsync` calls the configured provider:
   - OpenAI: `POST /v1/responses` with `instructions` + `input`.
   - Ollama: `POST /api/chat`.
5. The reply is saved, displayed and spoken. Speech heard while JARVIS is speaking is ignored, to prevent feedback loops.

## Configuration

Environment variables only: `JARVIS_PROVIDER` (`ollama` default | `openai`), `JARVIS_MODEL` (default `qwen2.5:1.5b` for Ollama; required for OpenAI), `OPENAI_API_KEY`, `JARVIS_ENDPOINT` (Ollama URL override).
