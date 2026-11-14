# Roadmap

The eight build phases, in order. A phase is done when its deliverables build, run and are verified. Feature-level status is in [capabilities.md](capabilities.md). Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`.

| # | Phase | Deliverables | Status |
|---|---|---|---|
| 1 | Desktop foundation | WPF HUD, chat, SQLite history, free AI provider chain, CPU/RAM/disk telemetry, Windows TTS | ✅ Done 2026-11-14 |
| 2 | Advanced voice | ✅ VAD + Groq Whisper STT · ✅ "Hey JARVIS" wake word (offline check) with mic indicator · ❌ Piper/cloud neural TTS with voice selection · ❌ barge-in interruption · ❌ speaking animation | ⚠️ Core done 2026-11-14 |
| 3 | AI agent | ✅ Structured tool calling · ✅ typed tool registry with JSON schemas · ✅ approval prompt · ✅ audit log of every tool call · ❌ explicit plan → verify state machine · ❌ spoken approval | ⚠️ Core done 2026-11-14 |
| 4 | Windows intelligence | ✅ open/list/close apps, volume · ❌ Windows UI Automation · ❌ app-specific adapters · ❌ process management · ❌ file operations limited to user-approved folders | ⚠️ Started |
| 5 | Computer vision | User-initiated screen capture; OCR and window metadata; cloud image understanding only after explicit consent; redaction of passwords/tokens/sensitive windows | ⏭ |
| 6 | Advanced memory | Preferences and project records; conversation summaries; semantic retrieval with source and timestamp; user inspect/edit/delete controls | ⏭ |
| 7 | External integrations | ✅ Web search (Tavily/Wikipedia) · ❌ page reading · ❌ controlled browser automation · ❌ email/calendar with least privilege · ❌ smart-home and local-network adapters · ❌ phone notifications | ⚠️ Started |
| 8 | Production | Windows Credential Manager for secrets; opt-in startup/background service; floating mini-HUD and full-screen mode; GPU/temperature/network telemetry; signed MSIX installer and updates; tests; opt-in crash reporting; resource profiling | ⏭ |
