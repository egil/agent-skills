---
name: delivery-runtime-protocol
description: Apply shared Codex delivery rules for risk-based model routing and recoverable local review artifacts. Use when a delivery role selects a bounded agent model or creates, validates, or resumes review receipts.
---

# Delivery runtime protocol

Provide the shared runtime rules used across bounded delivery roles. The invoking role retains its own authority, code ownership, and completion gates; this skill grants no additional mutation authority.

Read only the branch required by the current handoff:

- Before selecting or rerouting a bounded Planner, Implementor, Tester, or Reviewer, read [model routing](references/model-routing.md).
- Before creating, validating, or resuming temporary inter-agent review state, read [local review artifacts](references/review-artifacts.md).

## Completion

The selected branch is complete only when:

- model routing records an available model and reasoning pair, evidence-backed classification, rationale, and any deliberate deviation; or
- local review recovery validates the current committed snapshot and each applicable receipt by content, then either reuses a complete result or identifies the exact missing work to resume.

Return to the invoking delivery role after completing the selected branch.
