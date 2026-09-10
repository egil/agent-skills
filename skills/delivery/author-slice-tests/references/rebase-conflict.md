# Rebase conflict mode

1. Match the supplied pre-rebase head, target default-branch OID, current `HEAD`, replayed commit, progress, todo, complete index stages, worktree diff, and untracked inventory. Stop when any identity differs.
2. Resolve only unmerged test code, test fixtures, and test-project support owned by this slice. Preserve both upstream changes and the reviewed Verification or green-baseline contract.
3. Stage only resolved test-owned paths. Leave production-owned, mixed-ownership, and decision-bearing conflicts unresolved for the Implementor.
4. Run focused validation when the in-progress rebase permits meaningful execution; otherwise record why validation is unavailable.
5. Apply the repository's authoring self-review, reread every supplied identity and the full index, and prove no non-test state changed.

This mode is complete when every owned conflict is correctly staged, every unowned conflict remains untouched, the rebase itself has not advanced or aborted, and the Implementor receives all identities, resolved paths, remaining conflicts, decisions, and available evidence. The Implementor resumes the rebase and preserves the next checkpoint under the contract's exact-lease policy.
