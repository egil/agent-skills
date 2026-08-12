# Test Doubles and Boundaries

Add a test double only after identifying what boundary it represents and why a real dependency would make the test less useful.

## Use Precise Terms

- A **stub** supplies an input to the system under test. Do not verify how it was called.
- A **mock** or **spy** records an outbound interaction that the test verifies.
- A **fake** is a working but simplified implementation, such as an in-memory store.

The role matters more than the label; confusing input setup with output verification produces overspecified tests. Use classical isolation: isolate tests from one another, not every class from its in-process collaborators. A large arrangement graph is design feedback that mocking every node hides.

Distinguish dependency properties when deciding how to test:

- A **shared dependency** lets tests affect one another through mutable shared state.
- A **private dependency** is newly owned by one test and cannot carry interference from another.
- A **volatile dependency** requires non-default environment setup or produces nondeterministic results.
- An **out-of-process dependency** may be shared or private; classify its ownership and observability separately.

## Classify the Dependency

### In-process collaborator

Use the real object by default. Mocking internal classes or layers makes tests mirror the implementation graph and fail during harmless refactors.

Treat a hard-to-construct, nondeterministic, or materially slow collaborator as design feedback first: expose hidden inputs, split responsibilities, or add a genuine seam.

Use a stub or fake only when the real collaborator would still make the test less valuable. Assert observable output or state, state what regression evidence the substitution removes, and cover those semantics at another appropriate level.

### Managed dependency

This is external to the process but belongs to the application's persistent state or operational boundary, such as its database. Exercise it with focused integration tests when its real semantics matter. An in-memory replacement is not proof of relational constraints, transactions, provider queries, or production serialization.

Treat communication with an application-exclusive managed dependency as an implementation detail. Do not mock SQL, repository calls, or storage interaction merely to freeze that communication pattern.

### Unmanaged dependency

This is controlled outside the application, such as a third-party API, payment processor, email gateway, or another independently owned service. Place a narrow adapter at the boundary. In application tests, mock the final outbound command when that communication is an observable result. Add contract or sandbox coverage separately when compatibility is important.

### Nondeterministic input

Represent time, randomness, generated identifiers, and similar inputs through an explicit value or a small production seam when behavior depends on them. Do not abstract them merely because they appear in the code.

## Decide Before Mocking

Ask in order:

1. Is the interaction itself observable to a system consumer or external collaborator?
2. Can the same behavior be verified more stably through an output or state change?
3. Does using the real in-process collaborator remain fast and deterministic?
4. Is the double placed at the last boundary owned by this application?
5. Would a behavior-preserving internal refactor leave the expectation intact?

If the answer to the final question is no, redesign the assertion or move the seam outward.

A mock is legitimate only when it verifies an outbound communication that crosses the application boundary and whose side effect is externally observable. An internal collaboration remains an implementation detail even when both members are public. Likewise, publicity alone does not make a member observable: it must directly help the relevant client achieve a goal.

For example, do not prove checkout by mocking `OrderRepository.Save` and verifying the internal call. Assert the saved order through the real managed store. When issuing a payment is itself the outcome, spy on the application-owned `PaymentGateway` command and test its provider adapter separately.

Use outbound mocks or spies in application/controller tests, not domain unit tests. A fast controller test with all I/O replaced may technically meet the classical unit-test definition; do not mistake that label for real boundary evidence. The number of boundary doubles is determined by the unmanaged dependencies participating in the behavior; a one-mock-per-test quota has no value.

## Commands and Queries

- Stub queries that provide external input.
- Verify outbound commands only when sending them is part of the contract.
- Do not verify that a stubbed query was called unless call behavior has independent significance, such as a contractual cache or quota rule.
- At an unmanaged boundary, prove both the expected contract-significant calls and the absence of unexpected contract-significant calls on that boundary. Verify an exact count or order when the external contract requires it, including single delivery, idempotency, retries, billing, or ordered protocols—not for internal collaborations or unrelated diagnostic traffic.

## Keep Ports Narrow

Define an interface around the application's need, not around an entire vendor SDK. Return domain-relevant values and accept domain-relevant commands. Keep protocol translation inside the adapter.

Place the observation point below the translation the test intends to cover:

- For an application behavior test, spy on the domain-facing gateway port and test the adapter separately.
- For a local adapter test, run the real adapter and capture its final request, message, or SDK command at a transport sink.
- For provider compatibility, use provider-owned artifacts, verification, or a faithful sandbox; a captured local request is not enough.

Prefer a small handwritten fake or spy when many tests otherwise repeat the same stable boundary semantics. Give it domain-named behavior and centralize those test-side semantics there. A generic mocking library remains useful for a narrow, one-off setup or outbound expectation; moving repeated mock-framework setup into a generic helper only hides the coupling.

Observe a fake through the same application or port operations available to the real client whenever possible. Do not expose its internal collection or counters solely to make assertions convenient unless that state is itself part of the boundary contract. Do not use production translation code to calculate the expected message.

Only mock types the application owns. Wrap a third-party SDK behind a narrow adapter in the application's language, then verify that adapter's externally visible command and cover SDK compatibility separately.

Do not create one interface per class to satisfy a mocking framework or claim loose coupling. A single-implementation interface needs a concrete boundary or substitution purpose; managed dependencies and in-process domain classes normally remain concrete.

When practical, move decisions into a deterministic core that consumes values and returns values or explicit side-effect instructions. Keep the shell responsible for gathering inputs and applying those instructions. Apply this strategically: do not accept material performance cost or architecture growth merely to turn every test into an output-based test.

## Treat Fakes as Production Code

Fakes can drift from the real implementation and can accidentally erase important behavior. Use them when their reduced semantics are intentional and test those semantics against the real boundary where practical. Do not use an in-memory fake as the sole evidence for database, queue, concurrency, or provider behavior.

## Common Warning Signs

- Nearly every constructor dependency is mocked, and setup is longer than the behavior.
- Tests verify chains of internal calls and break when logic moves between classes.
- Production code exposes internals or needs partial concrete mocks solely for the mocking framework.
- A hand-written provider mock is treated as compatibility proof.

When these appear, recover the observable contract, use real in-process collaborators, and move doubles to actual system boundaries. If a partial concrete mock seems necessary, split decision logic from the gateway responsibility instead.
