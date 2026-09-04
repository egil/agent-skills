---
name: review-delivery-slice
description: Review one fixed delivery-slice snapshot on independent Standards and Spec axes with recoverable local artifacts.
---

# Review a delivery slice

Review one fixed snapshot in the Implementor's worktree, persist actionable findings for that Implementor, and finish. Remain read-only for production code, test code, and Git state. The only writes allowed are the predictable local review artifacts defined by `$delivery-runtime-protocol`. Never stage, commit, push, mutate the issue or pull request, resolve review threads, or merge. The Implementor or Tester owns remediation, and a fresh Reviewer examines each resulting snapshot.

## Load the consuming-repository contract

The Implementor must supply the delivery contract or its discoverable location. It must define the GitHub repository and identity, native branch and worktree rules, default branch and comparison-base semantics, Agent Brief and Verification contract, repository standards sources, test and quality gates, review-thread policy, and upward status-signal route. A missing tracked repository review path is not a contract gap and must not block delivery. If specification, branch identity, comparison base, or another required contract is absent or contradictory, report that the snapshot cannot pass review; do not invent authority.

## Dependencies

- External required companion: `$code-review` from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills), preserving its independent Standards and Spec axes.
- Internal required companions: `$design-high-value-tests` for test-design judgment, `$verification-driven-delivery` for the issue's Verification or approved green-baseline contract, `$delivery-runtime-protocol` for exact-snapshot artifact recovery, and `$development-session-observability` for sparse markers when the Implementor supplies its command.

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
- issue number, review mode, and full current `HEAD`, which together determine the predictable local artifact path; and
- any proposed infrastructure no-test exception and its alternative evidence.

Recover omitted read-only facts from the repository and GitHub only when unambiguous. If the specification, behavior-start SHA, comparison base, or required contract is missing or contradictory, return a blocked review result rather than filling the gap.

Load `$delivery-runtime-protocol` and apply its local-review-artifact branch. Verify the Implementor's ignore setup without changing it; when the path is not ignored, return that ordinary setup correction before writing. Validate current local state and every existing receipt against the exact issue, mode, `HEAD`, and comparison base. Return an already-complete valid result without duplicating review. Preserve stale or partial files as recovery evidence and resume only missing work.

## Pin the whole local snapshot

A branch comparison alone omits work in progress. Before reviewing, capture and give both axes the complete change:

1. Resolve and record immutable behavior-start SHA, comparison-base OID, and current `HEAD`. Inspect the comparison-base-to-`HEAD` commit list and three-dot diff; never use the old behavior-start SHA as the diff base after rebasing onto newer default branch.
2. Inspect the index diff against `HEAD`.
3. Inspect the unstaged worktree diff against the index.
4. Inventory and read every relevant untracked file, which ordinary Git diffs omit. Treat the ignored local review directory as recovery evidence rather than implementation content.
5. Record initial branch and status. Do not stage files merely to make review easier.

The Implementor must not edit concurrently. Re-check `HEAD`, branch, index, worktree, and untracked implementation inventory before recording findings. If any differs from the pinned state, invalidate the result and request a fresh review of the new snapshot. A reusable verdict requires committed implementation state with a clean index, tracked worktree, and relevant untracked inventory; ignored protocol artifacts are the only allowed local difference. When implementation state is dirty, record an incomplete result and return it for an Implementor-owned checkpoint rather than issuing a clean verdict. Record protocol-artifact state before and after review so those writes cannot conceal another change.

## Run the two axes independently

Before delegation, require and validate `request.md` with `in-progress` status and the exact snapshot identities. Delegate Standards and Spec in parallel as `$code-review` requires, passing each the full pinned snapshot rather than only a comparison-base range. Use collaboration tasks with no inherited conversation and include every required source in each prompt. Pass explicit model and reasoning to each spawn; do not rely on role or host defaults. Assign only `standards.md` to the Standards reviewer and only `spec.md` to the Spec reviewer. Each axis creates its file with `in-progress` status after initial snapshot validation, records confirmed findings promptly, and marks it `complete` only after end-of-review revalidation. Reuse a valid completed axis artifact after interruption and rerun only missing or invalid axes.

The `$code-review` workflow normally requires a non-empty diff. The only exception is a `test-contract` review of a behavior-preserving green baseline that adds no characterization code: report Standards as not applicable, treat that N/A as satisfied for this exception, and run an independent Spec assessment of the Agent Brief, approved baseline contract, commands, discovery, fidelity, and residual risk. An empty `complete-change` diff is not a deliverable.

The Standards axis receives repository standards sources and the contract-defined quality baseline. It applies ecosystem-reuse, simplicity, consistent-level-of-abstraction, testing, and commit-history rules. Numeric coverage, branch coverage, analyzer, or workflow success never replaces judgment about correctness and test value.

The Spec axis receives the complete Agent Brief, issue acceptance boundary, Verification or approved green-baseline contract, dependency context, and relevant evidence. It checks missing or partial behavior, scope growth, incorrect behavior, and whether promised verification is present at the approved seam and fidelity.

Treat missing or inaccurate evidence in the axis which owns it. Do not create a third testing axis or rerank the two companion reports.

## Apply the selected review mode

Read only the procedure matching the requested mode:

- [`test-contract`](references/test-contract.md) for the executable test contract before production work;
- [`complete-change`](references/complete-change.md) for the final production-and-test snapshot.

The selected procedure supplies that mode's review criteria; the independent axes and exact-snapshot receipt remain governed here.

## Persist axis results

Persist each axis report in its assigned artifact whether clean or finding-bearing. Preserve earlier reviewer text and Implementor dispositions. Reconcile existing open findings: do not duplicate an unchanged open concern, and add a follow-up finding only when new evidence creates a distinct issue or a claimed resolution remains defective. Give every new finding a stable ID, keep Standards and Spec in separate files, and use the GitHub review-comment shape defined by the artifact protocol: priority and concise title, repository-relative file with one-based line or range and diff side, reviewed commit, evidence-backed explanation, and an optional fenced `suggestion` containing an exact replacement.

Write one independently actionable concern per finding. Order findings within each axis from highest to lowest priority without ranking one axis against the other. Never put prompts, reasoning, secrets, unrelated source, or general implementation chatter in the file.

The Reviewer owns finding text and `result.md`; the Implementor owns `dispositions.md`. After validating both independent axis artifacts, write `result.md` with a `complete` status, exact identities, per-axis counts, and the clean or finding-bearing verdict. Do not stage, commit, push, or begin remediation. Because the artifact root is ignored, recording the verdict does not change the reviewed Git state.

## Report only to the Implementor

Return the immutable behavior-start SHA, pinned comparison-base OID, `HEAD`, local-state summary, mode, snapshot artifact directory and file states, and the two reports under separate `## Standards` and `## Spec` headings. Cite the exact file and hunk or contract/evidence location plus governing standards or specification evidence for every actionable finding. State explicitly when an axis is clean or unavailable.

End with counts and the worst finding within each axis, without choosing a winner across axes. A clean verdict requires both axes clean, no open finding for this snapshot, a valid complete `result.md`, and the end-of-review snapshot matching the pinned snapshot, except that the explicitly allowed green-baseline Standards N/A counts as satisfied.

Send this result and the snapshot artifact directory only to the issue-owning Implementor. Do not forward findings, test details, or remediation discussion to a parent or top-level Supervisor. If a finding exposes a human decision or human implementation boundary, record that finding and tell the Implementor to escalate it through the Supervisor. Then end the Reviewer assignment.
