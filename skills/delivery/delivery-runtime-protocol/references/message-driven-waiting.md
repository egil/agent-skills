# Message-driven waiting

Trial variant: `delivery-quiet-wait-v1`. Change waiting and wake-up behavior only; do not change models, effort, worker allocation, issue boundaries, worker-reuse rules, permission safeguards, or verification gates.

## Arrange result delivery before waiting

Every coordinating role owns the result-delivery route for its immediate children. Pass the existing role-specific completion, blocker, and decision signals plus the relevant check/recovery deadline in each handoff. Use one supported delivery path for a result; do not duplicate a reliable native result notification with another identical message. Do not add progress heartbeats or requests for reassurance. Preserve provisioning handshakes and guided planning pauses.

Check the exposed runtime tool schema and available capability evidence once per runtime/session setup, then reuse that choice until a failure or runtime change invalidates it. Do not infer capability from a tool name alone. A queued message and an ordinarily idle session are not necessarily a wake-up mechanism. Do not end a coordinating turn or close the session while children are pending unless automatic parked resumption has been verified. A leaf may finish after returning its required result through the established route.

## Suspend without polling

First dispatch ready work within the existing allocation and complete independent authorized actions. When none can advance, use a runtime-managed wait that returns early on the relevant child result/blocker or user input, with a finite deadline:

- Prefer the supported message-interruptible facility that permits the longest wait up to the next due check. Use exposed `wait_agent` with an explicit supported timeout; use `clock.sleep` or durable parking instead only when its child-message and user-input wake behavior is verified. A longer interruptible timeout is not a delay in processing an arriving event.
- Keep all repository-specified workflow, review, provisioning, and recovery deadlines. For pending child work with no specified liveness-check interval, use a **10-minute recovery check**. This is a trial default for detecting silent/lost work, not a task-completion timeout. Wake at the earliest applicable deadline; do not repeatedly extend it when short waits expire or unrelated messages arrive.
- If the runtime cannot provide that long a message-interruptible wait, use its longest supported bounded wait while keeping the coordinating turn active. Record `bounded-wait-fallback` once in existing recovery state. A shorter runtime cap is not permission to issue out-of-range arguments, alter configuration, or silently end supervision.

Use runtime tools directly, not shell `sleep`, shell polling loops, busy reasoning, or another AI watcher. Do not claim zero wake-ups if the client forces short timeouts. For an external gate without push notifications, wait until its scheduled check and query only that gate; preserve its total wait budget and failure path. Guided mode or a pending human decision still pauses as specified by the owning skill; do not poll the user or manufacture a deadline for their answer.

## Act only on a useful wake-up

- **Material result, blocker, failure, or user input:** process it promptly. Verify the sender/owner and the exact work/snapshot it concerns; ignore duplicate or stale signals already handled. Reconcile the affected work and dependents, not every issue. Keep review findings inside the issue-owning chain. A runtime turn ending does not prove a task, review, or issue passed its completion gates.
- **Plain timeout, no new signal, no due deadline:** re-arm the wait. Do not list agents, reread histories, check GitHub, rewrite checkpoints, nudge workers, rerun tests, or spawn replacements merely because the wait returned.
- **Due recovery/check deadline:** inspect only the pending owner or gate. If evidence shows useful work is still running, set the next bounded check; silence alone is not failure. If work ended without a usable result, ownership is lost, or the gate failed, use the existing recovery/reclassification path. Never create a duplicate owner or weaken a required check to avoid waiting.

Full discovery remains required at initialization, recovery, scope changes requiring it, and inconsistent state. Every required pre-launch, pre-mutation, and pre-merge validation remains intact. On wake-path failure, fall back to the supported bounded wait and preserve the capability limitation in existing recovery state. Neither this policy nor a sleep tool authorizes unverified asynchronous work after the host has stopped.
