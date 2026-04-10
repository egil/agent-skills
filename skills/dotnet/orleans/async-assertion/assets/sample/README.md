# Orleans Async Assertion Sample

This sample demonstrates a deterministic `WaitForAssertion` pattern for Orleans tests using a shared grain call log with per-test marker tracking. Instead of polling with `Task.Delay` or coupling tests to specific RPC sequences, the tests use _any_ grain call to the target grain as a retry trigger. The assertion itself defines the contract.

## Architecture

- **`GrainCallCollectionFilter`** — `IIncomingGrainCallFilter` that records grain IDs into a shared append-only log. Per-test markers enable automatic pruning.
- **`CollectGrainCallsAttribute`** — xUnit `BeforeAfterTestAttribute` that registers/unregisters each test with the filter.
- **`AsyncAssertionTestBase`** — Optional base class that applies `[CollectGrainCalls]` and wires up the fixture.
- **`WaitForAssertionAsync`** — Extension method that scans the log for matching grain calls and retries the assertion.
- **`RequestContextScope`** — Prevents assertion calls from being recorded as grain call activity.

## What the tests prove

- Regular RPC calls trigger assertion retries through the grain call filter.
- One-way RPC calls produce deterministic triggers via the incoming grain call filter.
- Stream deliveries go through the grain call filter on the consumer grain.
- Parallel waits use unique grain IDs and per-test markers to avoid cross-test interference.

## Run the tests

```
dotnet test
```
