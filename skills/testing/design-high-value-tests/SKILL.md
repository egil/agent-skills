---
name: design-high-value-tests
description: Design and implement maintainable test strategies and production-code seams for unit, integration, contract, and end-to-end tests. Use when adding or changing behavior, fixing a bug with regression coverage, practicing TDD, deciding test level, choosing mocks or fakes, testing databases or external systems, reviewing or refactoring tests, evaluating coverage, or changing production code to improve testability. Do not use for merely running an already-specified test command when no test-design judgment is needed.
---

# Design High-Value Tests

## Purpose

Treat tests as owned code whose purpose is to enable sustainable change. Protect important observable behavior at the lowest total cost without coupling tests to implementation details. Optimize for confidence per unit of maintenance, not test count or coverage percentage. Test code is a liability as well as a safety net: keep only tests whose regression value comfortably exceeds their cost to understand, run, and change.

Honor the user's requested workflow and the repository's instructions. Do not impose test-first implementation when the task is read-only or the user explicitly requests another sequence.

## Load References Selectively

Start with this core workflow. Read only references that own a decision the task actually requires; usually one or two:

| Task decision | Read |
| --- | --- |
| The appropriate level is unclear, or the portfolio spans levels | [choosing-test-level.md](references/choosing-test-level.md) |
| Add or change a double, port, adapter, or nondeterministic seam | [test-doubles-and-boundaries.md](references/test-doubles-and-boundaries.md) |
| Exercise a database, file, queue, host, log sink, or deployed environment | [integration-and-databases.md](references/integration-and-databases.md) |
| Verify an independently released service or cross-service journey | [contract-and-cross-service.md](references/contract-and-cross-service.md) |
| Choose fixture or builder shape, parameterization, property testing, snapshots, custom assertion helpers, or naming mechanics | [test-design-and-naming.md](references/test-design-and-naming.md) |
| Diagnose or refactor a low-value suite | [anti-patterns.md](references/anti-patterns.md) |
| Choose .NET-specific runner or filtering, host or fixture, EF context, `TimeProvider`, Orleans, or mutation-testing mechanics | [dotnet-testing.md](references/dotnet-testing.md) |

Do not load a reference just because the example happens to use its language or mentions an unchanged dependency.

## Follow the Workflow

### 1. State the protected behavior

Before designing or editing, state in one sentence:

`When <observable scenario>, the system <observable outcome>.`

For a bug, also state the regression mechanism: what incorrect behavior would return if the production change were reverted? If neither statement is clear, inspect requirements and production behavior before designing the test.

Phrase the behavior as a fact or story a domain-aware non-programmer could recognize. Treat a unit as one cohesive behavior, regardless of how many in-process classes implement it.

### 2. Map the behavior and boundaries

Identify:

- the domain decision or invariant;
- the application orchestration around that decision;
- managed dependencies the application owns as part of its state, such as its database;
- unmanaged systems outside the application's control, such as another organization's API;
- nondeterministic inputs such as time, randomness, generated identifiers, scheduling, or concurrency;
- the user-visible or consumer-visible outcome.

Prefer a functional core with an imperative shell when it naturally fits: keep decisions deterministic and put side effects at explicit boundaries. Do not introduce test-only production APIs or abstractions without a genuine design role.

Classify the production code before investing in tests:

- concentrate unit coverage on domain-significant or algorithmically complex code with few collaborators;
- cover thin controllers through a small number of integration paths;
- leave trivial code to indirect coverage;
- split code that is both decision-heavy and collaborator-heavy into a deep decision component and a humble orchestration component.

Prefer fewer collaborators as a component's importance or decision complexity grows. Refactor when concrete coupling symptoms justify it; do not reshape an otherwise coherent model to satisfy a numeric rule.

### 3. Choose the narrowest sufficient test level

Choose by risk and dependency boundary, not by class or method:

- Use a unit test for important deterministic business behavior that can run without I/O or shared mutable state.
- Use an integration test when confidence depends on real application wiring, serialization, persistence, framework configuration, or a managed dependency.
- Use a contract test when compatibility with a separately evolving provider or consumer is the risk.
- Use an end-to-end test only when the critical risk exists across the assembled system and cannot be covered more cheaply below it.

Do not repeat the same assertion mechanically at every level. Concentrate fast cases around domain rules, use focused integration coverage at boundaries, and keep deployed-path checks sparse.

### 4. Design around observable outcomes

Prefer, when equally expressive:

1. output-based verification;
2. state-based verification;
3. communication-based verification.

Assert through observable behavior: an operation or state that directly helps the current client achieve a goal.

- Public does not automatically mean observable; a public implementation detail is still the wrong target.
- Avoid private methods, internal call order, collaborator graphs, incidental log messages, ORM mechanics, and exact call counts unless they are part of the contract.
- Let a unit include real in-process collaborators. Isolation means independence from other tests, I/O, and shared state, not one mocked class at a time.

Write tests from the outside as black-box specifications. Use source structure, coverage, and mutation results only as white-box analysis tools for discovering cases; do not turn those internal paths into the asserted contract.

### 5. Establish meaningful red evidence

When changing code, make the proposed test fail for the expected behavioral reason before relying on it. Distinguish a meaningful red result from compilation errors, broken setup, missing infrastructure, or an assertion that fails before reaching the behavior. Confirm the failure reaches the intended action and points to the protected outcome. If reproducing the old behavior is impractical, document alternate evidence such as a mutation, temporary reversal, or historical failing fixture.

For an existing regression that is already red, reproduce it with the smallest authentic fixture that retains the causal detail. Do not weaken the fixture merely to make the test easy to write.

### 6. Make the smallest coherent production change

Implement enough production behavior to satisfy the contract. Keep domain decisions out of mocks and test fixtures. If the test is difficult to write, treat that as negative design evidence and inspect coupling, hidden inputs, and mixed responsibilities; do not assume testability alone proves the resulting design is good. Prefer the Humble Object shape when framework or I/O code contains decisions: extract the important logic and leave a thin adapter or controller.

### 7. Refactor both sides

Remove duplication and improve names while preserving observable behavior. A behavior-preserving production refactor should normally leave the test unchanged. If it does not, identify whether the public contract actually changed or the test knows too much.

### 8. Apply the quality gate

Evaluate every retained test on four dimensions:

- **Regression protection:** Would a realistic defect in important behavior make it fail? Consider the amount, complexity, and domain significance of the code exercised, including consequential framework or library behavior.
- **Refactoring resistance:** Does it survive behavior-preserving implementation changes?
- **Fast feedback:** Is it quick and reliable enough for the stage where it runs?
- **Maintainability:** Is its intent clear, setup proportionate, and ownership cost low? Include the operational cost of any external dependency.

Use these dimensions as a multiplicative heuristic: a practical zero in any one can erase the test's value.

- Judge feedback speed against the stage the evidence gates. A scheduled provider smoke can be timely even though it is unsuitable for the edit loop.
- Prioritize maintainability and refactoring resistance.
- Balance feedback speed against regression protection from the system's actual risks; no single test level maximizes both.
- Rewrite or remove tests whose maintenance cost exceeds their confidence, while preserving valuable behavior coverage elsewhere.

## Avoid Metric Substitution

Use coverage and mutation results as navigation signals:

- Low coverage can expose important untested paths.
- Branch coverage is more informative than line coverage, but neither proves that outcomes are asserted or that important library paths were considered.
- Surviving mutants can expose weak assertions or missing cases.
- High coverage cannot establish test quality.
- A target percentage must not justify trivial, implementation-coupled, or duplicate tests.

Review important uncovered behavior and suspicious survivors individually.

## Verify and Report

When implementation is authorized, run the smallest relevant test during iteration, then the broader affected suite. Keep valuable tests in the routine development cycle; a test that is never run provides no protection. Confirm the intended tests were discovered and executed; zero selected tests is not success. Report:

- the behavior protected;
- the chosen test level and why;
- the exact verification commands and results;
- any dependency or environment not exercised;
- residual risk that belongs at another level.

For read-only design or when no executable repository is available, provide the proposed verification sequence, label it as not run, and do not invent commands or results.
