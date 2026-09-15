---
name: deliver-milestone
description: Drive a GitHub milestone or explicit issue set through small, independently mergeable slices to merged pull requests.
disable-model-invocation: true
argument-hint: "<milestone number, milestone title, or issue list>"
---

# Deliver a milestone

Target: `$ARGUMENTS`

You are the delivery supervisor **and** the implementor. You hold the worktree
and the branch, and you retain all production, publication, and merge authority.
You delegate only bounded, context-isolated work: planning, test authoring, and
the two review axes.

Delegated roles perform their own scoped writes — the planner persists the issue
graph, and the tester commits and pushes test-only checkpoints. Everything else
that mutates Git or GitHub is yours.

If `$ARGUMENTS` is empty, ask which milestone or issue set to deliver and stop.
Do not infer a backlog target.

## 1. Choose the supervision mode

Ask once, before launching any delivery work:

> Should I pursue this milestone autonomously through completion, asking only
> when I need human clarification, or pause after each completed issue so we can
> plan the next step together?

Continue read-only discovery while the answer is pending. Silence is not a mode
choice. Record the answer alongside the mandate (step 2) and resume it after any
interruption or compaction.

- **Autonomous** — pursue the scoped goal through its authorized stages. Make
  evidence-backed sequencing decisions, recover stalled work, and continue after
  each issue without a permission checkpoint. Ask only for a concrete decision,
  missing authority, or unavailable human-controlled resource.
- **Guided** — deliver one slice at a time. After each completes, report the
  result, remaining dependencies, and a proposed next slice, then wait for
  direction.

Selecting autonomous for this through-merge workflow also carries standing
push and merge authority within the stated scope, unless the user sets a
narrower endpoint. A user-requested mode change supersedes the saved choice.

## 2. Load the delivery contract

Locate the consuming repository's delivery contract through `CLAUDE.md`, an
`AGENTS.md` pointer, or the invocation itself. Read `../../references/contract.md` for
the values it must supply.

If the contract is absent, incomplete, or contradictory, stop and report the
missing decision with its issue context. Do not invent a default branch, branch
prefix, label, merge strategy, or review service.

Persist the originating instruction, scope, endpoint, mode, and restrictions
where the contract defines durable checkpoints. Carry that grant into every
delegated assignment.

## 3. Build the delivery snapshot

Build the full snapshot at start and after any recovery. Resolve every issue in
scope with state, milestone, labels, assignees, comments, Agent Brief, and
Verification or approved green-baseline contract; native parent/sub-issue and
dependency relationships; linked branches, pull requests, checks, and review
threads; and the latest durable phase checkpoint with the exact commit it covers.

Use explicit limits or cursor pagination for every list and reconcile reported
totals — a successful default-sized page is not evidence of completeness. Native
GitHub relationships win over prose. Flag disagreement rather than choosing a side.

During normal delivery, reconcile only the affected issue and its dependents on
material signals. A plain notification does not require a full refresh.

## 4. Keep the frontier small

A launchable slice owns one observable behavior or preservation boundary, one
verification contract, one linked branch and pull request, and a change
independently safe to merge. Read `../../references/slicing.md` before judging size.

Screen each issue for obvious decomposition before launch. When you confirm
multiple independent slices, claim the original as a code-free coordination
parent, record that in GitHub, delegate to `delivery-planner`, and supervise its
children.

Rank unblocked slices by foundations that unlock work, then transitive blockers
weighted by work unlocked, then independent leaves. Apply the deletion test: a
foundation ranks first only when removing it would block or materially distort
downstream work.

Prefer finishing nearly complete work over starting new work. Parallelize only
when slices share no dependency, unresolved design decision, code ownership,
schema or migration work, or test infrastructure. Guided mode keeps the next
issue behind the planning checkpoint.

## 5. Deliver each slice

For each selected issue, read `../deliver-issue/SKILL.md` and follow it for that
issue, carrying it through merge before returning here. It owns the branch, the
test and review cycle, publication, and the merge.

Read the file rather than invoking `/delivery:deliver-issue`: both entry points
are user-invoked only, so the slash command is not available to you from inside
this run. The user can still start a single slice that way directly.

Delegate planning gaps to `delivery-planner`. A product, domain, architecture,
or priority decision that repository evidence cannot resolve becomes a
human-decision state — never invented readiness.

## 6. Wait without polling

Delegated agents run in the background and notify you on completion. Do not poll
for their status, re-read history, or send progress nudges while they work.

For external gates — workflow runs, an automated pull-request review — use
`Monitor` with the contract's bounded budget rather than a shell loop. Foreground
`sleep` is unavailable; do not substitute repeated status queries for waiting.

An agent's turn ending is not evidence that its work passed a gate. Verify the
required evidence yourself before acting on a completion.

## 7. Completion

The target is complete only when its delivery issues are authoritatively closed,
the changes are on the remote default branch, required reviews and checks are
satisfied, code-free parents are reconciled, and the project projection is
terminal.

Report merged work and explicit deferrals. Do not deploy or release.
