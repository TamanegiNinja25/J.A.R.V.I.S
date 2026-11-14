# J.A.R.V.I.S.

Just A Rather Very Intelligent System: a Windows desktop AI assistant with a holographic HUD, chat, voice input/output, SQLite memory, live system telemetry and an allowlisted command router. AI runs on the free Groq API by default, with optional fallbacks (Gemini, OpenRouter, OpenAI, local Ollama).

Current state: Phase 1 done, the core of Phases 2 and 3 done (voice + tools); see the [roadmap](wiki/roadmap.md) and the [film-JARVIS capability list](wiki/capabilities.md). Project knowledge lives in [`wiki/`](wiki/index.md).

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

JARVIS tries every provider that has a key, in this order: **Groq → Gemini → OpenRouter → OpenAI**. If one fails or hits its daily limit, the next one answers. With no keys set, it uses a local Ollama model. The **AI MODE** panel shows the chain and which provider gave the last answer.

| Provider | Cost | Key variable | Model variable (default) |
|---|---|---|---|
| Groq | Free plan, limits per model | `GROQ_API_KEY` | `GROQ_MODEL` (`openai/gpt-oss-120b,openai/gpt-oss-20b`) |
| Gemini | Free tier; **prompts may be used by Google and read by reviewers** | `GEMINI_API_KEY` | `GEMINI_MODEL` (required) |
| OpenRouter | `:free` models: 50 requests/day | `OPENROUTER_API_KEY` | `OPENROUTER_MODEL` (required) |
| OpenAI | Paid | `OPENAI_API_KEY` | `OPENAI_MODEL` (required) |
| Ollama | Free, local | — | `OLLAMA_MODEL` (`qwen2.5:1.5b`) |

A comma-separated model list falls back model by model; Groq's free daily token limit is per model, so the default doubles it. `JARVIS_PROVIDER` overrides the provider order, e.g. `groq,ollama`. `OLLAMA_URL` overrides the Ollama address. Variables set with `setx` apply to **new** PowerShell windows only.

### Groq (free, recommended)

1. Go to https://console.groq.com, sign in, open **API Keys**, click **Create API Key**, and copy the key.
2. In PowerShell (don't share or commit the key):
   ```powershell
   setx GROQ_API_KEY "paste-your-key-here"
   ```
3. Open a **new** PowerShell window, go to `src\JARVIS`, and run `dotnet run`.
4. Check: the top-right shows **AI CONFIGURED** (green), AI MODE shows `GROQ (openai/gpt-oss-120b) → GROQ (openai/gpt-oss-20b)`, and "Hello" gets a reply.

### Optional fallback: Gemini (free)

1. Go to https://aistudio.google.com, click **Get API key** → **Create API key**, and copy it.
2. In AI Studio, open the model list and copy the ID of a **Flash** model available on the free tier.
3. In PowerShell:
   ```powershell
   setx GEMINI_API_KEY "paste-your-key-here"
   setx GEMINI_MODEL "paste-model-id-here"
   ```
4. Open a new window and run `dotnet run`. AI MODE should now end with `→ GEMINI (...)`.

Don't use the Gemini free tier for private content; Google's terms say free-tier input may be used to improve its products.

### Optional: live web search (Tavily, free)

Without this, `web_search` uses Wikipedia: free, but not current news.

1. Go to https://app.tavily.com and sign up (1,000 free searches/month, no card).
2. Copy the API key shown on the dashboard (starts with `tvly-`).
3. In PowerShell:
   ```powershell
   setx TAVILY_API_KEY "paste-your-key-here"
   ```
4. Open a new window, run `dotnet run`, and ask "Search the web for today's top technology news". The answer should mention current sources.

### Optional: local Ollama (offline)

1. Install Ollama from https://ollama.com/download/windows.
2. Download a small model (about 1 GB):
   ```powershell
   ollama pull qwen2.5:1.5b
   ```
3. To use it as the last fallback, set the order explicitly, then open a new window and run `dotnet run`:
   ```powershell
   setx JARVIS_PROVIDER "groq,ollama"
   ```

On 8 GB RAM, close memory-heavy apps while a local model runs.

## 4. Use it

**Type** a message and press **Enter** (or **TRANSMIT**).

**Voice** (needs `GROQ_API_KEY`; speech is transcribed by Groq Whisper):
- **◉ LISTEN**: speak one command. The mic closes after it.
- **HEY JARVIS: ON**: always listening. Say "Hey JARVIS, open notepad", or say "Hey JARVIS", wait for "Yes?", then give the command. The wake phrase is checked **offline**; audio is sent to Groq only after it is heard.
- The red **MIC** light and level bar show whenever the microphone is open. If the bar doesn't move when you speak, raise the input level in **Settings → System → Sound → Input**.
- JARVIS ignores the mic while it is speaking.

**What JARVIS can do** (the AI picks these tools itself):

| Tool | Permission | Example |
|---|---|---|
| `open_app` | Automatic | "Open the calculator" (notepad, calculator, file explorer, task manager, paint, settings) |
| `list_open_apps` | Automatic | "What apps are open?" |
| `close_app` | **Asks you first** | "Close notepad". The app's own save prompt still appears. |
| `set_volume` | Automatic | "Set the volume to 30", "mute" |
| `get_time` / `system_status` | Automatic | "What time is it?", "How much memory is free?" |
| `web_search` | Automatic | "Search the web for …" (Tavily if set up, else Wikipedia) |

Exact phrases run instantly **without the AI**, and also work offline: `open notepad` · `open calculator` · `open file explorer` · `open task manager` · `open paint` · `open settings` · `what time is it` · `system status` · `mute` · `unmute` · `volume up` · `volume down` (`launch …` also works).

Replies are spoken with an installed British male voice if one exists. To add one: **Settings → Time & language → Speech → Add voices → English (United Kingdom)**.

`%LOCALAPPDATA%\JARVIS\memory.db` stores the conversation (the last 10 messages are sent as context) and an audit log of every tool request.

## Project files

| File | Responsibility |
|---|---|
| `src/JARVIS/MainWindow.xaml(.cs)` | HUD, chat flow, voice modes, approval prompt |
| `src/JARVIS/App.xaml` | Theme colours and styles |
| `src/JARVIS/AiService.cs` | OpenAI-compatible client: provider/model fallback and the tool-calling loop |
| `src/JARVIS/ToolRegistry.cs` | Permission check, execution and audit for every tool; offline shortcuts |
| `src/JARVIS/BuiltInTools.cs` | The tools: apps, volume, time, status, web search |
| `src/JARVIS/MicrophoneListener.cs` | Mic capture and voice activity detection |
| `src/JARVIS/WakeWordDetector.cs` | Offline "Hey JARVIS" check |
| `src/JARVIS/SpeechToText.cs` | Groq Whisper transcription |
| `src/JARVIS/MemoryStore.cs` | SQLite conversation history and tool audit log |
| `src/JARVIS/Shared.cs` | Environment settings and the shared HTTP client |
| `src/JARVIS/SystemTelemetry.cs` | CPU, RAM and disk readings (Win32) |
| `CLAUDE.md` | Rules for Claude working in this repo |
| `raw/` · `wiki/` · `output/` | Knowledge base: sources · compiled knowledge · generated deliverables |

Security rules are in [wiki/security.md](wiki/security.md).
