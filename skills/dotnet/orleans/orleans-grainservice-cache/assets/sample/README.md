# Orleans Grain Service Subscription Sample

This sample validates that an Orleans grain can persist references to `GrainService` subscribers and continue notifying them after the grain deactivates and later reactivates. Each silo hosts a `CacheGrainService` that subscribes once at startup. The grain stores the service references in its persisted state and uses Orleans `ObserverManager` for fan-out, so updates after reactivation still reach all services without resubscription. The tests await Orleans RPC telemetry spans via `IAsyncEnumerable<Activity>` instead of polling with `Task.Delay`.

## What the test proves
- A grain service on **each silo** subscribes once and keeps an in-memory cache.
- The grain persists the service references.
- After the grain deactivates, updating its state still notifies **all** grain services, demonstrating that stored service references remain valid.

## Run the test
```
DOTNET_CLI_HOME=/tmp/dotnet dotnet test
```

## Notes
- The Orleans testing API uses `TestCluster`, which is an in-process cluster. Orleans 10 does not expose a public type literally named `InProcessTestCluster`, but the test setup is in-process and equivalent.
