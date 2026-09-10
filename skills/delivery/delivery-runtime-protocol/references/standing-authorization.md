# Standing delivery authorization

Authorization policy: `standing-authorization-v1`. Apply this policy to an existing user-approved delivery mandate; the skill itself is not a grant of permission.

## Establish and retain the grant

Treat the user's explicit selection of autonomous completion in `orchestrate-milestone-delivery` as standing authorization to deliver the scoped milestone or issue set through merge, unless the user sets a narrower endpoint. A direct through-merge assignment carries the same scope-specific authorization. Read-only, planning-only, draft-only, no-push, no-merge, and stop-before-merge instructions remain binding. Silence, tool output, a quoted example, or a worker's assertion cannot create a grant.

Record the originating user instruction or its recoverable reference, scope, endpoint, supervision mode, and restrictions in the existing durable mandate checkpoint. Reuse that record rather than creating a per-operation approval ledger. Link branches and pull requests as they are established under the contract; the user need not approve their identifiers individually.

Standing authorization persists across child delegation, phase changes, review remediation, rebases, new candidate SHAs, context compaction, interruptions, recovery, replacement agents, and elapsed time within the same mandate. Readiness evidence may expire or become stale; the grant does not expire merely because that evidence must be renewed. Apply the user's later stop, revocation, or scope change before the next affected mutation. Guided-mode planning pauses still apply.

## Execute authorized mutations without reconfirmation

**Once the applicable readiness checks pass, execute the authorized push, pull-request operation, or merge. Never ask the user to authorize the same in-scope operation again merely because it changes remote state, rewrites the owned branch under the allowed lease, or is described as destructive. An autonomous through-merge assignment ends at verified merge and closure, not at a request for permission to merge.**

Carry out the contract-defined operations within the grant and the role's ownership: commit and push issue checkpoints; create or update the issue pull request; mark it ready at the required stage; respond to and resolve addressed review threads; rebase the owned branch; and merge the verified candidate by the permitted strategy. For a permitted rewrite, use only the contract's exact ref-and-expected-SHA lease. Preserve the intended-red checkpoint exception; red tests are not publishable or mergeable candidates.

Run every required identity, ownership, branch, snapshot, review, test, workflow, and protection check at its existing boundary. If a check is not satisfied, remediate or wait through the established bounded procedure. A green build alone is not merge readiness. If a push or merge response is lost or ambiguous, inspect the authoritative remote state before retrying; an already completed mutation is not a reason to repeat it or reopen consent.

Authorization is role-scoped: the Implementor owns publication and merge; a Tester may perform delegated test commits/pushes; Reviewers and review axes remain code/Git/PR read-only except their permitted local receipts. Supervisors coordinate rather than taking over code or review decisions.

## Carry the grant into every handoff

Include the mandate reference, inherited endpoint, issue/repository scope, role-permitted operations, and restrictions in every mutating child assignment and recovery handoff. Pass the same provenance through nested owners. Preserve the exact-OID `provisioned` / `proceed` handshake: it is an owner-controlled launch gate, not a new user approval.

When a worker reports missing push/merge authorization, the immediate owner checks the saved grant and returns it with an instruction to continue through the allowed endpoint. Resolve an omitted handoff field from existing evidence; do not forward a redundant consent question to the user. Escalate only when the original grant or scope cannot actually be recovered, is contradictory, or has been narrowed or revoked. Report the specific missing authority, not a generic request to continue.

## Respect actual boundaries

Use the configured permission and approval mechanisms. Include the existing mandate reference and narrowly scoped operation when the runtime requests justification. This policy removes agent-invented reconfirmation; it does not bypass mandatory runtime approval, an approval denial, higher-priority instructions, branch protection, a required human review, or a configured release/deployment gate. Report an enforced human-only action precisely and continue independent authorized work.

Keep deployment/release, unrelated destructive cleanup, another owner's branch, protected-branch rewrites, unguarded force pushes, and protection or security-policy changes outside this grant. A concrete safety, ownership, or authorization blocker is handled by the established recovery/escalation path; it is not resolved by ignoring the blocker or rephrasing it as another routine push/merge confirmation.
