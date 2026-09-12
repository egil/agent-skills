# Message-driven waiting

Waiting policy: `delivery-quiet-wait-v2`. This branch changes waiting only; keep the selected worker-reuse policy, model routing, allocation, authorization, and verification gates unchanged.

## Establish the wait state

Before delegating, waiting, or recovering pending work, establish this state in the existing role checkpoint:

```text
wait_policy: delivery-quiet-wait-v2
mechanism: <exposed interruptible tool or verified durable parking>
limits: <this session's supported minimum/maximum and evidence source>
result_route: <immediate owner and pending child/gate identities>
next_check_at_utc: <absolute earliest check/recovery deadline>
check_reason: <required gate/provisioning check or child-liveness check>
exception: <earlier deadline, runtime limit, or none>
```

Reuse valid state. Discover the mechanism and supported limits from this session's tool definitions or verified runtime evidence, not another agent's capabilities or a habitual timeout. Revalidate capabilities after a runtime change or failure. If the maximum is not exposed, record `limit-unknown` and the available evidence; use an established supported duration while resolving the capability gap at a bounded boundary. Never probe with out-of-range arguments.

Pass the result route, pending identities, relevant deadlines, and waiting-policy identifier in every coordinating handoff. Each child validates its own limits. Use one supported result-notification route, retaining provisioning handshakes and guided planning pauses. A queued message alone does not prove an idle session will restart. Keep the coordinating turn active unless automatic parked resumption on both child messages and user input is verified. Leaves may finish after delivering their required result.

## Select the timeout deterministically

Complete independent authorized work and dispatch ready work within the existing allocation first. When only pending work remains:

1. Retain each contract-defined gate, provisioning, and recovery deadline. For pending child work without a liveness interval, establish a check ten minutes from assignment or the last actual liveness check. Preserve its absolute time across intervening waits and unrelated messages. A liveness check is not a task-completion timeout.
2. Select the earliest pending check as `next_check_at_utc`. Keep external gates' specified check cadence and total failure budget; the ten-minute default applies only to unspecified child-liveness checks. Human decisions keep their intentional pause, without an invented reply deadline.
3. Compute the remaining time from the current runtime clock. If the check is due, perform the due-check branch below. Otherwise request the **largest legal timeout no later than that check**. For an exposed `timeout_ms` parameter:

   ```text
   remaining_ms = next_check_at_utc - now_utc       # expressed in milliseconds
   timeout_ms   = min(supported_max_ms, remaining_ms)
   ```

   Honor the tool's minimum and duration granularity. If less than its minimum remains, use another already-verified interruptible facility that can reach the deadline; otherwise perform the targeted check slightly early once and record that limitation. Do not busy-loop on the clock.
4. With ten minutes remaining and a supported maximum of at least ten minutes, pass `timeout_ms: 600000`. With three minutes remaining, pass `180000` when supported. With a documented one-minute cap, use that cap and retain the original deadline as `bounded-wait-fallback`.
5. Use the calculated value explicitly. Responsiveness and a child possibly finishing soon are reasons to use interruptible waiting, not reasons to shorten its timeout. A short observed return does not lower the supported maximum. Record a shorter-wait exception only when an actual earlier check or runtime limitation requires it.

Use runtime waits directly. `clock.sleep` or durable parking is eligible only after verifying its child-message and user-input wake behavior. Shell polling, shell sleep, and a second AI watcher are not substitutes. When the client enforces short waits, disclose the limitation rather than claiming zero wake-ups.

## Handle the return

- **Material result, blocker, failure, or user input:** process it promptly; verify sender, owner, and exact work/snapshot. Reconcile the affected work and dependents. Retire satisfied checks and retain unrelated deadlines. A worker's turn ending does not prove its assignment or issue passed the required gates.
- **No actionable signal and check not due:** re-arm using the remaining time and saved limits. An ordinary timeout or duplicate notification requires no agent listing, history read, GitHub query, checkpoint rewrite, progress nudge, test rerun, or replacement worker.
- **Check due:** inspect only the pending owner or gate. If authoritative evidence shows useful work continues, record that check and the next bounded deadline. Otherwise apply the existing recovery, reclassification, or gate-failure path. Silence alone is not failure; preserve one owner and all required checks.
- **Wake-path failure:** retain the original deadline and use the supported bounded-wait fallback. Record the capability failure once; a sleep tool does not authorize asynchronous work after the host has stopped.

## Recover before the next wait

On handoff, resume, compaction, or a return from a user-directed side task, restore the wait state before selecting another timeout. If it is missing or inconsistent, reconcile pending owners and due checks once, then reconstruct it using the rule above. Continue a valid deadline rather than resetting it to now plus ten minutes. A past deadline triggers its check.

Persist changes only at existing material handoff/checkpoint boundaries, actual due checks, and capability changes. Keep compact state in recovery summaries when supported; do not add per-timeout messages or ledgers. Full discovery remains required at initialization, recovery, scope changes needing it, and inconsistent ownership. Preserve all pre-launch, pre-mutation, and pre-merge validations.

Waiting is complete only when a material event or due check supplies the next action, or the owning workflow reaches its authorized terminal or human-decision boundary. Merely returning from a wait is not completion.
