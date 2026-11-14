# J.A.R.V.I.S.

Just A Rather Very Intelligent System: a Windows desktop AI assistant with a holographic HUD, chat, voice input/output, SQLite memory, live system telemetry and an allowlisted command router. AI runs on the free Groq API by default, with optional fallbacks (Gemini, OpenRouter, OpenAI, local Ollama).

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

JARVIS tries every provider that has a key, in this order: **Groq → Gemini → OpenRouter → OpenAI**. If one fails or hits its daily limit, the next one answers. With no keys set, it uses a local Ollama model. The **AI MODE** panel shows the chain and which provider gave the last answer.

| Provider | Cost | Key variable | Model variable (default) |
|---|---|---|---|
| Groq | Free plan: 1,000 requests/day | `GROQ_API_KEY` | `GROQ_MODEL` (`openai/gpt-oss-120b`) |
| Gemini | Free tier; **prompts may be used by Google and read by reviewers** | `GEMINI_API_KEY` | `GEMINI_MODEL` (required) |
| OpenRouter | `:free` models: 50 requests/day | `OPENROUTER_API_KEY` | `OPENROUTER_MODEL` (required) |
| OpenAI | Paid | `OPENAI_API_KEY` | `OPENAI_MODEL` (required) |
| Ollama | Free, local | — | `OLLAMA_MODEL` (`qwen2.5:1.5b`) |

`JARVIS_PROVIDER` overrides the order, e.g. `groq,ollama`. `OLLAMA_URL` overrides the Ollama address. Variables set with `setx` apply to **new** PowerShell windows only.

### Groq (free, recommended)

1. Go to https://console.groq.com, sign in, open **API Keys**, click **Create API Key**, and copy the key.
2. In PowerShell (don't share or commit the key):
   ```powershell
   setx GROQ_API_KEY "paste-your-key-here"
   ```
3. Open a **new** PowerShell window, go to `src\JARVIS`, and run `dotnet run`.
4. Check: the top-right shows **AI CONFIGURED** (green), AI MODE shows `GROQ (openai/gpt-oss-120b)`, and "Hello" gets a reply.

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
| `src/JARVIS/AiService.cs` | One OpenAI-compatible client with provider fallback |
| `src/JARVIS/MemoryStore.cs` | SQLite conversation history |
| `src/JARVIS/WindowsCommandRouter.cs` | Allowlisted local commands |
| `src/JARVIS/SystemTelemetry.cs` | CPU, RAM and disk readings (Win32) |
| `CLAUDE.md` | Rules for Claude working in this repo |
| `raw/` · `wiki/` · `output/` | Knowledge base: sources · compiled knowledge · generated deliverables |

Security rules are in [wiki/security.md](wiki/security.md).
