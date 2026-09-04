# Model routing

Select and pass an available Codex model plus reasoning setting for every bounded Planner, Implementor, Tester, and Reviewer task. Use issue evidence, record the classification and rationale in the durable handoff or telemetry, and never rely on a role or host default.

| Issue evidence | Model and reasoning |
| --- | --- |
| Mechanical, repetitive, or narrowly scripted | `gpt-5.6-luna`, `low` or `medium` |
| Tests from precise acceptance criteria | `gpt-5.6-luna`, `medium` |
| Well-specified but lengthy or exhaustive | `gpt-5.6-luna`, `high` or `max` |
| Normal feature implementation | `gpt-5.6-terra`, `medium` |
| Routine debugging or review | `gpt-5.6-terra`, `medium` |
| Unfamiliar or unclear architecture | `gpt-5.6-sol`, `medium` |
| Ambiguous cross-cutting, concurrent, or distributed behavior | `gpt-5.6-sol`, `medium` or `high` |
| Architecture, security, or difficult root cause | `gpt-5.6-sol`, `high` |
| Exceptionally hard work | `gpt-5.6-sol`, `max` |

Reroute only at a clean bounded boundary and on evidence. Escalate when two consecutive verification/fix cycles produce no new diagnosis, two distinct approaches fail the same acceptance behavior, review exposes architectural, security, or distributed risk, the task crosses its slice boundary, or the agent cannot state a testable hypothesis.

After two such cycles, circuit-break: preserve the role-owned recovery state. Push only a permitted committed code checkpoint; keep ignored review receipts local. Stop retries and send `blocked` or `human-action` so the Supervisor can select a new model, re-plan, or request a decision. Downgrade only for a genuinely routine new phase. Routing is complete when the handoff records the exact supported pair, classification, rationale, and any deliberate deviation.
