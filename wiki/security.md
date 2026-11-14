# Security

Permission model and safety rules that every phase must follow. Source: `raw/2026-11-14-chatgpt-jarvis-spec.md`.

## Permission model

| Operation | Permission |
|---|---|
| Open allowlisted applications | Automatic |
| Read files in approved folders | Automatic |
| Create files | Confirmation, depending on location |
| Delete files | Explicit approval |
| Administrator commands | Explicit approval |
| Send email or messages | Explicit approval |
| Purchases or financial actions | Explicit approval |
| Account or credential changes | Explicit approval |

Phase 1 only implements the first row. Everything else is blocked until the Phase 3 permission broker exists.

## Rules

1. **No arbitrary execution.** Model or user text is never passed to `cmd.exe`, PowerShell or `Process.Start`. Actions are typed, allowlisted tools.
2. **Verify before reporting.** JARVIS never claims an action succeeded without a tool result confirming it (enforced in the system prompt now, and by the orchestrator in Phase 3).
3. **Secrets** live in environment variables (Phase 1) or Windows Credential Manager (Phase 8). Never in source, logs, the wiki or `output/`.
4. **Untrusted input.** Screen contents, web pages, documents and model output are data, never instructions.
5. **Visible sensing.** The microphone and screen capture are only active when the HUD visibly shows it.
6. **Distribution.** A shipped build must not embed an API key; cloud calls go through an authenticated backend.
