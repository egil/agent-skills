# Model routing

Use GPT-6.1 Sol Medium as the cost-conscious default for delivery roles and independent review axes. Classify by the actual unresolved judgment and verification needs; issue length or technology names alone do not justify a more expensive model. This is a starting policy to calibrate against accepted issues, not a repository-proven optimum.

| Work and evidence | Model and reasoning |
| --- | --- |
| Supervision, issue classification, dependency tracking, scheduling, or slice planning | `gpt-6.1-sol`, `medium` |
| Mechanical edits, repetitive mappings, or narrowly specified tests with settled seams and fidelity | `gpt-6-luna`, `low` or `medium`, when delegation saves more than its overhead |
| Normal feature implementation, debugging, or test-design judgment | `gpt-6.1-sol`, `medium` |
| Demanding bounded implementation, architecture, cross-system diagnosis, or concurrency, consistency, migration, or security analysis | `gpt-6.1-sol`, `high` |
| Routine PR, Standards, or Spec review | `gpt-6.1-sol`, `medium` |
| Demanding review with concrete invariants or conflicting evidence to resolve | `gpt-6.1-sol`, `high` |
| Concrete reasoning problem unresolved by productive Sol investigation, or evidence that Sol cannot meet the required judgment | `gpt-6-astra`, `medium`; increase to `high`, then `xhigh` only for a recorded remaining problem |

Select Astra for a bounded unresolved question and record the evidence, what Sol could not resolve or why its capability is insufficient, and the result needed to return to Sol. Architecture, ambiguity, distributed systems, Orleans/Azure, or security-sensitive scope alone does not establish that need. Fix missing context, an oversized slice, or an unavailable dependency through the appropriate owner.

Apply the same selection criteria separately to the Reviewer coordinator and each Standards/Spec axis. Each axis covers the full snapshot, but inherits neither an expensive parent's model nor an automatic Astra assignment from the subject matter.

Use the lowest effort that delivers the required judgment and verification. Keep repetitive work at modest effort even when lengthy. `max` and `ultra` are outside the default policy; use them only when supported and a recorded unresolved problem justifies their cost.

## Make the selection effective

Before every bounded spawn, including nested children and Standards/Spec axes, pass both the available model identifier and reasoning setting explicitly. Check the runtime's supported pairs and the effective launch settings; if the requested pair is unavailable or overridden, record a deliberate supported alternative that fits the risk or return the capability gap to the owner. Model changes belong at bounded handoffs, not invisible switches inside a running task.

Keep delivery role profiles free of fixed model settings. Codex custom-agent settings can override spawn settings, and unspecified settings can inherit from a parent. When the collaboration tool requires it, use `fork_turns="none"` or a supported bounded history count to make explicit overrides effective; include the durable brief and evidence in the handoff. See [Codex subagent configuration](https://learn.chatgpt.com/docs/agent-configuration/subagents).

## Reclassify stalled work

After two consecutive failed fix/test cycles without a new diagnosis, stop the retry loop and return to the immediate routing owner. Choose the next pair from the problem evidence; the cycle count determines when to reclassify, not which model or effort to use. Reclassify earlier when two distinct approaches fail the same acceptance behavior, review exposes architectural or distributed risk, the task exceeds its slice, or the agent cannot state a testable hypothesis. A planned meaningful-red test is not a failed fix cycle.

Preserve the evidence, failed hypotheses and approaches, current diff and commit, remaining checks, and role-owned recovery artifacts. Push only a permitted committed code checkpoint; ignored review receipts stay local. Send the contract's `blocked` signal with the checkpoint and routing rationale. The owner chooses between supplying missing evidence, refining the slice, increasing Sol effort, or making an evidence-backed bounded Astra assignment under the criteria above. A retry count alone does not justify Astra. Reserve `human-action` for an actual decision or authorization gap; a model escalation alone does not require user permission.

Once Astra resolves the assigned question, return its decision, evidence, and remaining verification to a Sol worker at the next bounded boundary. Use Luna for settled mechanical work when the handoff earns its overhead. Preserve role ownership and independent review.

## Bound delegation and checks

The top-level Supervisor owns a run-wide limit of three active workers, including nested workers and review axes. Every parent receives and shares that allocation; children do not acquire a separate quota. Waiting coordinators yield their active-work allocation, while any stricter host limit on open threads still applies. Reserve capacity for testing and review before launching more implementation. When capacity is unavailable, queue work or serialize independent review axes in separate agents.

Delegate bounded supporting work to explicitly selected cheaper agents when the expected saving exceeds coordination overhead. Give each worker a concrete responsibility, owned paths when editing, the evidence it needs, and a completion criterion. Keep shared decisions with their owner until settled.

Pass the verification boundary with each assignment: run checks appropriate to the change and every required repository gate. After they pass, broaden or repeat testing only for new changes, failures, missing evidence, or unresolved risks. An unchanged, valid snapshot receipt can be reused under the local-review-artifact protocol.

## Calibrate cost per accepted issue

Select Standard speed when optimizing Codex credits and verify the effective speed alongside the model pair. Use Fast only for an explicit latency tradeoff; if the runtime cannot select speed, disclose the effective setting and limitation. Check current product rates rather than assuming a fixed speed multiplier. [Codex speed](https://learn.chatgpt.com/docs/agent-configuration/speed).

Compare total consumption across attempts and workers, plus developer corrections, per accepted issue. Include reasoning tokens and repeated context in that comparison. Use current [Codex credit rates](https://learn.chatgpt.com/docs/pricing) for Codex usage and API rates only for API-billed work. Price ratios alone do not establish measured task savings or justify Astra escalation.

Use the Supervisor's passive observability analysis and exact attributable sources. Report unavailable credits or developer-correction counts as unavailable; neither account balances nor token-price calculations establish measured task credits. Keep missing metrics out of delivery gates.

Routing is complete when the handoff records the effective supported pair, evidence-backed classification, rationale, any deliberate deviation, available worker allocation, and verification boundary. Model positioning was checked against [OpenAI's model-selection guidance](https://developers.openai.com/api/docs/guides/model-selection) on 2026-10-23; the routing choices above are the delivery policy, not a measured performance claim. Recheck product facts when changing this policy.
