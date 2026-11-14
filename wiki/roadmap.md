# Roadmap

The eight build phases, in order. A phase is done when its deliverables build, run and are verified. Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`.

| # | Phase | Deliverables | Status |
|---|---|---|---|
| 1 | Desktop foundation | WPF HUD, chat, SQLite history, free AI provider chain (Groq → Gemini → OpenRouter → OpenAI / Ollama), push-to-talk dictation, Windows TTS, CPU/RAM/disk telemetry, allowlisted commands | ✅ Done 2026-11-14 |
| 2 | Advanced voice | "Hey JARVIS" wake word (openWakeWord) with visible mic indicator; VAD + STT (whisper.cpp locally, or Groq's free `whisper-large-v3`); Piper or cloud neural TTS with voice selection; barge-in interruption; listening/speaking HUD states | ⏭ Next |
| 3 | AI agent | Structured tool calling; typed tool registry with JSON schemas; permission broker + confirmation UI; task state machine (plan → authorize → execute → verify → report); audit log of every tool call | |
| 4 | Windows intelligence | Windows UI Automation; app-specific adapters; process management; file operations limited to user-approved folders | |
| 5 | Computer vision | User-initiated screen capture; OCR and window metadata; cloud image understanding only after explicit consent; redaction of passwords/tokens/sensitive windows | |
| 6 | Advanced memory | Preferences and project records; conversation summaries; semantic retrieval with source and timestamp; user inspect/edit/delete controls | |
| 7 | External integrations | Web search and controlled browser automation; email/calendar with least privilege; smart-home and local-network adapters; phone notifications | |
| 8 | Production | Windows Credential Manager for secrets; opt-in startup/background service; floating mini-HUD and full-screen mode; GPU/temperature/network telemetry; signed MSIX installer and updates; tests; opt-in crash reporting; resource profiling | |
