# J.A.R.V.I.S.

Just A Rather Very Intelligent System: a Windows desktop AI assistant with a holographic HUD, chat, voice input/output, SQLite memory, live system telemetry and an allowlisted command router. AI runs on a local Ollama model or the OpenAI API.

This is **Phase 1** of the [roadmap](wiki/roadmap.md). Project knowledge lives in [`wiki/`](wiki/index.md).

## 1. Install the tools (one time)

1. Download and install the **.NET 10 SDK (x64)** from https://dotnet.microsoft.com/download/dotnet/10.0
2. Open a **new** PowerShell window and run:
   ```powershell
   dotnet --list-sdks
   ```
   Check that a line starting with `10.` appears.
3. Optional: install Visual Studio 2026 with the **.NET desktop development** workload to edit the UI visually.

## 2. Build and run

1. In PowerShell, go to the project folder (adjust the path to where you cloned the repo):
   ```powershell
   cd C:\path\to\J.A.R.V.I.S\src\JARVIS
   ```
2. Build:
   ```powershell
   dotnet build -c Release
   ```
   Check that it ends with `Build succeeded`.
3. Run:
   ```powershell
   dotnet run
   ```
   The J.A.R.V.I.S. window opens. Without an AI provider it shows **LOCAL COMMANDS ONLY** (amber light); the commands in section 4 still work.

## 3. Configure the AI provider

Pick **one** option. Environment variables set with `$env:` last only for that PowerShell window; `setx` makes them permanent (open a new window afterwards).

### Option A: OpenAI (recommended for 8 GB RAM)

1. Sign in at https://platform.openai.com, open **API keys**, click **Create new secret key**, and copy it. API usage is billed separately from a ChatGPT subscription.
2. Open https://platform.openai.com/docs/models and copy the ID of a model your account can use.
3. In PowerShell, run each line, replacing the placeholders. Do not share the key or commit it:
   ```powershell
   setx OPENAI_API_KEY "paste-your-key-here"
   setx JARVIS_PROVIDER "openai"
   setx JARVIS_MODEL "paste-model-id-here"
   ```
4. Close PowerShell, open a new window, go to `src\JARVIS`, and run `dotnet run`.
5. Check: the top-right shows **AI CONFIGURED** (green), and asking "Hello" gets a reply.

### Option B: Ollama (local, offline)

1. Install Ollama from https://ollama.com/download/windows.
2. In PowerShell, download a small model (about 1 GB):
   ```powershell
   ollama pull qwen2.5:1.5b
   ```
3. Set the provider. Ollama is the default, so this is only needed if you set OpenAI before:
   ```powershell
   setx JARVIS_PROVIDER "ollama"
   setx JARVIS_MODEL "qwen2.5:1.5b"
   ```
4. Open a new PowerShell window and run `dotnet run` in `src\JARVIS`.
5. Check: asking "Hello" gets a reply. If you see a connection error, start the Ollama app and try again.

Close memory-heavy apps when using a local model on 8 GB RAM. `JARVIS_ENDPOINT` overrides the Ollama URL (default `http://localhost:11434/api/chat`).

## 4. Use it

- **Type** a message and press **Enter** (or **TRANSMIT**).
- **Voice:** click **◉ LISTEN**, speak, click **■ STOP** to end. This uses Windows speech recognition in the Windows display language. If it fails, open **Settings → Privacy & security → Microphone** and turn on microphone access for desktop apps.
- **Built-in commands** run locally without AI:
  `open notepad` · `open calculator` · `open explorer` · `open task manager` · `open paint` · `what time is it` · `system status` (`launch …` also works)
- Replies are spoken with an installed British male voice if one exists; otherwise the Windows default voice is used. To add one: **Settings → Time & language → Speech → Add voices → English (United Kingdom)**.
- Conversation history is stored in `%LOCALAPPDATA%\JARVIS\memory.db`. The last 10 messages are sent as context.

## Project files

| File | Responsibility |
|---|---|
| `src/JARVIS/MainWindow.xaml(.cs)` | HUD layout, chat flow, voice |
| `src/JARVIS/App.xaml` | Theme colours and styles |
| `src/JARVIS/AiService.cs` | OpenAI Responses API and Ollama chat clients |
| `src/JARVIS/MemoryStore.cs` | SQLite conversation history |
| `src/JARVIS/WindowsCommandRouter.cs` | Allowlisted local commands |
| `src/JARVIS/SystemTelemetry.cs` | CPU, RAM and disk readings (Win32) |
| `CLAUDE.md` | Rules for Claude working in this repo |
| `raw/` · `wiki/` · `output/` | Knowledge base: sources · compiled knowledge · generated deliverables |

Security rules are in [wiki/security.md](wiki/security.md).
