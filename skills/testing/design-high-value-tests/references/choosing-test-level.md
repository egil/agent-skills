# Choosing the Test Level

Choose a test level from the risk being addressed, not from a preferred pyramid shape or the name of the production type.

A unit test verifies one cohesive unit of behavior, runs quickly, and is isolated from other tests. It may exercise several real in-process classes. Reserve the integration label for real boundary or composition evidence, use more specific labels such as component or contract when helpful, and treat a merely slow in-memory test according to its actual feedback cost. Classification is secondary to honest evidence and pipeline placement.

## Decision Sequence

1. Name the observable failure that matters.
2. Identify the lowest boundary at which that failure can be detected faithfully.
3. Ask whether replacing any dependency would remove the behavior or compatibility risk being tested.
4. Choose the fastest level that retains that risk.
5. Add a higher-level test only for a distinct assembly, configuration, protocol, or deployment risk.

## Level Guide

| Level | Best fit | Include | Avoid |
| --- | --- | --- | --- |
| Unit | Business rules, calculations, state transitions, policies | Real in-process collaborators; deterministic inputs | I/O, shared state, framework bootstrapping, assertions about internal calls |
| Component/application | A UI component or locally assembled application slice | Real local composition; outer I/O replaced at explicit boundaries | Cosmetic DOM or internal-layer assertions; claims about a replaced provider |
| Integration | Persistence, serialization, dependency injection, hosted pipelines, framework behavior | Real application wiring and real managed dependencies where practical | Replacing the very technology whose behavior creates the risk |
| Contract | Protocol shape or semantics across independently released boundaries | Provider artifacts, protocol fixtures, schema validation, or a faithful sandbox | Treating a hand-written mock as proof of provider compatibility |
| End-to-end | A critical journey whose risk emerges only in the assembled or deployed system | The smallest path across real components needed to demonstrate the journey | Exhaustive edge-case matrices, fragile UI detail assertions, duplication of domain cases |

Classify a test by what it actually executes, not its folder. A test that starts no process but accesses a real database is an integration test. A test that constructs several domain objects in memory may still be a unit test.

Treat end-to-end as the broadest subset of integration testing: it exercises all or most out-of-process dependencies from a user-facing entry point. The boundary is intentionally approximate; even a valuable end-to-end test may replace a dependency that cannot be controlled safely.

## Shape the Portfolio

- Put most domain edge cases at the unit level.
- For each important use case, cover one cohesive successful local operation, including its managed durable state when present. Add integration edge cases only when lower-level tests cannot reproduce the risk.
- Exercise protocol compatibility at adapter boundaries when provider drift is plausible.
- Reserve end-to-end coverage for a small set of critical journeys and deployment checks.
- Avoid duplicating identical assertions across levels; let each level own a distinct risk.

Treat the test pyramid as cost guidance, not a quota. The appropriate shape depends on where the application's logic, integration complexity, and failure cost live. A mostly CRUD application may justifiably have as many integration tests as unit tests and no deployed end-to-end suite; a simple API with one external dependency may make hosted end-to-end and integration tests nearly indistinguishable.

Set a higher admission bar for integration tests than unit tests: slower feedback and harder operation must buy materially more regression protection. Prefer fail-fast guards over an integration case when a missing orchestration step would immediately stop the operation before persistent corruption and the invariant itself is already unit-tested.

Treat asynchronous acceptance and later dispatch as separate durable operations. An endpoint-to-outbox integration test and an outbox-to-provider-adapter test may each be complete without one test spanning both.

## Escalate Deliberately

Move upward only when a lower level cannot establish confidence in something important, for example:

- database constraints, transactions, mappings, or query semantics;
- serialization or transport configuration;
- dependency-injection and middleware composition;
- provider-consumer compatibility;
- routing, authentication, browser behavior, or deployment configuration.
- framework or SDK upgrade behavior that narrow tests bypass because they do not execute the upgraded surface.

Move downward when the higher-level test merely enumerates deterministic business cases. Extract the decision into a testable core and retain one higher-level wiring example if needed.

Do not create a nominal integration suite that replaces every out-of-process dependency and merely verifies repository or layer calls. If the real managed dependency cannot be exercised, keep the domain suite strong and state the missing integration evidence explicitly.

## Handle Cross-Service Behavior

Do not use a full distributed end-to-end environment as the first defense against service drift. Separate local adapter, compatibility, and deployed-journey evidence. Escalate further only when an important risk truly emerges from multi-service coordination and cannot be represented faithfully below it.
