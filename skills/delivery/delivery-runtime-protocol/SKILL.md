---
name: delivery-runtime-protocol
description: Apply shared delivery rules when propagating push/merge authorization, selecting agent models, managing review receipts, or coordinating pending work.
---

# Delivery runtime protocol

Provide the shared runtime rules used across bounded delivery roles. The invoking role retains its own authority, code ownership, and completion gates; this skill grants no additional mutation authority.

Read only the branch required by the current handoff:

- Before delegating or recovering mutating work, or deciding whether a push or merge needs user confirmation, read [standing delivery authorization](references/standing-authorization.md). Recover the existing grant and execute within it; distinguish readiness checks from new consent.

- Before selecting a Supervisor model or spawning or rerouting any bounded role or review axis, read [model routing](references/model-routing.md) for classification, escalation, worker allocation, and verification limits.
- Before creating, validating, or resuming temporary inter-agent review state, read [local review artifacts](references/review-artifacts.md).
- Before delegating work whose result must wake this role, or waiting on a child or external gate, read [message-driven waiting](references/message-driven-waiting.md). This applies to Supervisors, Implementors, Reviewer coordinators, and any other role awaiting bounded work.

## Completion

The selected branch is complete only when:

- standing authorization recovers the user-approved scope, endpoint, restrictions, and role-owned operations, then propagates that grant or identifies a concrete missing boundary; or
- model routing records an effective supported model and reasoning pair, evidence-backed classification, rationale, any deliberate deviation, worker allocation, and verification boundary; or
- local review recovery validates the current committed snapshot and each applicable receipt by content, then either reuses a complete result or identifies the exact missing work to resume; or
- message-driven waiting preserves a supported wake-up route and bounded recovery deadline, then returns control for a material signal or due check without treating a timeout as completion.

Return to the invoking delivery role after completing the selected branch.
