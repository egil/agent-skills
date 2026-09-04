# Local review artifacts

Store temporary review handoffs in the issue worktree at:

```text
artifacts/reviews/issue-<number>/<mode>/<full-head-sha>/
|-- request.md
|-- verification.md
|-- standards.md
|-- spec.md
|-- result.md
`-- dispositions.md
```

`<mode>` is `test-contract` or `complete-change`. The issue, mode, and full reviewed `HEAD` make the directory predictable: an Implementor checks this exact directory before commissioning a Reviewer and resumes usable work instead of duplicating it. A reusable candidate has committed implementation state with a clean index, tracked worktree, and relevant untracked inventory; ignored protocol artifacts are the only allowed local difference. This invariant makes the full `HEAD` a complete implementation-snapshot key.

Before the first Tester or Reviewer handoff, the Implementor verifies a representative path with `git check-ignore -v`. When no existing rule covers it, the Implementor adds `/artifacts/reviews/` to the repository-local exclude file resolved by `git rev-parse --git-path info/exclude`. It does not change a tracked ignore file solely for these artifacts. Tester and Reviewer roles verify that setup but never modify ignore configuration; a failed check returns an ordinary setup correction to the Implementor rather than a human decision.

Retain the directory at least until the pull request is ready to merge. Never stage, commit, push, stash, or remove it as cleanup during delivery. A missing tracked repository review path never blocks delivery. Missing local artifacts mean create or resume the required work; they are neither a blocker nor evidence that review passed.

These files are crash-recovery state and implementation evidence, not a Supervisor status channel. Each file records the issue, mode, full `HEAD`, comparison base, writer task ID when available, status, and write time. Write the file owned by the current role as soon as its result is known so an interruption loses at most the active step.

## Ownership and lifecycle

- The Tester writes `verification.md` for `test-contract` and `complete-change` handoffs once the exact resulting `HEAD` is known. The Implementor writes it only for an approved no-test exception.
- The Implementor creates or verifies `request.md` before dispatch. It records the clean committed snapshot, points to `verification.md`, and uses `in-progress` status without claiming a review result.
- The Standards and Spec reviewers create only `standards.md` and `spec.md`, respectively, with `in-progress` status after initial snapshot validation. They record confirmed findings promptly and mark the file `complete` only after end-of-review revalidation. Earlier finding text is immutable.
- The aggregating Reviewer writes `result.md` only after validating both axis files against the request and snapshot. It marks the result `complete` or records exactly what remains incomplete.
- The Implementor writes only `dispositions.md`, preserving stable finding IDs and exact remediation commits and verification evidence.
- Test-code remediation is delegated to a fresh Tester; the Implementor records the returned result.
- A production or test change has a different full `HEAD` and therefore a different snapshot directory. A clean review still produces a complete `result.md` recovery receipt.
- An unchanged open concern keeps its original ID and is not copied into later cycles. A failed claimed resolution becomes a new finding which links the prior ID.
- Before dispatch or resume, revalidate the current branch, `HEAD`, clean implementation state, ignored-artifact inventory, and file contents. Reuse a verification or axis file only when its issue, mode, full `HEAD`, comparison base, status, and writer ownership match. Reuse `result.md` only when `request.md` is valid and verification plus both independent axes are complete for that exact snapshot. Validate `dispositions.md` when findings make it applicable. A dirty, stale, contradictory, partial, or malformed state is recovery evidence, not a clean verdict; preserve its artifacts and resume or rerun the missing work.

## Document shape

Use the same metadata header in every file. Put Reviewer findings in the relevant axis file and Implementor decisions in `dispositions.md`; never rewrite Reviewer-authored findings.

````markdown
# <Request, Verification, Standards review, Spec review, Result, or Dispositions>: issue <number>

- Issue: <URL or stable identifier>
- Mode: `test-contract` or `complete-change`
- Branch: `<branch>`
- Reviewed commit: `<full SHA>`
- Comparison base: `<full OID>`
- Writer task: `<task or run ID when available>`
- Status: `in-progress`, `complete`, or `incomplete`
- Written at: `<ISO-8601 UTC timestamp>`

## Review

- Behavior start: `<full SHA>`
- Snapshot: <clean committed implementation state and ignored-artifact inventory>

### Standards

#### LR-<reviewed-sha-prefix>-S001 [P1] Concise actionable title

- File: `path/to/file.ext`
- Lines: `42-47`
- Side: `RIGHT`
- Reviewed commit: `<full SHA>`
- Governing rule: `path/to/standards.md` or named rule

Explain the concrete problem, why it matters, and the evidence. Keep it usable as a GitHub pull-request review comment.

```suggestion
exact replacement text when a safe, local replacement is available
```

### Spec

<!-- In spec.md, use LR-<reviewed-sha-prefix>-P001 and the same finding shape. -->
````

Use the repository's established priority convention. If none exists, use `P0` for release-blocking or destructive risk, `P1` for a high-impact correctness defect, `P2` for an ordinary actionable defect, and `P3` for a low-impact improvement that is still worth changing. Omit the suggestion block when no exact replacement is safe.

Locations use one-based inclusive lines against the reviewed commit. Use `RIGHT` for added or changed lines and `LEFT` for removed lines. For a contract or evidence finding rather than a code line, replace File, Lines, and Side with `Location: <precise document, section, command, or missing evidence>`.

In `dispositions.md`, the Implementor changes a finding's status from `open` to `closed` only when the decision and evidence are complete. Allowed decisions are `accepted`, `rejected`, `superseded`, `split`, and `human-action`; `human-action` remains open until the decision is supplied. Record evidence rather than rewriting the finding. A resolution names the exact production or test commit and verification; a rejection names the governing repository or specification evidence. Resolve findings in file order within their axis unless dependency or safety requires a different order, and record that reason.
