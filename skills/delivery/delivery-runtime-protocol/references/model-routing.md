# Model routing

Classify by ambiguity, risk, and required judgment. Terra Medium is the cost-conscious default; select directly from the evidence below. This is a starting policy to calibrate against accepted issues, not a repository-proven optimum. Issue length alone does not justify more reasoning.

| Work and evidence | Model and reasoning |
| --- | --- |
| Issue classification, dependency tracking, scheduling, or straightforward slice planning | `gpt-5.6-terra`, `medium` |
| Mechanical edits, repetitive mappings, or narrowly specified tests with settled seams and fidelity | `gpt-5.6-luna`, `low` or `medium` |
| Normal feature implementation, debugging, or test-design judgment | `gpt-5.6-terra`, `medium` |
| Complex but well-bounded implementation where Sol reliably succeeds | `gpt-5.6-sol`, `medium` or `high` |
| Architecture, ambiguous requirements, high-risk implementation, or difficult cross-system diagnosis | `gpt-6-astra`, `medium` |
| Routine PR, Standards, or Spec review | `gpt-5.6-terra`, `high` |
| Concurrency, consistency, migration, or security-sensitive review | `gpt-6-astra`, `high` |
| Hard problem still unresolved after productive investigation | `gpt-6-astra`, `high`, then `xhigh` if needed |

`xhigh` means Extra High reasoning effort and is supported by [GPT-6 Astra](https://developers.openai.com/api/docs/models/gpt-6-astra). Validate the selected pair against the current runtime before launch as described below.

Start directly with Astra Medium for an unclear ordering or consistency bug or an architectural change, including Orleans/Azure work. Sol is an optional choice for bounded demanding work, never a mandatory step before Astra. Try Astra Medium before routinely increasing Sol to `max`.

Use the lowest effort that delivers the required judgment and verification. Keep repetitive work at modest effort even when lengthy. `max` and `ultra` are outside the default policy; use them only when supported and a recorded unresolved problem justifies their cost.

## Make the selection effective

Before every bounded spawn, including nested children and Standards/Spec axes, pass both the available model identifier and reasoning setting explicitly. Check the runtime's supported pairs and the effective launch settings; if the requested pair is unavailable or overridden, record a deliberate supported alternative that fits the risk or return the capability gap to the owner. Model changes belong at bounded handoffs, not invisible switches inside a running task.

Keep delivery role profiles free of fixed model settings. Codex custom-agent settings can override spawn settings, and unspecified settings can inherit from a parent. When the collaboration tool requires it, use `fork_turns="none"` or a supported bounded history count to make explicit overrides effective; include the durable brief and evidence in the handoff. See [Codex subagent configuration](https://learn.chatgpt.com/docs/agent-configuration/subagents).

## Reclassify stalled work

After two consecutive failed fix/test cycles without a new diagnosis, stop the retry loop and return to the immediate routing owner. Choose the next pair from the problem evidence; the cycle count determines when to reclassify, not which model or effort to use. Reclassify earlier when two distinct approaches fail the same acceptance behavior, review exposes architectural or distributed risk, the task exceeds its slice, or the agent cannot state a testable hypothesis. A planned meaningful-red test is not a failed fix cycle.

Preserve the evidence, failed hypotheses and approaches, current diff and commit, remaining checks, and role-owned recovery artifacts. Push only a permitted committed code checkpoint; ignored review receipts stay local. Send the contract's `blocked` signal with the checkpoint and routing rationale. The owner selects the appropriate model directly, skipping tiers when warranted, or routes an expanded slice back to planning. Reserve `human-action` for an actual decision or authorization gap; a model escalation alone does not require user permission.

Once the difficult decision or root cause is resolved, hand clearly specified implementation or cleanup to a cheaper worker at the next bounded boundary. Preserve role ownership and independent review.

## Bound delegation and checks

The top-level Supervisor owns a run-wide limit of three active workers, including nested workers and review axes. Every parent receives and shares that allocation; children do not acquire a separate quota. Waiting coordinators yield their active-work allocation, while any stricter host limit on open threads still applies. Reserve capacity for testing and review before launching more implementation. When capacity is unavailable, queue work or serialize independent review axes in separate agents.

Delegate bounded supporting work to explicitly selected cheaper agents when the expected saving exceeds coordination overhead. Give each worker a concrete responsibility, owned paths when editing, the evidence it needs, and a completion criterion. Keep shared decisions with their owner until settled.

Pass the verification boundary with each assignment: run checks appropriate to the change and every required repository gate. After they pass, broaden or repeat testing only for new changes, failures, missing evidence, or unresolved risks. An unchanged, valid snapshot receipt can be reused under the local-review-artifact protocol.

## Calibrate cost per accepted issue

Select Standard speed when optimizing Codex credits and verify the effective speed alongside the model pair. Use Fast only for an explicit latency tradeoff; if the runtime cannot select speed, disclose the effective setting and limitation. As of 2026-09-05, Astra Fast consumes 2.5 times its Standard credits where available. [Codex speed](https://learn.chatgpt.com/docs/agent-configuration/speed).

Compare total consumption across attempts and workers, plus developer corrections, per accepted issue. A higher token price can still yield a lower task cost. At the [published Standard credit rates](https://learn.chatgpt.com/docs/pricing), Astra costs 2.5 times Sol per token across input, cache, and output; at an unchanged mix it needs 60% fewer tokens to break even. That is arithmetic, not measured retry behavior or a prediction that Astra Medium beats Sol Max. Reasoning output contributes to consumption.

Use the Supervisor's passive observability analysis and exact attributable sources. Report unavailable credits or developer-correction counts as unavailable; neither account balances nor token-price calculations establish measured task credits. Keep missing metrics out of delivery gates.

Routing is complete when the handoff records the effective supported pair, evidence-backed classification, rationale, any deliberate deviation, available worker allocation, and verification boundary. Model positioning and effort guidance were checked against [OpenAI's model guidance](https://learn.chatgpt.com/docs/models) and [Astra delegation and verification guidance](https://developers.openai.com/api/docs/guides/latest-model?model=gpt-6-astra) on 2026-09-05; recheck product facts when changing this policy.
