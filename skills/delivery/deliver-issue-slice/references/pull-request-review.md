# Pull-request checks and review

Enter this phase only with a local-review-clean candidate whose local and remote heads are equal.

When the consuming contract configures an automatic pull-request reviewer:

1. Create the issue-associated draft pull request.
2. Keep it draft while required workflows run. Prove every result belongs to the current head and use the contract's bounded wait cadence and budget.
3. Route failures to the owning role, verify the fix, obtain fresh complete-change review, and push the exact reviewed head. Persist and report a required workflow that remains queued, cancelled, stuck, or unavailable beyond its budget as `blocked`.
4. When the candidate and workflows remain clean, mark the pull request ready and trigger the reviewer only through its documented mechanism.
5. Read every page of thread-aware review state and reconcile counts. Evaluate each finding against the issue, current diff, repository policy, and evidence.
6. Apply valid production findings through the Implementor and valid test findings through a Tester. Reject invalid or out-of-scope findings with concise evidence.
7. After any change, synchronize GitHub-applied commits locally, verify them, finish owning-role checks, obtain fresh local complete-change review, and push the reviewed head.
8. Reply to every comment with its disposition and evidence; resolve every addressed thread. After a changed push, require workflows and a new configured review that demonstrably cover that head.

Absence of comments is not a completed review. When no qualifying configured review arrives within its bounded budget, persist the exact head and wait evidence and report `blocked`.

When no automatic reviewer is configured, apply the contract's GitHub review process with the same exact-head, disposition, reply, resolution, and bounded-wait rules.

This phase is complete only when required workflows pass for the current head, the configured review covers that head, every current thread is addressed and resolved, no actionable finding remains, and fresh local Standards and Spec review is clean. Keep findings and remediation within the issue-owning task.
