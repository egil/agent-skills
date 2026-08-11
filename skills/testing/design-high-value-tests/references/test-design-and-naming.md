# Test Design, Fixtures, and Naming

Make each test explain a behavior while minimizing the amount of test code a maintainer must understand.

## Structure the Test

Use Arrange-Act-Assert when it improves readability:

- **Arrange:** establish only inputs and state relevant to the behavior.
- **Act:** perform one public operation or coherent user action.
- **Assert:** verify one behavioral outcome, which may require several related assertions.

Keep one act for one unit of behavior. Multiple act/assert sequences usually belong in separate tests; combine sequential stages only in an already-slow integration test when each stage naturally prepares the next and the diagnostic trade-off is worthwhile. Do not branch inside any test. Loops and helper calls must not obscure which case or decisive setup failed.

An act section that needs several calls for one business operation is negative design evidence: the public API may expose a protocol that lets clients violate an invariant. Prefer one encapsulated operation where the domain permits it. Multiple related assertions are fine when they describe the complete outcome of that one operation.

Separate obvious AAA sections with blank lines. Add section comments only when larger setup or assertion blocks need internal spacing. In test-first work, outlining the expected assertion before arranging inputs can clarify the intended contract.

## Name the Behavior

Name tests as plain-language references to the business scenario, not the production method's implementation. Start with:

`<business scenario>`

Add a stable outcome only when it is needed to distinguish behaviors that otherwise have the same scenario:

`<scenario>_<stable outcome>`

For example, prefer `Expired_subscription_cannot_be_renewed` over `RenewAsync_ExpiredSubscription_ReturnsFalse`.

- Keep names true when classes or methods are reorganized.
- Keep the decisive outcome clear in the body; do not copy assertion detail into a long name that can become stale.
- Avoid rigid `Method_Scenario_Result` templates. Omit the method name unless a low-level utility method is itself the useful contract.
- State the fact directly instead of adding wishful `should` wording or mechanical `Given_When_Then` noise.
- Follow explicit repository instructions. Otherwise preserve repository formatting and casing only when compatible with scenario-first names.

## Choose Assertions

- Assert through the public contract.
- Prefer the most meaningful complete object or value comparison available.
- Keep several assertions together when they describe one atomic outcome.
- Split the test when failures would represent different behaviors or require different arrangements.
- Use a small domain-named assertion helper when it keeps the test at one abstraction level. Keep the decisive outcome apparent at the call site and preserve useful failure diagnostics.
- Avoid assertions that merely repeat mock setup, constructors, language behavior, or framework guarantees.
- Compute expected values independently of the production algorithm. Hard-code representative results derived from a specification, domain expert, trusted oracle, or preserved legacy behavior; do not copy the implementation into the test.
- Include failure messages only when the assertion output would otherwise be ambiguous.

## Build Test Data

- Make behavior-relevant values explicit at the test site.
- Put irrelevant valid defaults in a builder or factory with a descriptive name.
- Prefer local factory calls whose parameters expose every behavior-relevant value. Shared constructor or setup fixtures hide context and couple otherwise independent tests; reserve shared setup for truly universal infrastructure such as an integration-test database connection.
- Avoid a single canonical fixture reused across unrelated behaviors.
- Do not call production defaults when the default itself is part of the behavior under test.
- Preserve authentic values that caused a regression, including ordering, precision, casing, encoding, timing, or serialization shape.

Test builders are test code: keep them small, deterministic, and unsurprising.

Keep scenario-specific factories close to their tests and parameter-light. Promote a configurable object graph to a builder only when that vocabulary is reused coherently. Prefer modest duplication to a generic helper or builder whose options hide which values matter to the scenario.

## Use Snapshots Selectively

Use a snapshot when the whole stable artifact is a contract worth reviewing, or as secondary broad-diff evidence around a focused behavioral assertion.

- Keep the decisive semantic assertion in the test when only part of the artifact matters.
- Snapshot a deliberate projection rather than an incidental object graph. Normalize nondeterministic noise and exclude secrets, tokens, and unnecessary personal data.
- Include only enough relevant input or expected context to make the diff reviewable without reconstructing the scenario elsewhere.
- Review every changed field. Never accept a large snapshot update merely to make the suite green.
- Narrow or remove a snapshot that repeatedly changes during behavior-preserving refactors.

## Parameterize Deliberately

Use parameterized tests when the same rule and assertion apply to several meaningful cases. Give cases readable labels where the framework supports them. Parameterization trades code volume for descriptive names: split positive and negative cases when the data does not make their meaning self-evident, and use separate named tests when the behavior is too complex to read as a table.

Use property-based testing for broad invariants and input spaces when generated counterexamples remain understandable. Retain a discovered counterexample as a named regression example when it documents an important defect.

## Control Nondeterminism

Supply time, randomness, identifiers, culture, locale, and scheduling explicitly when they affect behavior. Avoid ambient global state. Do not freeze or abstract nondeterminism that has no bearing on the assertion.

Prefer passing the resolved time or other nondeterministic result as a plain value into domain logic. When a service is necessary at the application edge, resolve it once there and pass its value inward.

## Keep Tests Independent

Tests must be independent in execution and modification. They must not require execution order, residue from another test, wall-clock timing, a developer's machine state, or a shared fixture change that silently alters unrelated scenarios. Make required environment explicit and fail clearly when it is absent.
