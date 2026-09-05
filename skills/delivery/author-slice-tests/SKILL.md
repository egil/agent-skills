---
name: author-slice-tests
description: Author high-value tests for one delivery slice or resolve its test-owned rebase conflicts.
---

# Author slice tests

Own test code for one small delivery slice as a short-lived task against an exact snapshot in its existing worktree and linked branch. Select one mode and read only its procedure:

- [`red-contract`](references/red-contract.md): establish meaningful red before behavior-changing production work.
- [`green-baseline`](references/green-baseline.md): characterize behavior before preservation work.
- [`green-finalization`](references/green-finalization.md): prove restored assertion sensitivity and required gates after production work.
- [`rebase-conflict`](references/rebase-conflict.md): resolve only test-owned conflicts while the Implementor controls the rebase.

## Load the consuming-repository contract

The invoking Implementor must supply the delivery contract or its discoverable location. It must define the GitHub identity and mutation rules, native branch and worktree conventions, commit and push policy, default branch, test commands and required gates, Agent Brief and Verification contract format, and the upward status-signal route. If required values are absent or contradictory, report `blocked` or `human-action` to the Implementor; do not infer them.

## Dependencies

- Required internal: `$design-high-value-tests` for test-design judgment, `$verification-driven-delivery` for fidelity to the issue's Verification or approved green-baseline contract, `$delivery-runtime-protocol` for local review recovery, and `$development-session-observability` for sparse markers when the Implementor supplies its command.
- This role may receive findings from the internal `$review-delivery-slice` role; it never replaces that independent review.

Load `$delivery-runtime-protocol`'s model-routing branch to validate the Implementor's selected pair, rationale, worker allocation, and verification boundary. Return a stalled assignment through its reclassification handoff.

When a marker command is supplied, load `$development-session-observability` and emit material phases, work cycles, and quality results without delaying testing. Codex owns native usage collection; never estimate credits.

## Apply the testing contract

Load and apply `$design-high-value-tests` and `$verification-driven-delivery`. The issue's Agent Brief and Verification or approved green-baseline contract are binding. Reopen them only when current repository evidence makes them contradictory or infeasible; return that evidence to the Implementor instead of silently redesigning behavior or weakening a reviewed test.

Follow the consuming repository's testing policy, runner, meaningful-evidence rules, coverage and branch-coverage requirements, and any other enforced gates. Do not add trivial or implementation-coupled tests merely to satisfy a metric.

## Preserve role and ownership

Change only test code, test-only fixtures, and test-project support owned by the assigned slice. Do not edit production code, perform independent review, mutate issues or pull requests, mark a pull request ready, or merge. An explicitly delegated suggestion from a configured GitHub pull-request reviewer may change Tester-owned code only after the contract authorizes that mutation and the GitHub identity is reverified; synchronize and verify the exact resulting remote commit immediately.

When valuable testing requires a new or changed production seam, describe its smallest genuine design role and return it to the Implementor. When a failure exposes a production defect, return the evidence instead of fixing production code. When a reviewed test or contract is wrong, explain why and request reconsideration; change test code only after the Implementor accepts the change.

Do not absorb adjacent behavior. If the test portfolio no longer fits one bounded slice or reveals a material prerequisite or human decision, stop and tell the Implementor. The Implementor owns decomposition and bubbles human questions to the Supervisor.

## Verify the handoff before editing

Require:

- issue link, Agent Brief, and Verification or approved green-baseline contract;
- selected mode, selected model and reasoning, and exact starting or implementation commit;
- existing worktree and natively linked remote branch;
- the predictable local review snapshot directory and the specific test-owned finding IDs, when remediation was delegated;
- relevant prior review findings or approved contract changes; and
- checkpoint and push authority inherited from the issue-delivery task.

Verify expected branch and exact snapshot. Do not overwrite or silently include another agent's uncommitted work. If the handoff is dirty or points at the wrong revision, return it to the Implementor for reconciliation. The sole exception is `rebase-conflict` mode, whose handoff must identify the expected rebase, pre-rebase head, target default-branch OID, current `HEAD`, replayed commit, rebase progress and remaining todo, complete index-stage state, worktree diff, and untracked inventory.

## Persist the receipt and stop

For `red-contract` and `green-baseline`, map the handoff to review mode `test-contract`; for `green-finalization`, map it to `complete-change`. Once the exact resulting `HEAD` and comparison base are known, load `$delivery-runtime-protocol` and apply its local-review-artifact branch. Verify the Implementor's ignore setup without changing it; if the path is not ignored, return that ordinary setup correction to the Implementor before writing. Write the matching `verification.md` before returning, recording exact commands, discovery, results, fidelity, unexercised dependencies, and residual risk. Rebase-conflict mode returns its state to the controlling Implementor instead because its `HEAD` is not yet a review candidate.

Return to the Implementor:

- protected behavior and chosen test levels;
- exact base and resulting checkpoint SHAs, or all supplied rebase and index identities in conflict mode;
- meaningful-red, green-baseline, or per-assertion reversal evidence;
- exact commands, results, and intended tests discovered;
- coverage, branch-coverage, and other gate outcomes when applicable;
- dependencies or environments not exercised and residual risks; and
- any required production seam, blocking prerequisite, human decision, or human implementation boundary.

Keep review findings and test-remediation discussion inside the issue-owning task. Notify the Supervisor only through the Implementor's canonical `completed`, `decomposed`, `blocked`, or `human-action` signal.
