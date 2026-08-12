# Integration, Database, and End-to-End Testing

Use integration tests to gain confidence in behavior that depends on real technology, configuration, or application assembly. Do not turn them into slow replicas of every unit test.

## Define the Integration Boundary

State which risk the test includes:

- database schema, mappings, constraints, transactions, or queries;
- filesystem or object-storage behavior;
- serialization and message envelopes;
- dependency injection, middleware, routing, authentication, or hosted pipelines;
- queue, cache, search engine, or other infrastructure semantics;
- an adapter's protocol behavior.

Start only the components required for that risk.

Use the real instance of a managed dependency the application controls, such as its private database. At the last application-owned edge of an unmanaged dependency, stub query results that provide external input and mock or spy only contract-significant outbound commands. If only part of a dependency is externally shared, treat that surface as unmanaged and the private remainder as managed.

## Test Databases Faithfully

- Prefer the same database engine and relevant configuration used in production.
- Keep schema and immutable reference data under source control. Apply the real migration history or production schema-creation path; do not silently repair already-released migrations when a new corrective migration is required.
- Use unique data per test and deterministic identifiers where useful.
- Isolate tests with a strategy compatible with the application: explicit cleanup, disposable databases or schemas, or transactions only when background work and multiple connections cannot escape them.
- Run safely in parallel or disable parallelism explicitly for the affected collection; do not let accidental contention define the suite.
- Give arrange, act, and assert separate connections, transactions, or units of work. Assert through a fresh read so an ORM cache or tracked input object cannot masquerade as persisted evidence.
- Assert persisted business outcomes, atomicity, and important constraints, not ORM or repository implementation details.

An in-memory provider may be useful for a deliberately simplified fake, but it does not validate relational semantics or provider-specific queries. It must never be the sole database evidence when production fidelity is the reason for the test.

For mutating use cases with a credible partial-commit risk, verify that the related updates commit or roll back as one business operation. Trigger failure through authentic state, a real constraint, or an existing production seam; do not add a production-only-for-tests fault switch. Avoid an outer test transaction when it changes connection, commit, worker, or concurrency behavior relative to production. Prefer cleanup before a test or an isolated disposable scope; preserve immutable reference data.

## Keep Fixtures Intentional

Create the minimum valid state that preserves the behavior's causal details. Prefer builders or named fixture factories over a giant shared object graph. Make ownership and cleanup obvious. Avoid suite-wide mutable fixtures whose order or residue changes results.

Use authentic regression data when formatting, ordering, precision, encoding, schema evolution, or serialization details caused the defect. Reduce unrelated noise without removing the causal feature.

## Cover Application Paths Economically

For an important use case, a focused integration test can exercise the application entry point, real managed dependencies, and boundary adapters while replacing unmanaged external systems. Put the broad matrix of deterministic domain edge cases in unit tests. Add integration cases for materially different infrastructure failure modes, not for every input combination.

Prioritize writes because persistence mistakes can corrupt durable or external state. Test reads when their query semantics or business importance justify the cost. Exercise repositories and event dispatchers through the overarching use-case test; test them separately only when they contain independently valuable complexity that cannot first be extracted into a collaborator-free algorithm.

## Test Messaging and Asynchrony

- Separate the decision to publish from transport delivery where the architecture permits, for example with an outbox.
- Assert durable state synchronously when that is the owned contract.
- Test transport adapters against the real broker or a faithful environment when broker semantics matter.
- Await every asynchronous operation the test starts and surface host or worker failures. Do not use fire-and-forget work or `async void` test actions.
- Re-evaluate the observable assertion when meaningful system progress occurs, or poll that assertion under a bounded deadline.
- Apply a deadline or cancellation to both signal-driven and polling waits, and report the last observable state on timeout.
- Do not use fixed sleeps or an internal render, write, or message count as proof of completion; those are timing and implementation details.
- Retry the condition or assertion, not the whole test. Do not hide race conditions with blanket retries.

## Use End-to-End Tests Sparingly

Add an end-to-end test when the protected risk depends on the assembled system, such as a critical browser journey, deployed routing, identity configuration, or cross-component wiring. Keep it focused on stable user-visible outcomes.

Do not use end-to-end tests to enumerate domain rules, verify cosmetic DOM structure, or replace contract tests. A healthy end-to-end suite is small enough that failures receive immediate attention.

Keep one behavioral scenario per test by default. A natural journey may require many browser or API actions. Combine otherwise separable scenarios only when a hard-to-control external environment makes separate end-to-end setup materially more expensive; document the diagnostic trade-off.

## Treat Logging by Audience

- Treat support, audit, or compliance records consumed outside development as observable behavior. Express them in domain language and verify the boundary signal.
- Treat diagnostic logs used only by developers as implementation detail. Do not assert them, and avoid allowing diagnostic noise to obscure domain logic.
- Inject logging dependencies explicitly. Do not use ambient static access to hide excessive logging or layer complexity.

## Keep External Live Tests Separate

Tests against third-party sandboxes or live services are valuable for compatibility and smoke evidence but may be slow, costly, rate-limited, or unavailable. Mark and schedule them explicitly. Never let a skipped live dependency silently turn into a passing compatibility claim.

## Diagnose Environmental Failures Honestly

Distinguish behavior failures from unavailable containers, credentials, ports, migrations, or network access. Report what did and did not run. Do not weaken assertions or replace the real dependency merely to make the suite green.
