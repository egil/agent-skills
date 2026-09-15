---
name: deliver-issue
description: Deliver one bounded GitHub issue from its linked branch through test authoring, independent two-axis review, pull request, and merge.
disable-model-invocation: true
argument-hint: "<issue number>"
---

# Deliver one issue slice

Issue: `$ARGUMENTS`

You own this slice's outcome: the linked branch, the production code, the
publication, and the merge. The tester commits and pushes test-only checkpoints
on this same branch; every other Git and GitHub mutation is yours. You delegate test authoring to
`delivery-tester` and review to `/delivery:review-slice`. You never perform the
independent review yourself, and you never write the tests you then review.

If `$ARGUMENTS` is empty, ask which issue to deliver and stop.

## 1. Recover before you start

GitHub and the remote branch are the durable record. The worktree holds
temporary review receipts that must survive interruption.

Re-read the issue, Agent Brief, Verification or approved green-baseline
contract, native relationships, assignee, linked branch and pull request,
durable phase checkpoints, existing review artifacts, checks, and review
threads. Paginate every relationship and reconcile counts.

Record worktree path, branch, `HEAD`, upstream, remote branch head, index
state, and untracked inventory before changing anything. Resume the existing
linked branch and stage — never create a second implementation or restart from
the default branch while recoverable work exists.

Reconcile the latest phase checkpoint against the linked remote SHA. If it is
missing, inconsistent, or covers another SHA, stop at the last proven phase. Any
gate whose clean result cannot be retrieved must be rerun on the recovered
snapshot. Never infer a phase or a pass from branch contents.

Preserve unexpected work and report an ownership conflict rather than absorbing
or discarding it.

Before the first delegation, verify the review artifact root is ignored — read
`../../references/review-artifacts.md`. You own that ignore setup; delegated agents
verify it but never change it.

## 2. Confirm the issue is one slice

Before writing tests or broad production edits, test whether this is one slice:
one observable behavior or preservation boundary, one verification contract, one
branch and pull request, safely mergeable and production-ready on its own.

If it contains multiple independently mergeable slices, stop. Delegate to
`delivery-planner` so each becomes a durable child issue, report the
decomposition, and supervise the children rather than implementing them here.

Do not split into horizontal layers, unsafe partial behavior, or speculative
abstractions. If a discovery materially expands the slice, checkpoint your work
and return to planning rather than growing a long cycle.

## 3. Establish the executable contract

Read `../../references/executable-contract.md` and follow the branch matching the
approved change — behavior-changing, behavior-preserving, or the infrastructure
no-test exception.

Delegate each test phase to `delivery-tester` with exactly one mode, the exact
snapshot, and the contract. It owns test code; you own production code. Do not
edit tests yourself, and do not implement production behavior while it holds the
worktree.

### Reuse the same tester across modes

The executable-contract procedure tells you to resume *the same* tester through
its bounded modes rather than starting a fresh one each time. In Claude Code
that means:

- spawn `delivery-tester` once per issue, and record its agent name or id in the
  durable phase checkpoint alongside the branch and snapshot;
- for every later mode — green finalization, delegated test findings, a
  test-owned rebase conflict — continue *that* agent with a message carrying the
  new mode, the exact snapshot, and any delegated finding IDs. Do not spawn a
  second tester for the same issue;
- each continuation is still a bounded assignment: it ends at that mode's
  completion criterion with its own receipt. Prior context does not substitute
  for revalidating the snapshot; and
- replace the tester only when it cannot safely continue — it is gone, its
  session did not survive, or its scope no longer matches. Record the reason and
  the old and new identities. Ordinary mode completion is not a reason.

Subagent identity does not survive the end of your own session. After a restart,
treat the recorded identity as unresumable, spawn a fresh tester, and rebuild
its context from the branch, the checkpoint, and the existing receipts — which
is why those receipts, not the agent, are the durable record.

## 4. Commission independent review

After production verification and — unless the approved infrastructure no-test
exception applies — tester green finalization, curate coherent commits, push,
and verify the remote branch equals your candidate `HEAD`.

Under that exception you skip test authoring, test-contract review, and green
finalization only. Production verification, complete-change review, and every
applicable non-test gate still apply, and the recorded decision, alternative
verification, and residual risk go into `verification.md` before dispatch. Require a clean
index and tracked worktree; ignored review artifacts are the only allowed local
difference.

Then invoke `/delivery:review-slice complete-change` for that exact `HEAD`. It
pins the snapshot, runs the Standards and Spec axes independently, and returns
their findings.

Process findings in file order within each axis, one at a time:

- production findings are yours to decide and fix;
- test findings go back to `delivery-tester` with the finding ID and current
  snapshot;
- record each decision in `dispositions.md` before taking the next finding;
- preserve reviewer-authored text exactly — dispositions add evidence rather
  than rewriting findings; and
- when a finding implies substantial redesign or new behavior, stop and plan a
  new child or blocking issue.

After any change, rerun applicable verification, resume the tester for green
finalization when assertions changed, curate history, and commission a fresh
review of the new `HEAD`. A clean review still writes its result receipt.

The local stage is complete only when every finding has a terminal disposition,
applicable test-contract review and green finalization are complete *or* the
infrastructure no-test exception is recorded, both axes are clean for the exact
curated `HEAD`, that `HEAD` is the verified
remote branch head, and all required gates pass. Do not open even a draft pull
request before then.

## 5. Publish, review, and merge

Read `../../references/pull-request-review.md`, then `../../references/rebase.md`, and
satisfy each phase's exact-head criterion.

You already hold push and merge authority for this issue through the delivery
mandate. Once the applicable checks pass, perform the push, the ready
transition, and the merge. Do not stop at ready-to-merge to ask permission for
an operation already authorized — but do stop for anything outside it:
deployment, release, bypassing branch protection, another issue's branch, or a
pull-request stack.

Completion requires the pull request merged into the remote default branch, the
linked issue closed by that merge, no outstanding required thread or check, and
the resulting OID recorded.

## 6. Report

Return only: the issue and pull-request URLs, the resulting default-branch OID,
and confirmed issue closure. Keep findings, test detail, command output, and
remediation discussion inside this slice.

If a terminal fact is inconsistent, report it as blocked rather than as
complete. Merging does not authorize deployment or release.
