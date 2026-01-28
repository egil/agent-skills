# Orleans Async Assertion Sample

This sample demonstrates a deterministic `WaitForAssertion` pattern for Orleans tests by replaying OpenTelemetry activities. The tests re-run assertions when matching spans complete, avoiding `Task.Delay` polling. The sample covers regular RPC calls, one-way RPC calls, and stream deliveries. Parallel waits are executed in the same test to verify isolation across concurrent operations. The fixture exposes ergonomic helpers so tests do not need to manage `RequestContext` directly.

## What the tests prove
- Regular RPC calls (`Add`) emit trace activities that can trigger an assertion retry.
- One-way RPC calls (`AddOneWay`) still produce deterministic trace triggers.
- Stream deliveries emit activities from the consumer grain so tests can await stream completion.
- Parallel waits use `test.id` correlation to avoid cross-test interference.

## Run the tests
```
DOTNET_CLI_HOME=/tmp/dotnet dotnet test
```
