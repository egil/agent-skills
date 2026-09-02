---
name: deliver-issue-slice
description: Own one small GitHub issue from its natively linked branch through independent testing and review, publication, configured GitHub review automation, and an allowed merge. Invoke only for an explicitly assigned delivery slice.
---

# Deliver an issue slice

Own one small, independently mergeable vertical slice through merge. Coordinate the Tester and Reviewer, but do not replace either role or perform the formal independent review yourself. The Supervisor never participates in review of changed code: findings and remediation stay between this Implementor, the Tester, and the Reviewer.

## Load the consuming-repository contract

The assignment must identify or point to a delivery contract discoverable through applicable `AGENTS.md`. It must define the GitHub host, repository, authorized identity, issue and pull-request relationships, native issue-linked branch and worktree rules, default branch, branch naming, commit and push policy, protected-branch and merge rules, Agent Brief and Verification contract, test/build/quality gates, a durable phase-checkpoint format and location, local-review Markdown path and retention policy, required workflow and automated-review completion evidence and bounded wait policies, and the upward notification route. It must state which issue, branch, pull-request, and merge mutations are authorized and keep deployment or release outside this skill unless explicitly included.

If the contract or assignment omits a required value, stop before the affected mutation and report `blocked` or `human-action`; do not invent a project, account, branch prefix, default branch, label, merge strategy, or review service.

## Dependencies and model selection

- Internal: `$orchestrate-milestone-delivery` for linked-branch, provisioning, model-routing, and project-state protocol; `$plan-delivery-slices`, `$author-slice-tests`, `$review-delivery-slice`, `$design-high-value-tests`, `$verification-driven-delivery`, and `$development-session-observability`.
- External companion skills from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills): `$to-tickets` when planning is required and `$code-review` through the Reviewer.

The owner that creates each bounded Planner, Tester, or Reviewer task must select and pass both an exact Codex model identifier and reasoning setting. Load `$orchestrate-milestone-delivery`'s Codex routing table and use issue evidence; role skills have no fixed default and must not silently substitute another pair. For an Implementor that becomes a parent Supervisor, the same selection rule applies to every child. Record classification, pair, rationale, and any deviation in the handoff or available session telemetry.

Load `$development-session-observability` and inherit the Supervisor's stable run ID and issue work item. When supplied, use the marker command only for material semantic transitions and pass it to bounded children. Do not create ledgers, summarize telemetry, or duplicate Codex-native usage. Telemetry must not delay delivery or cross the review-reporting boundary.

Reroute only at a clean bounded boundary. Circuit-break after the evidence-based threshold in `$orchestrate-milestone-delivery`: preserve and push the recoverable checkpoint, then report `blocked` or `human-action` rather than repeatedly retrying the same failing assignment. Never change a running task's model invisibly.

An explicit assignment to deliver issue `#N` through merge authorizes only the ordinary operations required by the contract: work on the issue's own linked branch and worktree, commit and push checkpoints, publish and update its pull request, rebase its own branch with exact lease protection, reply to review, and perform the permitted merge once every gate is clean. It does not authorize deployment, release, bypassing protections, mutating another issue's branch, creating a pull-request stack, destructive cleanup, or unrelated tracker work.

Before a GitHub mutation, verify the contract-defined identity with `gh auth status --hostname <github-host>` and `gh api --hostname <github-host> user --jq .login`. Do not change Git credentials, request or reveal a token, or continue after incomplete or inconsistent identity evidence.

## Recover durable state

GitHub and the remote branch are the recovery record, not an agent's context.

1. Re-read the issue, Agent Brief, Verification or green-baseline contract, milestone or scope, native parent and dependency relationships, assignee, linked branch or pull request, durable phase checkpoints, contract-designated local-review file, checks, and review threads. Paginate every relationship and review-thread connection and reconcile reported counts; a successful default-sized page is not complete evidence.
2. Record worktree path, branch or detached state, `HEAD`, upstream, remote branch head, index state, worktree state, and untracked inventory before changing anything.
3. Fetch the exact linked remote ref and inspect `git worktree list --porcelain`. If this worktree is detached, attach a local branch tracking that ref only when no other worktree owns it, then verify branch, upstream, `HEAD`, and remote OID equality. Treat an existing checkout elsewhere as an ownership conflict.
4. When the Supervisor sends the provisioning instruction with resolved task and host IDs, echo them in a one-time `provisioned` receipt with path, branch, upstream, `HEAD`, and verified remote OID. End the turn without editing and wait for an explicit `proceed` follow-up tied to that receipt and OID. A recovered active task must verify the matching proceed message; if absent, remain read-only and request it.
5. Resume the existing linked branch and pull-request stage. Never create a second implementation or restart from the default branch while recoverable issue work exists.
6. Reconcile the latest contract-defined phase checkpoint with the linked remote SHA and any pull-request head. Recover the referenced Tester and Reviewer receipts. If the checkpoint is missing, inconsistent, or covers another SHA, stop at the last proven phase; any nondurable gate whose clean result cannot be retrieved must be rerun by a fresh bounded task on the recovered exact snapshot. Never infer a phase or pass from branch contents.
7. Preserve unexpected work and report an ownership conflict; do not discard, overwrite, or absorb it without evidence that it belongs to this issue.

## Keep the issue small

Before tests or broad production edits, aggressively test whether the issue is one slice. It should own one observable behavior or preservation boundary, one Verification or approved green-baseline contract, one branch and pull request, and be safely mergeable and production-ready in a fresh context. Incomplete parent functionality may remain disabled behind a feature flag, but every merged slice remains compatible and verified.

If the issue contains multiple independently mergeable vertical slices:

1. Stop implementation and invoke `$plan-delivery-slices` through a bounded collaboration task so each slice becomes a durable GitHub child issue with its own brief, Verification or green-baseline contract, scope, dependencies, project state, and readiness state.
2. Notify the immediate Supervisor once with `decomposed` and the child identifiers and graph.
3. Become a non-coding parent Supervisor. For each child, apply `$orchestrate-milestone-delivery`'s linked-branch, queued-task, detached-worktree, project-state, and explicit model/reasoning protocol. Never implement a child on the parent's branch or worktree.
4. Keep the smallest useful frontier. Parallelize only children with no dependency, no overlapping ownership, no shared unresolved design decision, no incompatible schema or migration work, no shared test-infrastructure change, and no likely interface influence. Dependent children wait for blockers to merge into updated default branch.
5. Receive only each child's `completed`, `decomposed`, `blocked`, or `human-action` signal; keep its testing and review traffic inside that child. Close the parent only after every child has closed through its merged pull request and the parent's acceptance boundary is fulfilled. Record child pull requests in the closing comment and report parent `completed` to the Supervisor.

Do not split into horizontal layers, unsafe partial behavior, or speculative abstractions. If a finding or discovery materially expands the slice, checkpoint recoverable work and return to planning instead of growing a long implementation and review cycle.

## Use the issue's native linked branch

Use GitHub's native issue-branch feature under the contract-defined naming rule and verify the issue's Development linkage. A pull request from that branch must retain the issue association and a closing reference so GitHub closes the issue when the pull request merges.

Checkpoint at least whenever a code-writing task finishes its bounded part:

- leave a repository-conforming commit on the issue branch and push it before that task ends or immediately after handoff;
- preserve objective, issue and contract context, material decisions, resulting changes, rejected paths or discoveries, and exact meaningful-red, green, or alternative evidence in the commit message body; and
- keep an intentionally red Tester checkpoint temporary and clearly marked. Do not publish a pull request from that state.

Before complete-change review, curate temporary red, fixup, and correction checkpoints into coherent commits that each leave the repository valid. Rewriting a pushed issue branch requires the contract's exact lease: verify its current remote SHA immediately before pushing, force only with an exact expected ref-and-SHA lease when authorized, and verify local/remote equality. Never rewrite a protected or ambiguously owned branch.

After every bounded Planner, Tester, Reviewer, or Implementor phase, the issue-owning Implementor writes the contract-defined durable phase checkpoint before starting the next phase. It records a monotonic phase or sequence, issue and branch, exact covered SHA and comparison base, task or run identity when available, result (`clean`, `findings`, `blocked`, or `human-action`), evidence or finding location, and the deterministic next owner or action. Reviewer and Tester roles return their receipts to the Implementor and do not mutate the tracker themselves. Do not put implementation chatter into the Supervisor channel; the durable checkpoint is recovery state, not an upward progress signal.

## Establish the executable contract first

For behavior-changing work without the infrastructure exception:

1. Launch a short-lived Tester on this same worktree with `$author-slice-tests` in `red-contract` mode. Pass the selected model and reasoning explicitly, together with Agent Brief, Verification contract, immutable behavior-start SHA, worktree, branch, and contract.
2. Let the Tester own all test code and prove meaningful red for the intended behavioral reason. Do not edit concurrently.
3. Checkpoint and push the Tester's completed code and evidence.
4. Launch a fresh short-lived Reviewer on the same worktree with `$review-delivery-slice` in `test-contract` mode, passing explicit model and reasoning plus the full handoff.
5. Route findings to a new bounded Tester assignment. Do not silently weaken or rewrite reviewed tests. Repeat with a fresh Reviewer until the test contract is clean.
6. Implement the smallest coherent production change that makes the reviewed tests pass. Run focused verification during iteration and broader affected verification before handoff. Commit and push a recoverable implementation checkpoint.
7. Launch a fresh Tester in `green-finalization` mode with explicit selected model and reasoning. It performs required assertion inversion and restored-green runs, finishes Tester-owned changes, runs applicable gates, and pushes the bounded handoff.

For behavior-preserving work, do not manufacture red:

1. Launch a fresh Tester in `green-baseline` mode with explicit selected model and reasoning. It establishes or authors the smallest characterization portfolio at the immutable behavior-start SHA, proves new assertions sensitive by controlled inversion, restores green, and checkpoints test changes.
2. Launch a fresh `test-contract` Reviewer with explicit selected model and reasoning to assess the green-baseline contract and evidence.
3. Implement the smallest production change preserving the reviewed contract, run focused and broader verification, and commit and push the implementation checkpoint.
4. Launch a fresh Tester in `green-finalization` mode with explicit selected model and reasoning to compare the result with the approved baseline, perform required inversions for changed tests, and run applicable gates before complete-change review.

A missing public surface is not meaningful red when a test cannot compile. In that case, add only the smallest compilable behavior-free shell before the Tester establishes red.

The Implementor may decide not to add automated tests only when no deterministic automated boundary faithfully exercises the changed infrastructure risk. Before implementation, record the decision, reason, alternative verification, and residual risk for the complete-change Reviewer. Difficulty, slowness, or inconvenience alone is insufficient. When no test code changes, skip only the test-authoring/review assignments that no longer apply; production verification, complete-change review, and every contract-defined quality gate still apply.

## Require fresh independent local review

After production verification and Tester green finalization, or after recording the infrastructure no-test decision, curate coherent valid commits, apply the repository's authoring self-review, push any published rewrite with exact lease protection, and verify the remote branch equals candidate `HEAD`. Then launch a new `$review-delivery-slice` task in `complete-change` mode on the same worktree with explicit selected model and reasoning. Supply behavior-start SHA, comparison-base OID, Agent Brief, Verification or green-baseline contract, candidate `HEAD`, commits, index, unstaged state, evidence, and residual risks.

The formal review is independent and code-read-only. Its sole permitted write is the contract-designated local-review Markdown file, and findings return only to this Implementor:

- the Implementor owns production-code findings and decides disposition against repository and specification evidence;
- a fresh Tester owns test-code changes, including justified reconsideration of a reviewed test;
- when findings exist, verify that the Reviewer changed only the designated file and that its reviewed SHA and locations match the supplied snapshot; commit and push that file unchanged before remediation so a restart can recover the queue;
- process findings in file order within each axis, one at a time. For each production finding, make and verify the production change; for each test finding, delegate that finding ID to a fresh Tester. Then update only its Implementor disposition with the decision, rationale, exact resolution commit when applicable, and verification evidence, and commit and push that checkpoint before taking the next finding;
- after any production or test change, run applicable verification, rerun Tester green finalization when assertions changed, curate history, and commission fresh complete-change review of the whole new committed snapshot. The fresh Reviewer appends a new cycle only when it finds actionable issues; a clean review leaves the file and exact `HEAD` unchanged; and
- when a finding implies substantial redesign or new behavior, stop the cycle and plan a new child or blocking issue.

Preserve Reviewer-authored text exactly; dispositions add evidence rather than rewriting history. If the contract treats the file as temporary, remove it before the final candidate review. If the contract retains it as delivery evidence, include every disposition in the final candidate. The local stage is complete only when every recorded finding has a terminal disposition, applicable test-contract review and Tester green finalization or the recorded infrastructure exception are complete, both Standards and Spec axes are clean for the exact curated candidate `HEAD`, that `HEAD` is the verified remote branch head, and all required gates pass. Do not create even a draft pull request before then.

## Publish and own configured GitHub review automation

If the consuming contract defines an automatic GitHub pull-request reviewer:

1. Reconfirm local and remote heads equal the exact reviewed candidate, then create a draft pull request associated with the issue.
2. Keep it draft while required workflows run. Prove each required workflow result belongs to the current pull-request head and use the contract-defined bounded polling cadence and per-workflow wait budget. Fix failures through the owning role, commit and curate, verify, obtain fresh complete-change review, and push the exact reviewed head. If a required workflow remains queued or in progress beyond its budget, is cancelled without a qualifying replacement, or is unavailable, persist the exact head and observations and report `blocked`; do not wait forever or treat absence of failure as success.
3. When the change remains complete and workflows pass, mark the pull request ready according to the contract. Trigger the configured reviewer only through its documented mechanism; do not assume or manually duplicate an automatic trigger.
4. Read every page of current thread-aware review state and reconcile reported counts. Evaluate every finding against issue, current diff, repository policy, and evidence.
5. Apply valid production suggestions through the Implementor and valid test suggestions through a bounded Tester. Reject incorrect or out-of-scope findings with concise evidence.
6. After any change, synchronize any GitHub-applied commit locally, verify it, complete the appropriate Tester or production verification, and obtain a fresh local complete-change review before continuing.
7. Reply to every review comment after disposition with evidence and, for a fix, the pushed commit. Resolve every addressed thread; reply to top-level comments even when GitHub cannot resolve them.
8. After every changed push, wait for required workflows and a new automatic review covering that remote head. Prove completion using the contract-defined provider identity and head-linked review evidence; absence of comments alone is not a completed clean review. Use the contract's bounded wait cadence and maximum budget. If the provider reports unavailable or no qualifying review arrives within that budget, persist the exact pull-request head and wait evidence and report `blocked`; do not wait forever or infer a clean review. Continue until local Standards and Spec review is clean, every current thread is addressed and resolved, the latest configured review covers the current head, no unresolved actionable finding remains, and every required check passes.

If no automatic reviewer is configured, follow the contract's required GitHub review process and still preserve the same disposition, reply, resolution, and exact-head rules. Never send individual test failures, review findings, or remediation chatter to the Supervisor.

## Rebase and merge

Dependent work starts only after blockers merge into the default branch; do not create a pull-request stack unless separately authorized.

Immediately before merge, refresh remote state and require that the pull request is complete, linked to the issue, review-clean, check-clean, and permitted by branch protections. Rebase the issue branch onto current default branch when the contract requires it and record that OID as the new comparison base.

During a conflicted rebase, preserve role ownership:

1. Record pre-rebase head, target default-branch OID, current `HEAD`, replayed-commit identity, progress and todo, complete index-stage state, worktree diff, and untracked inventory. Resolve and stage production-owned conflicts, but never test-owned conflicts.
2. For test-owned conflicts, launch a fresh bounded Tester with `$author-slice-tests` in `rebase-conflict` mode, passing that exact state and explicit selected model and reasoning. Do not touch Git, index, or worktree while it runs.
3. Revalidate all identities and the full index against the Tester's report, verify only declared test-owned paths changed, then continue the rebase.
4. If a file mixes production and test ownership or requires a product or contract decision, leave it unresolved, preserve evidence, abort only as authorized by the contract, confirm the published branch is unchanged, and report `blocked` or `human-action`.
5. After conflict resolution, complete the rebase, apply owner self-review, immediately preserve the recovery checkpoint under exact lease, and rerun the applicable gates and fresh complete-change review.

Every rebase that changes `HEAD`, conflict-free or not, invalidates the prior exact-snapshot verdict. Run applicable verification, Tester green finalization if tests changed, self-review, fresh complete-change review, exact-lease push, workflows, and configured GitHub review again.

Immediately before the merge call, query remote default-branch OID and pull-request head OID. Require default branch to equal the reviewed comparison base and pull-request head to equal the exact locally reviewed, verified remote SHA. If the default branch advanced, repeat rebase, verification, review, and automation loops. Use the contract-defined merge command with an exact head-match guard so a concurrent change fails instead of merging an unreviewed commit.

Completion requires:

- pull request merged into the remote default branch with resulting default-branch or merge-result OID recorded;
- GitHub closed the linked issue through that merge;
- no required review thread or check remains outstanding; and
- the remote branch and project can be reconciled by the Supervisor.

Then send the immediate Supervisor only the canonical `completed` result and identifiers. If issue closure or another terminal fact is inconsistent, report `blocked`; merging does not authorize deployment or release.

## Escalate only coordination signals

Send signals only to the immediate Supervisor, which bubbles them to the top-level Supervisor:

- `decomposed`: child issue identifiers and graph;
- `human-action`: one concrete decision or human implementation request with minimum options and evidence;
- `blocked`: exact blocker and durable checkpoint; or
- `completed`: issue and pull-request URLs, resulting default-branch or merge-result OID, and automatic issue closure.

Pause and checkpoint while awaiting a decision. Do not ask the user separately from the owning Supervisor.
