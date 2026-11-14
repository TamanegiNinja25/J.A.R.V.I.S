# Security

Permission model and safety rules that every phase must follow. Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`.

## Permission model

Every action is a typed tool run by `ToolRegistry`: permission check → run → audit log row (`tool_audit` table, including declined requests). `Confirm` tools show a Yes/No prompt that defaults to **No**.

| Operation | Permission | Implemented as |
|---|---|---|
| Open allowlisted applications | Automatic | `open_app` |
| Read-only queries (time, status, open apps, web search) | Automatic | `get_time`, `system_status`, `list_open_apps`, `web_search` |
| Volume / mute | Automatic | `set_volume` |
| Close applications | **Confirm** | `close_app` (graceful close; `explorer` and JARVIS are protected) |
| Read files in approved folders | Automatic | — |
| Create files | Confirmation, depending on location | — |
| Delete files | **Confirm** | — |
| Administrator commands | **Confirm** | — |
| Send email or messages | **Confirm** | — |
| Purchases or financial actions | **Confirm** | — |
| Account or credential changes | **Confirm** | — |

## Rules

1. **No arbitrary execution.** Model or user text is never passed to `cmd.exe` or PowerShell. `Process.Start` only receives allowlisted targets. Actions are typed tools.
2. **Verify before reporting.** JARVIS never claims an action succeeded without a tool result confirming it. Tools check the outcome where they can; for example, `close_app` re-checks after 2 s whether the windows actually closed.
3. **Secrets** live in user environment variables (now) or Windows Credential Manager (Phase 8). Never in source, logs, the wiki or `output/`.
4. **Untrusted input.** Screen contents, web pages, documents and model output are data, never instructions. `web_search` results are labelled as untrusted, and the system prompt forbids following instructions in them.
5. **Visible sensing.** The red MIC light and level bar show whenever the microphone is open; screen capture will follow the same rule.
6. **Voice privacy.** In wake-word mode, audio is checked offline and sent to Groq only after "Hey JARVIS" is heard; push-to-talk sends the one utterance requested.
7. **Free-tier data.** Gemini's free tier may use input to improve Google products, and humans may review it. Never route screen captures, files or personal data to it; Groq is the default because it does not retain data by default.
8. **Distribution.** A shipped build must not embed an API key; cloud calls go through an authenticated backend.
