# Capabilities

What a film-style JARVIS needs, and what exists today. Update a row whenever a feature lands. Phases refer to [roadmap.md](roadmap.md).

✅ works · ⚠️ basic version · ❌ not started

| Area | Status | Has now | Still missing | Phase |
|---|---|---|---|---|
| **Voice** | ⚠️ | Push-to-talk and always-on "Hey JARVIS" (offline check), Groq Whisper speech-to-text, mic light + level bar, no self-hearing | Natural British TTS voice (Piper/cloud), talking over JARVIS to interrupt it, waveform/speaking animation, multilingual wake word | 2 |
| **Agent brain** | ✅ core | Tool calling with multi-step loop (8 steps), approval prompt for risky tools, audit log in SQLite, provider/model fallback | Explicit plan → verify state machine, spoken approval ("yes, do it"), audit viewer in the HUD | 3 |
| **Windows control** | ⚠️ | Open 6 allowlisted apps, list open apps, close apps (with approval), volume/mute | Brightness, window arrangement, processes, settings pages, files in approved folders, UI Automation of other apps | 4 |
| **Vision** | ❌ | — | Screen capture on request, OCR, "explain this error" | 5 |
| **Memory** | ⚠️ | Last 10 messages | Profile and preferences, project notes, summaries, semantic recall, memory viewer/editor | 6 |
| **Internet** | ⚠️ | `web_search`: Tavily (with key) or Wikipedia | Page reading/summaries, monitoring topics, controlled browser automation | 7 |
| **Personal assistant** | ❌ | — | Calendar, reminders, email, daily briefing, notifications | 7 |
| **Devices** | ❌ | — | Smart home, phone notifications, Bluetooth | 7 |
| **System monitor** | ⚠️ | CPU, RAM, disk on the HUD; `system_status` tool | GPU, temperatures, network, battery, threshold alerts | 8 |
| **Always-on interface** | ⚠️ | One window | Start with Windows, tray icon, floating mini-HUD, full-screen mode, activity/history/notification panels | 8 |
| **Production** | ⚠️ | Keys in user environment variables | Windows Credential Manager, installer, automated tests | 8 |

## Not achievable (film only)

- **Real holograms.** It will always be on a screen.
- **Suit or hardware control, and "it can hack anything."** It can only control what it is given access to.
- **Human-level understanding.** AI makes mistakes, which is why risky actions require approval. Small free models such as `gpt-oss-20b` are noticeably weaker on facts.
- **The film actor's voice.** Cloning it would be unauthorized; an original British-inspired voice is used instead.
