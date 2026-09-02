---
name: author-slice-tests
description: Author and validate one small GitHub delivery slice's high-value tests or resolve its test-owned rebase conflicts in a bounded mode. Use only when an issue-owning Implementor delegates test work; do not use for production implementation or code review.
---

# Author slice tests

Own test code for one small delivery slice without owning its production implementation. Work as a short-lived Codex collaboration task in one of four bounded modes against an exact snapshot in the slice's existing worktree and natively linked branch:

- **Red contract:** before production behavior is implemented, author sufficient high-value tests and prove the requested behavior is absent for the intended reason.
- **Green baseline:** before behavior-preserving production work, establish sufficient green characterization evidence without manufacturing a failure.
- **Green finalization:** after production work, make approved test code correct, prove every new or materially changed assertion is sensitive, and verify required quality gates.
- **Rebase conflict:** while the Implementor controls an in-progress rebase, resolve and stage only test-owned conflict paths without changing production code or advancing the rebase.

## Load the consuming-repository contract

The invoking Implementor must supply the delivery contract or its discoverable location. It must define the GitHub identity and mutation rules, native branch and worktree conventions, commit and push policy, default branch, test commands and required gates, Agent Brief and Verification contract format, and the upward status-signal route. If required values are absent or contradictory, report `blocked` or `human-action` to the Implementor; do not infer them.

## Dependencies

- Required internal: `$design-high-value-tests` for test-design judgment, `$verification-driven-delivery` for fidelity to the issue's Verification or approved green-baseline contract, and `$development-session-observability` for sparse markers when the Implementor supplies its command.
- This role may receive findings from the internal `$review-delivery-slice` role; it never replaces that independent review.

The Implementor must pass an available Codex model identifier and reasoning setting for this bounded assignment. The role has no model default and must not silently substitute one. The owner should select them from issue risk and record the rationale.

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
- the contract-designated local-review path and the specific test-owned finding IDs, when remediation was delegated;
- relevant prior review findings or approved contract changes; and
- checkpoint and push authority inherited from the issue-delivery task.

Verify expected branch and exact snapshot. Do not overwrite or silently include another agent's uncommitted work. If the handoff is dirty or points at the wrong revision, return it to the Implementor for reconciliation. The sole exception is `rebase-conflict` mode, whose handoff must identify the expected rebase, pre-rebase head, target default-branch OID, current `HEAD`, replayed commit, rebase progress and remaining todo, complete index-stage state, worktree diff, and untracked inventory.

## Red contract mode

1. State each protected behavior as `When <observable scenario>, the system <observable outcome>`. Map its risk, observation seam, dependency fidelity, and narrowest sufficient test level.
2. Inspect relevant repository test shapes and design the smallest portfolio covering every material risk in the slice. Do not repeat assertions mechanically across levels.
3. Author tests against the approved public behavior. A correct test may use a minimal compilable production shell supplied by the Implementor, but the shell must not implement the behavior.
4. Run every intended test at the starting revision. Confirm discovery, reach the intended behavior, and fail because the expected observable outcome is absent. Compilation errors, broken setup, unavailable infrastructure, earlier failures, and zero discovered tests are not meaningful red.
5. If no compilable observation seam exists, request the smallest behavior-free shell from the Implementor and stop. If the expected behavior is already green at the starting revision, report a stale contract or baseline rather than manufacturing a failure.
6. Leave correct expected assertions in place. Tests may remain meaningfully red because production is pending; never retain a deliberately wrong assertion or injected production fault.
7. Apply the consuming repository's authoring self-review. This does not replace independent Reviewer review.
8. Commit and push the completed test checkpoint to the existing linked branch. Follow its commit convention; the message body must preserve objective, starting SHA, test-design decisions, relevant session context, rejected paths or discoveries, exact red command and result, unexercised dependencies, and residual risk. No pull request may be created until local work and review are complete.
9. Return checkpoint SHA and evidence to the Implementor. The Implementor commissions bounded test-contract review before production work begins.

## Green baseline mode

1. State each behavior or contract to preserve and map risk, observation seam, dependency fidelity, and sufficient test level.
2. Inspect existing tests at the immutable behavior-start SHA. Add only the smallest characterization tests needed for material preservation risks.
3. Run intended baseline tests at that SHA. Require discovery and a green result at approved fidelity; do not manufacture red for behavior that already exists.
4. For every new or materially changed assertion, deliberately reverse it or apply an equivalent controlled behavioral reversal, observe the intended test fail for that assertion's reason, restore it, and observe green. Never commit the reversal.
5. Run focused and broader affected baseline suites and applicable contract-defined quality gates. Record unexercised dependencies and residual risk rather than treating unavailable evidence as passing.
6. Apply the consuming repository's authoring self-review. If test code changed, commit and push a checkpoint whose body preserves objective, immutable start SHA, decisions and context, changes, inversion and green evidence, exact commands, and residual risk. If nothing changed, create no empty commit and report the verified start SHA.
7. Return baseline evidence to the Implementor for bounded test-contract review before production work begins.

## Green finalization mode

1. Start from the exact committed production snapshot supplied by the Implementor. Run focused tests before editing and classify failures as production behavior, test mechanics, environment, or stale contract.
2. Correct test code and test-only support within the approved contract. Return production failures or required production seams to the Implementor.
3. For every new or materially changed assertion, deliberately reverse it or apply an equivalent controlled behavioral reversal, run the intended test, and observe it fail for the protected behavioral reason. Restore the assertion and observe green before moving on. Do not commit a reversal or production fault.
4. Run the smallest relevant tests during iteration, then broader affected verification and the repository quality entry point required by the contract. Confirm discovery and enforce all applicable coverage, branch-coverage, analyzer, and other gates.
5. Record dependencies and environments not exercised as unavailable or residual risk, never passing evidence. Infrastructure behavior may omit new automated tests only when no deterministic automated boundary faithfully exercises the changed risk and the issue records alternative verification and residual risk; difficulty or slowness alone is insufficient.
6. Apply the consuming repository's authoring self-review. If test changes were needed, commit and push them with a context-preserving message; if no test files changed, create no empty checkpoint and report the exact verified SHA.
7. Return control to the Implementor for independent complete-change review. Do not create a pull request or represent your own work as independently reviewed.

Use green finalization for later test-only remediation, including an accepted automated GitHub review suggestion. Synchronize any GitHub-applied suggestion locally, verify the resulting exact commit, and return it for fresh independent local review before delivery continues.

For a local-review finding, work on only the delegated finding ID and preserve Reviewer-authored text. Return the exact test commit and verification evidence to the Implementor, which owns the finding's disposition and the review file.

## Rebase conflict mode

1. Require the rebase to originate from the supplied pre-rebase head and target default-branch OID. Match current `HEAD`, replayed commit, progress and todo, complete index-stage listing, worktree diff, and untracked inventory; stop if any identity differs.
2. Resolve only unmerged test code, test-only fixtures, and test-project support owned by this slice. Preserve the reviewed Verification or green-baseline contract and combine upstream changes with intended test behavior; never choose a side mechanically when it discards either contract.
3. Stage only resolved test-owned paths. Do not edit or stage production paths, continue or abort the rebase, create a commit, push, or perform unrelated cleanup. If a file mixes production and test ownership or requires a product or contract decision, leave it unresolved and return the evidence.
4. Run focused validation only when the in-progress rebase permits meaningful execution. Otherwise state why execution is unavailable; full gates run after the Implementor completes the rebase.
5. Apply the consuming repository's authoring self-review to the conflict resolution. Before reporting, reread all supplied identities and the full index and prove no non-test state changed. Return identities, resolved and staged paths, remaining conflicts, decisions, and available evidence. The Implementor resumes the rebase and preserves its checkpoint under the contract's exact-lease policy. A later test conflict receives a new bounded handoff.

## Report and stop

Report to the Implementor:

- protected behavior and chosen test levels;
- exact base and resulting checkpoint SHAs, or all supplied rebase and index identities in conflict mode;
- meaningful-red, green-baseline, or per-assertion reversal evidence;
- exact commands, results, and intended tests discovered;
- coverage, branch-coverage, and other gate outcomes when applicable;
- dependencies or environments not exercised and residual risks; and
- any required production seam, blocking prerequisite, human decision, or human implementation boundary.

Keep review findings and test-remediation discussion inside the issue-owning task. Notify the Supervisor only through the Implementor's canonical `completed`, `decomposed`, `blocked`, or `human-action` signal.
