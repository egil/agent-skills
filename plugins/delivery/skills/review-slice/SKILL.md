---
name: review-slice
description: Pin one fixed delivery snapshot, run independent Standards and Spec review axes over it, and aggregate their findings into a recoverable result receipt.
argument-hint: "<test-contract|complete-change>"
---

# Review a delivery slice

Mode: `$ARGUMENTS` (`test-contract` or `complete-change`; ask if absent).

You pin the snapshot and aggregate. The two axes are delegated to
`review-standards` and `review-spec`, which assess it independently. You do not
review the code yourself, and you do not remediate — findings return to whoever
invoked you.

## 1. Pin the whole local snapshot

A branch comparison alone omits work in progress. Capture all of it:

1. Resolve and record the immutable behavior-start SHA, the comparison-base OID,
   and the current `HEAD`. Inspect the comparison-base-to-`HEAD` commit list and
   three-dot diff. Never use the behavior-start SHA as the diff base after a
   rebase onto a newer default branch.
2. Inspect the index diff against `HEAD`.
3. Inspect the unstaged worktree diff against the index.
4. Inventory and read every relevant untracked file — ordinary diffs omit them.
5. Record branch and status. Do not stage anything to make review easier.

A reviewable candidate has committed implementation state with a clean index and
tracked worktree; ignored review artifacts are the only allowed local
difference. If implementation state is dirty, record an incomplete result and
return it for a checkpoint rather than issuing a verdict.

## 2. Resolve the artifact directory

Read `../../references/review-artifacts.md`. Resolve the predictable snapshot
directory from issue, mode, and full `HEAD`.

Validate every existing receipt against that exact issue, mode, `HEAD`, and
comparison base. **If a complete, valid `result.md` already exists for this
snapshot, return it and spawn nothing.** Otherwise preserve stale or partial
files as recovery evidence and commission only the missing axes.

Create or validate `request.md` with `in-progress` status before delegating. For
an approved no-test exception the invoker writes `verification.md`; otherwise
require the tester's matching receipt first.

## 3. Run the two axes independently

Launch `review-standards` and `review-spec` in the same message so they run
concurrently. Give each the complete pinned snapshot — behavior-start SHA,
comparison base, `HEAD`, commit list, local-state evidence, Agent Brief,
verification contract, artifact directory, and declared residual risks.

Do not let either agent use worktree isolation. They must read the same
worktree you pinned; an isolated copy is a different snapshot, and the ignored
review artifacts would not survive it.

Assign `standards.md` to the Standards axis and `spec.md` to the Spec axis, and
nothing else. Never show one axis the other's findings or ask either to rank
against the other.

The Standards axis gets the repository's standards sources and quality baseline.
The Spec axis gets the Agent Brief, acceptance boundary, verification or
approved green-baseline contract, and dependency context.

Read `../../references/complete-change.md` or `../../references/test-contract.md` for the
mode's criteria and pass the relevant one to both axes.

A behavior-preserving `test-contract` review that adds no characterization code
is the one case where Standards may report not-applicable; treat that N/A as
satisfied and still run an independent Spec assessment. An empty
`complete-change` diff is not a deliverable.

## 4. Aggregate

When both axes return, re-check `HEAD`, branch, index, worktree, and untracked
inventory against the pinned state. If anything differs, invalidate the result
and require a fresh review of the new snapshot — an axis completing is not a
clean verdict.

Validate both axis files against the request and snapshot. Write `result.md`
with `complete` status **only** when both axes returned, both artifacts validate
against the exact snapshot, and every finding is recorded. Otherwise write it
with `incomplete` status naming exactly which axis is missing or invalid and why.

An `incomplete` result is recovery evidence, never a verdict. A later run must
be able to tell the two apart from the file alone — a blocked or non-returning
axis must never leave behind a receipt that reads as clean.

Return the behavior-start SHA, comparison base, `HEAD`, local-state summary,
mode, artifact directory and file states, and the two reports under separate
`## Standards` and `## Spec` headings. End with counts and the worst finding in
each axis, without choosing a winner across them.

A clean verdict requires both axes clean, no open finding for this snapshot, a
valid complete `result.md`, and an end-of-review snapshot matching the pinned
one — with the green-baseline Standards N/A as the sole exception.

Do not stage, commit, push, or begin remediation. Because the artifact root is
ignored, recording the verdict does not change the reviewed Git state.
