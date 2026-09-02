---
name: development-session-observability
description: Analyze Codex session telemetry passively and add sparse, privacy-safe delivery markers where native telemetry lacks semantic context.
---

# Development session observability

Measure delivery-system behavior from Codex's own session telemetry. This skill is Codex-specific but user-, organization-, repository-, and project-agnostic. It is never a delivery gate and never asks an agent to maintain a parallel ledger, dashboard, or transcript archive.

## Privacy and authority

- Never edit `$CODEX_HOME/sessions`, copy a transcript, or emit prompts, reasoning, source, command arguments or output, credentials, or raw transcript text.
- Analyze only a supplied transcript file or an explicitly bounded sessions directory. The reader selects fields through a strict allowlist and fails closed when session attribution is unsafe.
- Codex-native timestamps, task/session identifiers, models, effort, turns, tools, compactions, and tokens are collected passively. Do not duplicate them in markers.
- `rate_limits.credits.balance` is account state, not attributable task consumption. Never report it as task credits or derive credits from it; task credit coverage is `unavailable` unless a future exact attributable source exists.

## Sparse semantic markers

When the Supervisor provides the emitter command, each role emits only material semantic facts. Run `scripts/emit-marker.ps1`; it writes one canonical JSON object to stdout with the exact `CODEX_DELIVERY_MARKER:` prefix. Codex captures that tool output. It writes no files.

The first marker in a session must establish `runId`, `workItem`, and `role`. Later markers inherit that context. Valid fields are restricted to delivery role, phase/result, work-cycle, quality outcome and finding counts, blocker/outcome, and routing class/rationale. Keep rationale short and nonsensitive. Do not emit heartbeats or narrate work.

## Analyze the boundary

The Supervisor owns boundary analysis. At a completed or useful partial boundary, invoke `scripts/summarize-codex-sessions.ps1` with one or more supplied rollout JSONL files, or an explicit bounded sessions path. The output has per-session and per-work-item JSON rows. It keeps wall span distinct from summed active turn time: parallel session active time can exceed wall span.

The transcript reader is a version-aware best-effort backfill adapter. OpenAI does not promise a stable transcript schema. It records source and schema coverage, marks unknown or malformed shapes `partial` or `unavailable`, and never turns missing data into zero. It attributes a rollout only when its filename UUID matches the owning `session_meta.payload.id`; copied parent metadata is ignored.

For each token dimension, cumulative values are reconciled safely: the first value contributes; an increase contributes its delta; an unchanged value contributes zero; a decrease begins a new segment and contributes its current value. Last-usage records are validation or fallback only, never blindly summed.

## Preferred future ingestion

Transcript reading is a backfill adapter, not a durable API contract. Prefer supported live sources when available: OpenTelemetry, `codex exec --json`, hooks, or the Codex App Server. Any integration must preserve the same allowlist, minimize retention, avoid transcript content, and supply exact provenance and coverage.

## Role handoff

The Supervisor assigns stable run/work-item identity and passes the optional marker command to Planner, Implementor, Tester, and Reviewer. Each role emits its own sparse markers naturally at material boundaries; it does not create a ledger or summarize telemetry. The Supervisor later analyzes the bounded transcripts and reports coverage alongside outcomes.
