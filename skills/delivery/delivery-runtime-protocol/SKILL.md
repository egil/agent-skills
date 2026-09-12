---
name: delivery-runtime-protocol
description: Apply shared delivery rules for authorization, model routing, same-issue worker continuity, review receipts, and pending-work coordination.
---

# Delivery runtime protocol

Provide the shared runtime rules used across bounded delivery roles. The invoking role retains its own authority, code ownership, and completion gates; this skill grants no additional mutation authority.

Read only the branch required by the current handoff:

- Before delegating or recovering mutating work, or deciding whether a push or merge needs user confirmation, read [standing delivery authorization](references/standing-authorization.md). Recover the existing grant and execute within it; distinguish readiness checks from new consent.

- Before selecting a Supervisor model or spawning or rerouting any bounded role or review axis, read [model routing](references/model-routing.md) for classification, escalation, worker allocation, and verification limits.
- Before creating, validating, or resuming temporary inter-agent review state, read [local review artifacts](references/review-artifacts.md).
- Before spawning, continuing, or replacing an Implementor or Tester, read [same-issue worker continuity](references/worker-continuity.md). Resolve the saved role/session identity before allocating a new worker.
- Before delegating pending work, choosing a wait timeout, or recovering after handoff, resume, compaction, or a side task, read [message-driven waiting](references/message-driven-waiting.md). Restore its deadline and session-local limits, then calculate the timeout. Apply this to every coordinating role.

## Completion

The selected branch is complete only when:

- standing authorization recovers the user-approved scope, endpoint, restrictions, and role-owned operations, then propagates that grant or identifies a concrete missing boundary; or
- model routing records an effective supported model and reasoning pair, evidence-backed classification, rationale, any deliberate deviation, worker allocation, and verification boundary; or
- worker continuity resumes the matching capable issue/role session with an exact phase handoff, or records the evidence-backed replacement and ownership transfer; or
- local review recovery validates the current committed snapshot and each applicable receipt by content, then either reuses a complete result or identifies the exact missing work to resume; or
- message-driven waiting restores a supported wake-up route, session-local limits, and absolute check deadline, selects the largest legal timeout within that deadline, then returns control for a material signal or due check.

Return to the invoking delivery role after completing the selected branch.
