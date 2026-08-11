# Test-Suite Anti-Patterns

Use these symptoms to diagnose value and coupling. Do not rewrite or delete a test merely because it matches a label; first recover the behavior it was intended to protect.

| Symptom | Likely problem | Preferred response |
| --- | --- | --- |
| High coverage but regressions escape | Assertions are weak, cases are trivial, or important behavior is missing | Trace important behaviors and realistic failure modes; inspect suspicious survivors rather than raising the target |
| Harmless refactors break many tests | Tests assert internal structure or interactions | Reassert public outputs or state; move mocks to true system boundaries |
| One mock per constructor argument | London-style isolation has replaced behavioral isolation | Use real in-process collaborators and test a coherent unit of behavior |
| Private methods are tested directly | The test is coupled to decomposition | Test through the public contract; extract a genuine concept only when production design benefits |
| Private state is exposed for assertions | Tests gain privileges production clients do not have | Assert the operation or state the real client can observe |
| Production members exist only for tests | Test-induced design damage | Remove the test-only surface and introduce only authentic boundaries or domain abstractions |
| Expected values reimplement the production algorithm | The test repeats the same mistake and breaks with every algorithm refactor | Use independently derived, hard-coded examples or an external oracle |
| Production code contains an `isTest` switch | Test paths pollute runtime behavior and add bug surface | Keep test implementations in test code; inject only a genuine boundary when substitution is required |
| Huge shared fixture | Tests depend on irrelevant data and hidden defaults | Build the minimum valid state locally with focused builders |
| Constructor or setup fixture supplies scenario data | Test context is hidden and one fixture edit changes many tests | Move behavior-relevant setup into each test; share only universal infrastructure |
| A test contains branches | One test represents several possible stories | Split it into deterministic, named cases |
| A unit test contains several act/assert stages | Several behaviors are bundled to save code | Split the behaviors; combine stages only as an explicit slow-integration optimization |
| Test names repeat class, method, or assertion formulas | Tests enumerate code structure or become stale comments instead of navigation aids | State the stable business scenario in domain language; add an outcome only when needed to disambiguate |
| Tests depend on order or shared residue | Isolation and cleanup are broken | Give each test unique state and explicit lifecycle ownership |
| Exact internal call counts are asserted | Incidental orchestration is treated as a contract | Verify the final result; retain counts only for idempotency, retry, billing, or protocol semantics |
| Logs are the primary assertion | Diagnostic implementation is mistaken for behavior | Assert the business outcome or external signal unless the log/audit record is a contractual output |
| Every domain case is exercised through UI or deployment | Feedback is slow and failures are ambiguous | Move deterministic cases down; retain a small wiring journey |
| In-memory persistence is the only database evidence | The substitute erases production semantics | Add focused tests against the real database engine |
| Repositories are tested in isolation from the use case | Integration cost buys only thin layer coverage | Exercise mappings and persistence through the application path; extract and unit-test any real algorithm |
| Flaky tests are retried until green | Nondeterminism or environmental races remain | Reproduce, control time/state, add completion signals, and fix the cause |
| A new test was never seen fail | The assertion may not detect the intended defect | Reverse or mutate the behavior, reproduce the regression, or document alternate evidence |
| Snapshot changes are accepted wholesale | Review has become approval of noise | Use focused semantic assertions or review each intentional contract change |
| Static time, logger, or service context is swapped in tests | Hidden shared state couples tests and production code | Inject the dependency at the edge and pass plain values inward |
| A concrete class is partially mocked | Logic and external communication share one responsibility | Split the deterministic logic from the gateway and mock only the owned boundary |
| Every class has a single-implementation interface | Mockability or speculative flexibility has become architecture | Keep in-process and managed dependencies concrete; introduce interfaces for real substitution or unmanaged boundaries |
| Every branch or precondition gets a dedicated test | Coverage substitutes for domain significance | Test important invariants; let trivial safeguards and fail-fast wiring remain indirect |

## Review a Suite by Behavior

1. List the important business behaviors and boundaries.
2. Map tests to the distinct regression each protects.
3. Find duplicate coverage that adds no new risk evidence.
4. Find important behaviors with weak or absent coverage.
5. Score brittle tests on regression protection, refactoring resistance, feedback, and maintainability.
6. Keep, rewrite, move to another level, or remove each test deliberately.

Do not preserve a low-value test solely because it exists. Do not delete it solely because it is inconvenient. Preserve valuable behavior coverage before removing redundant or harmful code.

## Watch Production-Code Responses

Difficulty testing is useful negative evidence of coupling, hidden inputs, or mixed responsibilities. It does not justify:

- interfaces for every class;
- service locators or mutable globals;
- public setters that violate invariants;
- weakened encapsulation;
- domain logic moved into mock setup;
- arbitrary layers created only to satisfy a test framework.

Improve the production model around real responsibilities and boundaries, then test through its stable contract.
