# Orleans Async Assertion Sample

This sample demonstrates a deterministic `WaitForAssertionAsync` pattern for Orleans tests using per-subscriber channels and an `IIncomingGrainCallFilter`. Instead of polling with `Task.Delay` or coupling tests to specific RPC sequences, the tests use _any_ grain call to the target grain as a retry trigger. The assertion itself defines the contract.

## Architecture

- **`GrainCallCollectionFilter`** — `IIncomingGrainCallFilter` that broadcasts `GrainCallTriggerEvent`s to per-subscriber bounded channels. Each concurrent `WaitForAssertionAsync` call gets its own channel — no contention between readers.
- **`IGrainCallCollectorProvider`** — Interface extending `IGrainFactory` that test fixtures implement. Exposes `CallCollector` and `WaitForAssertionAsyncTimeout`.
- **`GrainCallCollectorProviderExtensions`** — C# 14 extension methods on `IGrainCallCollectorProvider` providing `WaitForAssertionAsync` overloads (resolved grain, id-types matrix, custom trigger, return values).
- **`SiloFixture`** — Sample `IClassFixture`/`ICollectionFixture` that implements `IGrainCallCollectorProvider`, creating the test cluster and registering the filter.
- **`RequestContextScope`** — Prevents assertion calls from being recorded as grain call activity.

## What the tests prove

- Regular RPC calls trigger assertion retries through the grain call filter.
- One-way RPC calls produce deterministic triggers via the incoming grain call filter.
- Stream deliveries go through the grain call filter on the consumer grain.
- Parallel waits use unique grain IDs and per-subscriber channels to avoid cross-test interference.
- Value-returning assertions propagate the return value.
- Id-type overloads resolve grains internally.
- Custom trigger predicates fire on arbitrary grain calls.
- The channel-exhaustion path retries correctly after the channel is drained.

## Run the tests

```
dotnet test
```
