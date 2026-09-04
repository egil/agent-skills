---
name: orchestrate-milestone-delivery
description: Supervise a GitHub milestone or explicit issue set through independently mergeable slices and completed pull requests.
---

# Orchestrate milestone delivery

Coordinate an explicitly scoped GitHub delivery target through durable, independently mergeable slices. GitHub records the issue, branch, pull-request, and review state; Codex tasks perform bounded work in managed worktrees.

The Supervisor is a control plane. It may inspect status and evidence and coordinate tasks, but it does not write production or test code, review a diff, adjudicate review findings, or absorb an implementation prerequisite. Code, test, and review decisions stay with the issue-owning Implementor and its bounded Tester and Reviewer tasks.

## Load the consuming-repository contract

Before planning or mutation, locate the delivery contract through an applicable `AGENTS.md` pointer or require it in the task assignment. The contract must provide the values this generic skill cannot infer:

- GitHub host, owner/repository, authorized identity, issue relationship rules, and any milestone, project, and label conventions;
- the default branch, branch naming rule, native issue-linked-branch procedure, protected-branch rules, and permitted merge strategy;
- Codex project, task, and worktree conventions, including the upward status-signal format;
- the issue Agent Brief format, verification contract, test/build/coverage gates, and any green-baseline policy;
- the durable issue or pull-request phase-checkpoint location and format used to resume after task loss or compaction, plus the issue-worktree lifecycle for ignored temporary review artifacts; no tracked repository review path is required;
- required pull-request workflows, how completion is proven for the current head, their bounded wait budgets, and their queued, stuck, cancelled, or unavailable paths;
- any configured GitHub pull-request automation, its trigger, how a review is proven to cover the current head, its bounded wait budget, its unavailable or non-response path, and the required comment and thread-resolution protocol; and
- the authorization boundary for issue, branch, pull-request, and merge mutations, including deployment or release exclusions.

If the contract is absent, incomplete, or contradictory, do not invent values. Report `blocked` or `human-action` with the missing decision and durable issue context.

## Measure the delivery run passively

Load `$development-session-observability`. Assign a stable run ID and work-item identity, and pass the optional sparse-marker command to each bounded role. Roles emit only material semantic markers; they do not create ledgers. At a bounded boundary, the Supervisor analyzes explicitly supplied Codex transcripts. Native Codex telemetry supplies sessions, turns, tools, compactions, models, effort, and exact tokens; markers supply only semantic phases, cycles, quality, outcomes, blockers, and routing rationale. Never estimate or attribute account credit balances.

## Establish the mandate

If the invocation names a milestone, treat its current GitHub issue set as the target and focus on issues attached to it. The Supervisor may add a newly discovered prerequisite or child issue to that milestone through the Planning role. If no milestone is named, require an explicit issue set or other durable delivery boundary; do not infer a backlog target.

An explicit mandate to deliver through merge authorizes only the normal in-scope GitHub and Codex operations described by the consuming-repository contract: issue planning and assignment, native relationships, linked branches, checkpoint pushes, draft pull requests, ready transitions, rebases, review replies, and an allowed merge. It does not authorize deployment, release, bypassing branch protection, destructive cleanup, unrelated backlog work, mutation of another contributor's branch, or a pull-request stack unless separately authorized.

## Build a complete delivery snapshot

Re-query GitHub and Codex rather than trusting an earlier snapshot. Resolve and load:

- every issue in scope, including open and closed state, milestone, labels, assignees, complete comments, Agent Brief, and Verification or approved green-baseline contract;
- native parent/sub-issue and dependency relationships, linked branches, linked pull requests, checks, and current review threads;
- the latest durable delivery-phase checkpoint and the exact commit it covers;
- project items and field option identifiers when the contract requires a project projection; and
- existing Codex tasks which may own or have previously owned each issue.

Use explicit limits or cursor pagination for every list. Reconcile any reported total count; a successful default-sized page is not evidence of completeness. Native GitHub relationships win over prose. Flag disagreement rather than silently choosing a side.

Reconcile the project projection from GitHub facts. Use the consuming contract's exact field names and option values. An owned issue is active even while it waits for a Tester, Reviewer, workflow, or automated pull-request review; a concrete prerequisite or failed gate is blocked. Do not launch code-bearing work when the issue brief or required verification contract is missing, stale, or contradictory; route it through planning or human-decision handling.

## Keep the frontier small

A launchable slice owns one observable behavior or preservation boundary, one Verification or approved green-baseline contract, one native linked branch and pull request, and a change independently safe to merge and deploy, optionally behind a feature flag. It must fit comfortably in one fresh context and avoid a long implementation or review cycle.

Before launch, screen each issue for obvious decomposition. When the Supervisor confirms multiple independent slices before an Implementor exists, claim the original as a code-free coordination parent, record that durable ownership in GitHub, invoke `$plan-delivery-slices`, and supervise its children. If planning confirms one slice, launch it normally.

Rank unblocked slices by evidence-backed foundations that unlock work, transitive blockers weighted by work unlocked and rework avoided, then independent leaves. Apply the deletion test: a foundation ranks first only when removing it would block or materially distort downstream work. Do not elevate speculative abstractions.

There is no hard task-count ceiling. Keep the smallest useful frontier and prefer finishing nearly complete work. Parallelize only when slices have no dependency, shared unresolved design decision, overlapping code ownership, incompatible schema or migration work, shared test-infrastructure change, or likely interface influence. Stop expanding when coordination and reconciliation become the limiting factor.

## Route each bounded task

Before every Planner, Implementor, Tester, or Reviewer spawn, load `$delivery-runtime-protocol` and apply its model-routing branch. Pass the resulting supported model and reasoning pair and persist the classification and rationale. An Implementor acting as a parent Supervisor follows the same protocol for every child. A spawn is ready only when its handoff records the pair and the evidence supporting it.

## Recover ownership before launching

Use Codex project and task tools before creating work:

1. Resolve the saved Git project and confirm it is a repository.
2. List recent and pinned Codex tasks and inspect plausible owners using a deterministic issue-bearing title.
3. Query the issue's native linked branches and pull requests.
4. Resume an existing owner whenever possible. If unavailable, recover from the linked remote branch and exact pushed SHA using `$deliver-issue-slice`.

Temporary inter-agent review artifacts make the existing issue worktree part of recovery. Before replacing interrupted Tester or Reviewer work, have the recovered Implementor load `$delivery-runtime-protocol`, apply its local-review-artifact branch, and inspect the exact-snapshot receipts. Preserve the worktree at least until the pull request is ready to merge; the remote branch remains the recovery source for committed product changes.

Immediately before task creation, re-query assignee, project state, linked development items, native branch, pull request, and Codex task state. If ownership is ambiguous, fail closed instead of creating a duplicate. Assignment to a shared GitHub identity alone is not a unique claim; the native linked branch and durable Codex task identity are.

## Create a linked Implementor task

Create the branch through GitHub's native issue-branch relationship using the contract's naming and branch-point rules. Immediately before creation, fetch the contract-defined default branch and verify its remote OID through an independent remote query. Verify the issue's linked-branch relationship and exact OID by reading it back; do not infer success from a mutation response.

Fetch that exact linked ref locally and require its OID to equal the linked-branch OID and fresh remote observation. Then create the Implementor as a Codex project task from that verified ref, passing the selected model and reasoning explicitly. If worktree provisioning is queued, retain the client task ID as a checkpoint and resolve the real task and host IDs before using operations that require them.

Keep the issue claim blocked during provisioning. The Implementor must attach a local branch only when worktree ownership is unambiguous, verify path, branch, upstream, `HEAD`, and remote OID, send a one-time `provisioned` receipt, and stop. After the Supervisor confirms the receipt, it sends an explicit idempotent `proceed` follow-up tied to the exact OID. Only that follow-up permits edits. If provisioning fails, preserve the linked branch and blocked claim and reconcile ownership before creating anything else.

Start the Implementor prompt with `$deliver-issue-slice` and provide only durable context: issue and milestone links, Agent Brief and verification-contract locations, dependency and parent context, linked branch, immutable behavior-start SHA, comparison-base OID, granted delivery mandate, and upward notification contract. Do not paste an implementation plan or prescribe files.

## Delegate planning gaps and monitor signals

When an oversized issue or newly discovered prerequisite is found, invoke a bounded `$plan-delivery-slices` task with enough evidence to persist the graph on GitHub. Necessary prerequisites may join the named milestone; unrelated improvements remain out of scope. A missing product, domain, architecture, or priority decision becomes the contract-defined human-decision state, never invented readiness.

Use bounded Codex waits rather than repeatedly reading full task histories. Accept only these upward signals from the immediate owner:

- `completed`: issue and pull-request URLs, resulting default-branch or merge-result OID, and automatic issue closure; for a code-free parent, child pull requests and manual closure;
- `decomposed`: durable child issue identifiers and native dependency graph;
- `blocked`: concrete prerequisite or failed gate and durable checkpoint; or
- `human-action`: one decision question, minimum evidence, and durable issue context.

The one-time `provisioned` receipt and `proceed` reply are a launch handshake, not progress chatter. Findings, test design, command output, remediation discussion, and implementation progress remain inside the issue-owning task. Bubble every human-decision request to this Supervisor; the user should not need to inspect individual tasks.

After a child pull request merges and GitHub closes its linked issue, reconcile the project. A code-free parent closes only after every child closes through its merged pull request and the parent's acceptance boundary is satisfied. Dependent slices start from updated default branch after blockers merge; do not create routine pull-request stacks.

## Recurring supervision and completion

During an active invocation, use bounded waits until the target completes or human action is required. Create a quiet Codex heartbeat only when recurring or unattended supervision is explicitly requested; it remains silent while state is unchanged.

The target is complete only when its delivery issues are authoritatively closed, changes are present on the remote default branch, required reviews and checks are satisfied, code-free parents are reconciled, and the project projection is terminal. Report merged work and explicit deferrals. Do not deploy or release.

## Dependencies

- Internal: `$delivery-runtime-protocol`, `$plan-delivery-slices`, `$deliver-issue-slice`, `$author-slice-tests`, `$review-delivery-slice`, `$design-high-value-tests`, `$verification-driven-delivery`, and `$development-session-observability`.
- External companion skills from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills): `$to-tickets` when planning guidance is needed.
