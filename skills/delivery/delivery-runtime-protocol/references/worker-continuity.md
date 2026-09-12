# Same-issue worker continuity

Reuse policy: `delivery-worker-reuse-v1`. Reuse the existing **Implementor and Tester sessions for one issue**, through bounded phases. Keep their roles separate. Reviewer and Standards/Spec freshness requirements are unchanged. New issues and decomposed child slices receive their own workers, branches, and verification contracts.

## Select the existing owner first

Before spawning, following up, or recovering implementation or testing work:

1. Resolve the same repository, issue, linked branch, worktree, role, and saved session/task ID from the existing phase checkpoint. A similar nickname is not sufficient. Preserve one active issue Implementor and one active Tester assignment for that issue; existing helpers keep their bounded ownership and shared allocation.
2. Reuse the matching capable session when the assignment remains in scope and its effective model/effort still satisfies the routing decision. The Supervisor resumes the issue Implementor for production fixes, PR/workflow corrections, rebases, and recovery. The Implementor resumes the issue Tester for test-contract corrections, approved test changes, green finalization, and test-owned rebase conflicts. If a bounded production helper already owns a delegated correction, return it there rather than duplicating that helper.
3. Start an idle worker's next bounded phase with the runtime's supported resume/follow-up operation and verify that it accepted the assignment. A message merely queued to an idle session is not an execution acknowledgement. For a worker already active, use its supported message route without launching overlapping work; reconcile uncertain state once before retrying.
4. Pass only the phase delta and durable pointers: scoped goal and completion criterion, current mode, accepted contract changes, exact branch/worktree and snapshot identities, delegated finding IDs, remaining checks, applicable waiting state, and inherited authorization. Revalidate these against current state before edits. An unchanged valid receipt remains reusable only under the exact-snapshot artifact protocol.

At the existing phase checkpoint, record `worker_reuse_policy`, role-to-session identities, decision (`created`, `reused`, or `replaced`), phase/mode, and any replacement reason with its evidence. Reuse the existing run and issue identities. Record activation once and decisions at phase boundaries, not per turn or wait. Missing telemetry is not a delivery gate.

## Keep every phase bounded

A session retains identity and useful context; an assignment still ends at its own completion criterion.

- The Tester completes only its currently assigned mode, writes its required receipt, and returns control to the Implementor. Later green finalization requires a new exact-snapshot handoff; familiarity with the tests does not satisfy it early.
- Retain meaningful-red/baseline evidence, assertion sensitivity and restored-green checks, discovery, every applicable gate, and fresh independent review after changes. Reuse neither an old clean verdict for a changed snapshot nor memory in place of a receipt.
- The Implementor owns production edits and disposition decisions; the Tester owns approved test edits. Transfer the worktree explicitly between mutating phases. Keep production and test writers serialized, and keep all implementation state fixed during independent review.
- A phase return ends the worker's active turn where supported while preserving a resumable session ID. Keep an idle Tester inactive until its next handoff; it needs no keep-alive wait loop. A coordinator with pending children follows [message-driven waiting](message-driven-waiting.md) instead of silently ending its turn.

## Replace on evidence, not on a phase change

Create a replacement only when the existing session cannot safely continue: it is missing or non-resumable; its role/scope is incompatible; required model/effort changes cannot be applied through a supported bounded handoff; its working context remains unusable after bounded recovery; or host thread capacity requires retirement. Record the concrete reason, evidence, and old/new identity. Ordinary phase completion, a new commit, compaction, or a wait timeout alone is not a replacement reason.

Preserve exact commits, contract decisions, unresolved findings, failed hypotheses, receipts, and next action before transferring ownership. Stop or otherwise verify that the old owner can no longer mutate the assignment before enabling its replacement. Honor the existing reclassification rule after repeated failed fix/test cycles; continuity is not permission to retry indefinitely or avoid a necessary model change.

Keep the run-wide allocation and stricter host thread limit. An idle or parked session may still occupy host capacity. When it prevents required independent work, use only supported lifecycle operations to checkpoint and retire a safely inactive worker with no pending child work, retaining recovery pointers; never terminate useful active work or raise limits solely to preserve reuse. Resume a retired session only when the runtime supports it; otherwise use the recorded replacement path. A thread-limit failure requires allocation reconciliation, not repeated spawns or removal of review axes.

## Completion

A phase is complete only when its required exact-snapshot receipt and result reach the immediate owner. The Implementor's delivery mandate ends only at its existing authorized completion or escalation boundary. End the same-issue reuse relationship at verified issue completion, cancellation, decomposition into separately owned slices, or an evidence-backed replacement; do not turn it into a cross-issue worker pool.
