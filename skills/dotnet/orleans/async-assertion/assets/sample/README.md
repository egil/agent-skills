# Orleans Async Assertion Sample

This sample demonstrates a deterministic `WaitForAssertion` pattern for Orleans tests using an `IIncomingGrainCallFilter` and `Channel<T>`. Instead of polling with `Task.Delay`, the tests listen for completed grain calls and re-run assertions when a matching call completes. The sample covers regular RPC calls, one-way RPC calls, and stream deliveries.

## What the tests prove
- Regular RPC calls (`Add`) trigger assertion retries through the grain call filter.
- One-way RPC calls (`AddOneWay`) still produce deterministic triggers via the incoming grain call filter.
- Stream deliveries go through the grain call filter on the consumer grain, so stream-based assertions work without extra instrumentation.
- Parallel waits use unique grain IDs to avoid cross-test interference.

## Run the tests
```
dotnet test
```
