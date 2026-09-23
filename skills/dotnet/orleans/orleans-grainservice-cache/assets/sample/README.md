# Orleans Grain Service Subscription Sample

This sample validates that an Orleans grain can persist `GrainService` subscriber IDs and continue notifying them after the grain deactivates and later reactivates. Each silo hosts a `CacheGrainService` that subscribes once at startup. The grain reconstructs observer references from persisted IDs and fans out concurrently, so stale observers do not make healthy cache updates wait one response timeout at a time. Failed observer IDs are removed only when current cluster membership proves their exact silo generation is terminal or superseded; all confirmed removals are written together. The tests await Orleans RPC telemetry spans via `IAsyncEnumerable<Activity>` instead of polling with `Task.Delay`.

## What the test proves
- A grain service on **each silo** subscribes once and keeps an in-memory cache.
- The grain persists the service references.
- After the grain deactivates, updating its state still notifies **all** grain services, demonstrating that stored service references remain valid.

## Additional regression coverage

- Mix live deliveries with many timeout failures and assert live notifications complete within one observer-timeout window.
- Verify `Dead`, `Stopping`, and `ShuttingDown` generations can be cleaned up, while `Active`, `Joining`, `Created`, and unknown generations are retained.
- Verify a newer generation at the same endpoint permits cleanup of its superseded generation.
- Verify many confirmed-dead observers are removed with one persistence callback per fanout.

## Run the test
```
DOTNET_CLI_HOME=/tmp/dotnet dotnet test
```

## Notes
- The Orleans testing API uses `TestCluster`, which is an in-process cluster. Orleans 10 does not expose a public type literally named `InProcessTestCluster`, but the test setup is in-process and equivalent.
