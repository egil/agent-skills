# Local review findings

Use the consuming repository's configured path. This file is the durable, branch-local substitute for pull-request review comments before a pull request exists. It remains implementation evidence, not a Supervisor status channel.

## Ownership and lifecycle

- A Reviewer appends reviewer-authored metadata and findings only after revalidating the fixed snapshot. Earlier finding text is immutable.
- The Implementor verifies the write, then stages, commits, and pushes it before remediation. Only the Implementor updates disposition fields.
- Test-code remediation is delegated to a fresh Tester; the Implementor records the returned result.
- A fresh Reviewer appends another cycle after any production or test change. A clean review changes no file.
- An unchanged open concern keeps its original ID and is not copied into later cycles. A failed claimed resolution becomes a new finding which links the prior ID.
- Follow the consuming contract's retention policy. When the file is temporary, remove it before the final candidate review. When it is retained delivery evidence, include it in the final candidate.

## Document shape

Start the file with the issue and branch. Append one section per review attempt; never erase an earlier attempt.

````markdown
# Local review: issue <number or stable identifier>

- Issue: <URL or stable identifier>
- Branch: `<branch>`
- File policy: <retained evidence or temporary checkpoint>

## Review <monotonic sequence>: <test-contract or complete-change>

- Reviewed commit: `<full SHA>`
- Comparison base: `<full OID>`
- Behavior start: `<full SHA>`
- Reviewer task: `<task or run ID when available>`
- Snapshot: <clean, or exact index/worktree/untracked summary>

### Standards

#### LR-<sequence>-S001 [P1] Concise actionable title

- File: `path/to/file.ext`
- Lines: `42-47`
- Side: `RIGHT`
- Reviewed commit: `<full SHA>`
- Governing rule: `path/to/standards.md` or named rule

Explain the concrete problem, why it matters, and the evidence. Keep it usable as a GitHub pull-request review comment.

```suggestion
exact replacement text when a safe, local replacement is available
```

##### Implementor disposition

- Status: `open`
- Decision: `pending`
- Owner: `implementor` or `tester`
- Rationale: pending
- Resolution commit: pending
- Verification: pending

### Spec

<!-- Use LR-<sequence>-P001 and the same finding shape. -->
````

Use the repository's established priority convention. If none exists, use `P0` for release-blocking or destructive risk, `P1` for a high-impact correctness defect, `P2` for an ordinary actionable defect, and `P3` for a low-impact improvement that is still worth changing. Omit the suggestion block when no exact replacement is safe.

Locations use one-based inclusive lines against the reviewed commit. Use `RIGHT` for added or changed lines and `LEFT` for removed lines. For a contract or evidence finding rather than a code line, replace File, Lines, and Side with `Location: <precise document, section, command, or missing evidence>`.

The Implementor changes disposition Status from `open` to `closed` only when the Decision and evidence are complete. Allowed decisions are `accepted`, `rejected`, `superseded`, `split`, and `human-action`; `human-action` remains open until the decision is supplied. Record evidence rather than rewriting the finding. A resolution names the exact production or test commit and verification; a rejection names the governing repository or specification evidence. Resolve findings in file order within their axis unless dependency or safety requires a different order, and record that reason.
