---
name: review-delivery-slice
description: Perform a bounded, code-read-only review of one GitHub delivery slice's test contract or complete local change, persisting actionable findings in the configured branch-local Markdown file. Invoke only when an issue-owning Implementor delegates a fixed worktree snapshot.
---

# Review a delivery slice

Review one fixed snapshot in the Implementor's worktree, persist actionable findings for that Implementor, and finish. Remain read-only for production code, test code, and Git state. The sole write allowed is the contract-designated local-review Markdown file after the reviewed snapshot has been revalidated. Never stage, commit, push, mutate the issue or pull request, resolve review threads, or merge. The Implementor or Tester owns remediation, and a fresh Reviewer examines each resulting snapshot.

## Load the consuming-repository contract

The Implementor must supply the delivery contract or its discoverable location. It must define the GitHub repository and identity, native branch and worktree rules, default branch and comparison-base semantics, Agent Brief and Verification contract, repository standards sources, test and quality gates, local-review file path and retention policy, review-thread policy, and upward status-signal route. If specification, branch identity, comparison base, review-file location, or required contract is absent or contradictory, report that the snapshot cannot pass review; do not invent authority.

## Dependencies

- External required companion: `$code-review` from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills), preserving its independent Standards and Spec axes.
- Internal required companions: `$design-high-value-tests` for test-design judgment, `$verification-driven-delivery` for the issue's Verification or approved green-baseline contract, and `$development-session-observability` for sparse markers when the Implementor supplies its command.

The invoking owner must pass an available Codex model identifier and reasoning setting for this bounded review. The role has no model default and must not silently substitute one. Standards and Spec sub-reviews are separate bounded spawns: pass each an explicit model and reasoning selected for the issue's risk and provide the complete snapshot and rationale.

When a marker command is supplied, load `$development-session-observability` and emit only material review phases and quality results. Do not create an evidence ledger, put findings or source content in markers, or estimate unavailable usage.

## Require a complete review brief

The Implementor supplies:

- mode: `test-contract` or `complete-change`;
- issue and authoritative Agent Brief;
- Verification contract for behavior-changing work, or approved green-baseline contract for behavior-preserving work;
- immutable behavior-start SHA used for red or baseline provenance;
- current comparison-base OID used to isolate this slice from intervening default-branch work;
- worktree and branch, current `HEAD`, commits, verification evidence, and declared residual risks;
- local-review sequence and the contract-designated Markdown path; and
- any proposed infrastructure no-test exception and its alternative evidence.

Recover omitted read-only facts from the repository and GitHub only when unambiguous. If the specification, behavior-start SHA, comparison base, or required contract is missing or contradictory, return a blocked review result rather than filling the gap.

## Pin the whole local snapshot

A branch comparison alone omits work in progress. Before reviewing, capture and give both axes the complete change:

1. Resolve and record immutable behavior-start SHA, comparison-base OID, and current `HEAD`. Inspect the comparison-base-to-`HEAD` commit list and three-dot diff; never use the old behavior-start SHA as the diff base after rebasing onto newer default branch.
2. Inspect the index diff against `HEAD`.
3. Inspect the unstaged worktree diff against the index.
4. Inventory and read every relevant untracked file, which ordinary Git diffs omit. Record the designated local-review file separately from implementation content.
5. Record initial branch and status. Do not stage files merely to make review easier.

The Implementor must not edit concurrently. Re-check `HEAD`, branch, index, worktree, untracked inventory, and any pre-existing local-review file before recording findings. If any differs from the pinned state, invalidate the result and request a fresh review of the new snapshot. After that successful re-check, the Reviewer may change only the designated file; record its before and after state so this expected evidence write cannot conceal another snapshot change.

## Run the two axes independently

Delegate Standards and Spec in parallel as `$code-review` requires, passing each the full pinned snapshot rather than only a comparison-base range. Use collaboration tasks with no inherited conversation and include every required source in each prompt. Pass explicit model and reasoning to each spawn; do not rely on role or host defaults.

The `$code-review` workflow normally requires a non-empty diff. The only exception is a `test-contract` review of a behavior-preserving green baseline that adds no characterization code: report Standards as not applicable, treat that N/A as satisfied for this exception, and run an independent Spec assessment of the Agent Brief, approved baseline contract, commands, discovery, fidelity, and residual risk. An empty `complete-change` diff is not a deliverable.

The Standards axis receives repository standards sources and the contract-defined quality baseline. It applies ecosystem-reuse, simplicity, consistent-level-of-abstraction, testing, and commit-history rules. Numeric coverage, branch coverage, analyzer, or workflow success never replaces judgment about correctness and test value.

The Spec axis receives the complete Agent Brief, issue acceptance boundary, Verification or approved green-baseline contract, dependency context, and relevant evidence. It checks missing or partial behavior, scope growth, incorrect behavior, and whether promised verification is present at the approved seam and fidelity.

Treat missing or inaccurate evidence in the axis which owns it. Do not create a third testing axis or rerank the two companion reports.

## Review the requested mode

### `test-contract`

Review the executable contract before production behavior is implemented. Production being pending is expected and is not a finding. For behavior-changing work, inspect proposed tests, any minimal compilable behavior-free shell, test-design decisions, and meaningful-red evidence. Require that:

- each retained test protects an observable behavior owned by this one slice;
- the chosen level is the narrowest boundary that faithfully retains the named risk;
- observations avoid implementation details and unnecessary collaborator choreography;
- meaningful red reaches the intended behavior and fails for the expected reason rather than compilation, setup, missing infrastructure, an earlier assertion, or zero discovery;
- the test portfolio covers material risks without mechanically repeating the same assertion at several levels; and
- every new or materially changed assertion has a credible deliberate-inversion plan and contract-defined gates remain achievable with high-value tests.

For behavior-preserving work, inspect the preservation boundary, characterization portfolio, green discovery evidence at immutable behavior-start SHA, controlled inversion evidence for every new or materially changed assertion, and concrete residual risk. Never demand manufactured red.

A clean result means the tests and evidence form a valid executable contract; it does not mean the issue is implemented or ready to publish.

### `complete-change`

Review all production and test changes, including any infrastructure exception, against the current comparison base. Require evidence that:

- reviewed tests remained Tester-owned and the implementation satisfies them without weakening the contract;
- meaningful red or an approved green baseline was established when applicable, every new or materially changed assertion received deliberate-inversion failure, all assertions were restored green, and evidence was reported accurately;
- an infrastructure no-test decision truly lacks a deterministic faithful automated boundary, records concrete alternative verification and residual risk, and waives no contract-defined gate;
- focused and broader affected verification discovered and executed intended tests;
- required build, coverage, branch-coverage, analyzer, and other quality gates pass without low-value metric-filler tests;
- dependency and boundary fidelity match the contract and unexercised environments are named;
- the complete solution and each meaningful changed component are as simple as behavior, compatibility, operability, and approved seams permit; and
- the slice is independently mergeable, safe in the default branch, and contains no hidden follow-on requirement needed to make it valid.

If a finding reveals substantial redesign, new behavior, or another independently mergeable slice, identify the scope boundary so the Implementor can stop remediation and return it to planning.

## Persist actionable findings

When either axis has an actionable finding, read [references/review-findings.md](references/review-findings.md) and append one review cycle to the contract-designated Markdown file. Preserve earlier reviewer text and Implementor dispositions. Reconcile existing open findings: do not duplicate an unchanged open concern, and append a follow-up finding only when new evidence creates a distinct issue or a claimed resolution remains defective. Give every new finding a stable ID, keep Standards and Spec in separate sections, and use the GitHub review-comment shape: priority and concise title, repository-relative file with one-based line or range and diff side, reviewed commit, evidence-backed explanation, and an optional fenced `suggestion` containing an exact replacement.

Write one independently actionable concern per finding. Order findings within each axis from highest to lowest priority without ranking one axis against the other. Never put prompts, reasoning, secrets, unrelated source, or general implementation chatter in the file.

The Reviewer owns finding text; the Implementor owns disposition fields. Do not stage, commit, push, or begin remediation. When both axes are clean, leave the file unchanged and return a clean receipt, so recording the verdict does not change the exact reviewed `HEAD`.

## Report only to the Implementor

Return the immutable behavior-start SHA, pinned comparison-base OID, `HEAD`, local-state summary, mode, local-review path and resulting file state, and the two reports under separate `## Standards` and `## Spec` headings. Cite the exact file and hunk or contract/evidence location plus governing standards or specification evidence for every actionable finding. State explicitly when an axis is clean or unavailable.

End with counts and the worst finding within each axis, without choosing a winner across axes. A clean verdict requires both axes clean, no open finding in the local-review file, and the end-of-review snapshot matching the pinned snapshot, except that the explicitly allowed green-baseline Standards N/A counts as satisfied.

Send this result and the local-review path only to the issue-owning Implementor. Do not forward findings, test details, or remediation discussion to a parent or top-level Supervisor. If a finding exposes a human decision or human implementation boundary, record that finding and tell the Implementor to escalate it through the Supervisor. Then end the Reviewer assignment.
