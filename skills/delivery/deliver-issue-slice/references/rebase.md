# Rebase before merge

Dependent work begins only after blockers merge into the default branch. Pull-request stacks require separate authorization.

Immediately before merge, refresh remote state and require the pull request to be linked, review-clean, check-clean, and permitted by branch protections. Rebase onto the current default branch when the contract requires it and record that OID as the new comparison base.

For a conflicted rebase:

1. Record the pre-rebase head, target OID, current `HEAD`, replayed commit, progress, todo, full index stages, worktree diff, and untracked inventory.
2. Resolve and stage production-owned conflicts. For test-owned conflicts, launch a Tester in `rebase-conflict` mode with that exact state and leave Git, index, and worktree untouched while it runs.
3. Validate every identity and the full index against the Tester's receipt before continuing.
4. Leave mixed-ownership or decision-bearing conflicts unresolved. Preserve evidence, abort only as the contract authorizes, verify the published branch is unchanged, and report `blocked` or `human-action`.
5. After resolution, finish the rebase, apply owner self-review, preserve the recovery checkpoint with an exact lease, and rerun applicable gates plus complete-change review.

Every rebase that changes `HEAD` invalidates the old snapshot verdict. Rerun applicable verification, Tester green finalization when tests changed, self-review, complete-change review, exact-lease push, workflows, and configured GitHub review.

Immediately before the merge call, require the remote default-branch OID to equal the reviewed comparison base and the pull-request head to equal the locally reviewed and verified SHA. When default advances, repeat the rebase and all invalidated evidence. Use the contract's merge command with an exact head-match guard.

The rebase phase is complete only when the current remote head and default base exactly match the reviewed identities and every invalidated gate has fresh evidence.
