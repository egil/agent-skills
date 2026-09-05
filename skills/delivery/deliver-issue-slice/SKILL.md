---
name: deliver-issue-slice
description: Deliver one GitHub issue from its linked branch through independent testing, review, and merge.
---

# Deliver an issue slice

Own one small, independently mergeable vertical slice through merge. Coordinate the Tester and Reviewer, but do not replace either role or perform the formal independent review yourself. The Supervisor never participates in review of changed code: findings and remediation stay between this Implementor, the Tester, and the Reviewer.

## Load the consuming-repository contract

The assignment must identify or point to a delivery contract discoverable through applicable `AGENTS.md`. It must define:

- GitHub host, repository, authorized identity, and issue and pull-request relationships;
- linked-branch, worktree, default-branch, naming, commit, push, protection, and merge rules;
- Agent Brief and Verification or green-baseline contract;
- test, build, quality, workflow, and automated-review gates with exact-head evidence and bounded waits;
- durable phase-checkpoint format, upward signals, and authorized mutations; and
- deployment and release exclusions unless separately authorized.

A tracked repository path for temporary review records is neither required nor permitted to block delivery.

If the contract or assignment omits a required value, stop before the affected mutation and report `blocked` or `human-action`; do not invent a project, account, branch prefix, default branch, label, merge strategy, or review service.

## Dependencies and model selection

- Internal: `$orchestrate-milestone-delivery` for linked-branch, provisioning, and project-state protocol; `$delivery-runtime-protocol` for model routing and local review recovery; `$plan-delivery-slices`, `$author-slice-tests`, `$review-delivery-slice`, `$design-high-value-tests`, `$verification-driven-delivery`, and `$development-session-observability`.
- External companion skills from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills): `$to-tickets` when planning is required and `$code-review` through the Reviewer.

Load `$delivery-runtime-protocol` and apply its model-routing branch to the assigned model and every bounded Planner, Tester, or Reviewer handoff. Pass the effective supported model and reasoning pair, evidence-backed rationale, shared worker allocation, and verification boundary. An Implementor that becomes a parent Supervisor applies the same protocol to every child.

Load `$development-session-observability` and inherit the Supervisor's stable run ID and issue work item. When supplied, use the marker command only for material semantic transitions and pass it to bounded children. Do not create ledgers, summarize telemetry, or duplicate Codex-native usage. Telemetry must not delay delivery or cross the review-reporting boundary.

Apply the protocol's reclassification handoff when work stalls. Reroute a bounded child within the assigned allocation, or return your own stalled assignment to the Supervisor with recovery evidence. Reserve human-action signals for actual decisions or authorization gaps.

An explicit assignment to deliver issue `#N` through merge authorizes only the ordinary operations required by the contract: work on the issue's own linked branch and worktree, commit and push checkpoints, publish and update its pull request, rebase its own branch with exact lease protection, reply to review, and perform the permitted merge once every gate is clean. It does not authorize deployment, release, bypassing protections, mutating another issue's branch, creating a pull-request stack, destructive cleanup, or unrelated tracker work.

Before a GitHub mutation, verify the contract-defined identity with `gh auth status --hostname <github-host>` and `gh api --hostname <github-host> user --jq .login`. Do not change Git credentials, request or reveal a token, or continue after incomplete or inconsistent identity evidence.

## Recover durable state

GitHub and the remote branch are the durable delivery record; the existing issue worktree holds temporary inter-agent review receipts that must survive agent interruption.

1. Re-read the issue, Agent Brief, Verification or green-baseline contract, milestone or scope, native parent and dependency relationships, assignee, linked branch or pull request, durable phase checkpoints, predictable local review artifacts, checks, and review threads. Paginate every relationship and review-thread connection and reconcile reported counts; a successful default-sized page is not complete evidence.
2. Record worktree path, branch or detached state, `HEAD`, upstream, remote branch head, index state, worktree state, and untracked inventory before changing anything.
3. Fetch the exact linked remote ref and inspect `git worktree list --porcelain`. If this worktree is detached, attach a local branch tracking that ref only when no other worktree owns it, then verify branch, upstream, `HEAD`, and remote OID equality. Treat an existing checkout elsewhere as an ownership conflict.
4. When the Supervisor sends the provisioning instruction with resolved task and host IDs, echo them in a one-time `provisioned` receipt with path, branch, upstream, `HEAD`, and verified remote OID. End the turn without editing and wait for an explicit `proceed` follow-up tied to that receipt and OID. A recovered active task must verify the matching proceed message; if absent, remain read-only and request it.
5. Resume the existing linked branch and pull-request stage. Never create a second implementation or restart from the default branch while recoverable issue work exists.
6. Reconcile the latest contract-defined phase checkpoint with the linked remote SHA and any pull-request head. Recover the referenced Tester and Reviewer receipts. If the checkpoint is missing, inconsistent, or covers another SHA, stop at the last proven phase; any nondurable gate whose clean result cannot be retrieved must be rerun by a fresh bounded task on the recovered exact snapshot. Never infer a phase or pass from branch contents.
7. Preserve unexpected work and report an ownership conflict; do not discard, overwrite, or absorb it without evidence that it belongs to this issue.

Before the first Tester handoff and on every recovery, load `$delivery-runtime-protocol` and apply its local-review-artifact branch. The Implementor owns local ignore setup and retention. The protocol's snapshot and content validation—not file presence—decides whether to reuse complete work or resume missing work; a missing tracked review path never blocks delivery or proves review passed.

## Keep the issue small

Before tests or broad production edits, aggressively test whether the issue is one slice. It should own one observable behavior or preservation boundary, one Verification or approved green-baseline contract, one branch and pull request, and be safely mergeable and production-ready in a fresh context. Incomplete parent functionality may remain disabled behind a feature flag, but every merged slice remains compatible and verified.

If the issue contains multiple independently mergeable vertical slices:

1. Stop implementation and invoke `$plan-delivery-slices` through a bounded collaboration task so each slice becomes a durable GitHub child issue with its own brief, Verification or green-baseline contract, scope, dependencies, project state, and readiness state.
2. Notify the immediate Supervisor once with `decomposed` and the child identifiers and graph.
3. Become a non-coding parent Supervisor. For each child, apply `$orchestrate-milestone-delivery`'s linked-branch, queued-task, detached-worktree, project-state, and model-routing protocol. Inherit the saved supervision mode and shared worker allocation. Never implement a child on the parent's branch or worktree.
4. Follow the Supervisor's frontier and supervision-mode rules. In guided mode, send a `planning-checkpoint` for each completed child through the immediate Supervisor to the top-level Supervisor and wait for the user's next-step direction before launching another child. Dependent children wait for blockers to merge into updated default branch.
5. Receive only each child's `completed`, `decomposed`, `planning-checkpoint`, `blocked`, or `human-action` signal; keep its testing and review traffic inside that child. Relay nested planning checkpoints to the top-level Supervisor. Close the parent only after every child has closed through its merged pull request and the parent's acceptance boundary is fulfilled. Record child pull requests in the closing comment and report parent `completed` to the Supervisor.

Do not split into horizontal layers, unsafe partial behavior, or speculative abstractions. If a finding or discovery materially expands the slice, checkpoint recoverable work and return to planning instead of growing a long implementation and review cycle.

## Use the issue's native linked branch

Use GitHub's native issue-branch feature under the contract-defined naming rule and verify the issue's Development linkage. A pull request from that branch must retain the issue association and a closing reference so GitHub closes the issue when the pull request merges.

Checkpoint at least whenever a code-writing task finishes its bounded part:

- leave a repository-conforming commit on the issue branch and push it before that task ends or immediately after handoff;
- preserve objective, issue and contract context, material decisions, resulting changes, rejected paths or discoveries, and exact meaningful-red, green, or alternative evidence in the commit message body; and
- keep an intentionally red Tester checkpoint temporary and clearly marked. Do not publish a pull request from that state.

Before complete-change review, curate temporary red, fixup, and correction checkpoints into coherent commits that each leave the repository valid. Rewriting a pushed issue branch requires the contract's exact lease: verify its current remote SHA immediately before pushing, force only with an exact expected ref-and-SHA lease when authorized, and verify local/remote equality. Never rewrite a protected or ambiguously owned branch.

After every bounded Planner, Tester, Reviewer, or Implementor phase, the issue-owning Implementor writes the contract-defined durable phase checkpoint before starting the next phase. It records a monotonic phase or sequence, issue and branch, exact covered SHA and comparison base, task or run identity when available, result (`clean`, `findings`, `blocked`, or `human-action`), evidence or finding location, and the deterministic next owner or action. Reviewer and Tester roles return their receipts to the Implementor and do not mutate the tracker themselves. Do not put implementation chatter into the Supervisor channel; the durable checkpoint is recovery state, not an upward progress signal.

## Establish the executable contract

Before production work, read [the executable-contract procedure](references/executable-contract.md). For behavior-changing or behavior-preserving work, complete that branch's pre-implementation Tester and test-contract-review steps before starting production implementation. For an infrastructure exception, first record the approved no-test decision, alternative evidence, and residual risk. Then follow the selected branch through its final completion criterion.

## Require fresh independent local review

After production verification and Tester green finalization, or after recording the infrastructure no-test decision, curate coherent valid commits, apply the repository's authoring self-review, push any published rewrite with exact lease protection, and verify the remote branch equals candidate `HEAD`. Require the index, tracked worktree, and relevant untracked implementation inventory to be clean; ignored protocol artifacts are the only allowed local difference. Then launch a new `$review-delivery-slice` task in `complete-change` mode on the same worktree with explicit selected model and reasoning. Supply behavior-start SHA, comparison-base OID, Agent Brief, Verification or green-baseline contract, candidate `HEAD`, commits, local-state evidence, and residual risks.

Before dispatch, resolve the predictable snapshot directory. For an approved no-test exception, write its exact evidence to `verification.md`; otherwise require the Tester's matching receipt. Create or validate `request.md`, then check `standards.md`, `spec.md`, and `result.md`. Return an already-complete exact-snapshot result to the workflow without spawning duplicate review. Otherwise commission only the missing or invalid work.

The formal review is independent and code-read-only. Its writes are limited to that snapshot's ignored review artifacts, and findings return only to this Implementor:

- the Implementor owns production-code findings and decides disposition against repository and specification evidence;
- a fresh Tester owns test-code changes, including justified reconsideration of a reviewed test;
- when findings exist, verify that the Reviewer changed only the expected artifact files and that every reviewed SHA and location matches the supplied snapshot; keep the complete axis and result receipts before remediation so a restart can recover the queue;
- process findings in file order within each axis, one at a time. For each production finding, make and verify the production change; for each test finding, delegate that finding ID to a fresh Tester. Then update only `dispositions.md` with the decision, rationale, exact resolution commit when applicable, and verification evidence before taking the next finding;
- after any production or test change, run applicable verification, rerun Tester green finalization when assertions changed, curate history, and commission fresh complete-change review in the new `HEAD` snapshot directory. A clean review still writes its exact-snapshot result receipt without changing Git state; and
- when a finding implies substantial redesign or new behavior, stop the cycle and plan a new child or blocking issue.

Preserve Reviewer-authored text exactly; dispositions add evidence rather than rewriting history. The local stage is complete only when every recorded finding has a terminal disposition, applicable test-contract review and Tester green finalization or the recorded infrastructure exception are complete, both Standards and Spec axes are clean for the exact curated candidate `HEAD`, a complete exact-snapshot result receipt exists, that `HEAD` is the verified remote branch head, and all required gates pass. Do not create even a draft pull request before then.

## Publish and reconcile pull-request review

After the local stage is complete, read [the pull-request review procedure](references/pull-request-review.md). Apply the configured-review or no-automation branch through its exact-head completion criterion before attempting merge.

## Rebase and merge

Immediately before merge, read [the rebase procedure](references/rebase.md) and satisfy its fresh-base and exact-head criterion. Then use the contract-defined merge operation.

Completion requires:

- pull request merged into the remote default branch with resulting default-branch or merge-result OID recorded;
- GitHub closed the linked issue through that merge;
- no required review thread or check remains outstanding; and
- the remote branch and project can be reconciled by the Supervisor.

Then send the immediate Supervisor only the canonical `completed` result and identifiers. If issue closure or another terminal fact is inconsistent, report `blocked`; merging does not authorize deployment or release.

## Escalate only coordination signals

Send signals only to the immediate Supervisor, which bubbles them to the top-level Supervisor:

- `decomposed`: child issue identifiers and graph;
- `planning-checkpoint`: in guided mode, a completed child's issue and pull-request result, remaining dependencies, and proposed next step for planning with the user;
- `human-action`: one concrete decision or human implementation request with minimum options and evidence;
- `blocked`: exact blocker and durable checkpoint; or
- `completed`: issue and pull-request URLs, resulting default-branch or merge-result OID, and automatic issue closure.

Pause and checkpoint while awaiting a decision. Do not ask the user separately from the owning Supervisor.
