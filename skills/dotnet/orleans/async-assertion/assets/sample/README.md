# Orleans Async Assertion Sample

This sample demonstrates the deterministic `WaitForAssertionAsync` pattern for Orleans tests using the
[`Egil.Orleans.Testing`](https://www.nuget.org/packages/Egil.Orleans.Testing) package. Instead of polling with
`Task.Delay` or coupling tests to specific RPC sequences, assertions are retried whenever the package's
`GrainActivityCollector` observes grain activity. The assertion itself defines the contract.

## Architecture

- **`GrainActivityCollector`** (package) — observes incoming grain calls and, when enabled, storage operations in the silo.
- **`AddGrainActivityCollector`** (package) — silo builder extension that registers the collector and its grain call filter, and exposes `CollectStorageActivityFromDefault()` for storage observation.
- **`WaitForAssertionAsync`** (package) — extension methods on `IGrainActivityWaiter` that retry an assertion on observed activity, optionally scoped to a single grain.
- **`SiloFixture`** — sample `IClassFixture`/`ICollectionFixture` that owns the test cluster and collector and implements `IGrainActivityWaiter`, so tests call `fixture.WaitForAssertionAsync(...)` directly.

## What the tests prove

- Regular RPC calls trigger assertion retries.
- One-way RPC calls produce deterministic triggers.
- Stream deliveries trigger retries through the grain call filter on the consumer grain.
- Parallel waits use unique grain IDs so grain-scoped waits do not interfere.
- Value-returning assertions propagate the return value.
- The overload without a grain scope retries on any observed activity.
- A never-succeeding assertion fails with `WaitForAssertionTimeoutException` carrying the last assertion failure.
- The `GetGrainCallsAsync` feed can be observed directly when individual events are needed.

## Run the tests

```
dotnet test
```
