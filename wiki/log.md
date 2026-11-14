# Log

Append-only record of knowledge-base ingests and significant project changes. Newest last.

## 2026-11-14: Initial ingest and Phase 1 cleanup

- Ingested `raw/2026-11-14-chatgpt-jarvis-spec.md` and `raw/2026-11-14-jarvis-windows-starter.zip`. Created `architecture.md`, `roadmap.md` and `security.md`.
- Moved the starter into `src/JARVIS/` and fixed it so it builds with 0 warnings on .NET SDK 10.0.300 and launches:
  - Removed `CharacterSpacing` (not a WPF property) and fixed the invalid `System.Windows.Media.Effects.DropShadowEffect` XAML element.
  - Added `System.IO` and `System.Net.Http` global usings (WPF implicit usings exclude them).
  - Updated packages to 10.0.15, which clears advisory GHSA-2m69-gcr7-jv3q in SQLitePCLRaw 2.1.11.
  - Fixed the current prompt being sent to the AI twice (history was read after saving it).
  - OpenAI: use `instructions` and parse `output[].content[type=output_text]`; `output_text` is an SDK helper, not a REST field. Removed the unverifiable default model; `JARVIS_MODEL` is required for OpenAI.
  - Replaced `PerformanceCounter` and `Microsoft.VisualBasic.ComputerInfo` with Win32 `GetSystemTimes` / `GlobalMemoryStatusEx`, which are locale-independent and need no extra package.
  - Rotating ring now has a dash pattern, so the rotation is visible. Chat auto-scrolls. Speech recognition uses the Windows default language and ignores audio while JARVIS speaks. Commands tolerate trailing punctuation and "launch …".
  - Removed the welcome text that referred to a nonexistent `appsettings.json`. Merged `ROADMAP.md` into `wiki/roadmap.md`.

## 2026-11-14: Free AI provider chain

- Researched free LLM APIs and checked each against official pages: [Groq rate limits](https://console.groq.com/docs/rate-limits), [Groq data](https://console.groq.com/docs/your-data), [Gemini rate limits](https://aistudio.google.com/docs/rate-limits), [Gemini terms](https://aistudio.google.com/docs/terms), [OpenRouter limits](https://openrouter.ai/docs/api-reference/limits). Results are in `architecture.md` → AI providers.
- Replaced the separate OpenAI Responses and Ollama clients with one OpenAI-compatible Chat Completions client and an ordered fallback chain (Groq → Gemini → OpenRouter → OpenAI, else Ollama). `JARVIS_MODEL` / `JARVIS_ENDPOINT` became `<PROVIDER>_MODEL` / `OLLAMA_URL`.
- Verified with the user's Groq key: a live reply from `openai/gpt-oss-120b` with history carried over, and a fallback run (Ollama unreachable → Gemini skipped for no key → Groq answered). App launches; build has 0 warnings.

## 2026-11-14: Voice, wake word and tool system

- Added [capabilities.md](capabilities.md), the film-JARVIS gap list, kept current per feature.
- **Speech-to-text:** replaced Windows dictation with NAudio capture + energy VAD + Groq Whisper (`whisper-large-v3-turbo`), dropping segments with `no_speech_prob ≥ 0.5`.
- **Tools (Phase 3 core):** Chat Completions tool calling with an 8-step loop; `ToolRegistry` does permission → run → audit (`tool_audit` table); `close_app` requires a Yes/No approval that defaults to No.
- **Tools added:** `open_app`, `list_open_apps`, `close_app`, `set_volume`, `get_time`, `system_status`, `web_search`. `WindowsCommandRouter` was removed; its phrases became offline shortcuts into the same tools.
- **Web search:** Tavily ([credits](https://docs.tavily.com/documentation/api-credits), [search API](https://docs.tavily.com/documentation/api-reference/endpoint/search)) with a Wikipedia fallback. Rejected Groq `browser_search` ([docs](https://console.groq.com/docs/browser-search)): one test query used about 372K tokens and gave an outdated answer.
- **Wake word:** offline `System.Speech` grammar check per utterance, then the Whisper transcript must contain the name. Chosen over openWakeWord because it needs no model downloads, and its pre-trained models are licensed non-commercial.
- **Groq models:** the free daily token limit is per model, so the default became `gpt-oss-120b,gpt-oss-20b` with model-level fallback. Found and fixed a bug where the `tools` JSON node was reused across fallback requests ("node already has a parent").
- **Verified with synthesized speech and the live Groq API:**
  - VAD produced one utterance per phrase; wake word positives detected, negatives rejected offline; Whisper transcripts exact.
  - Agent combined `get_time` + `system_status`, used `web_search`, and the `close_app` decline/approve path left Notepad open and then closed it.
  - All 12 tool calls were in the audit log. Build has 0 warnings; the app launches.
- The `gpt-oss-20b` web answer named Tony Stark (the in-universe creator) rather than the comic creators: a model-quality limit, noted in capabilities.md.
