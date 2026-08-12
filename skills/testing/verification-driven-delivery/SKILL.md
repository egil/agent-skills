---
name: verification-driven-delivery
description: Carry high-value test decisions through specification, tracer-bullet ticketing, implementation, TDD, and code review. Use alongside to-spec, to-tickets, implement, tdd, or code-review when work must preserve protected behaviors, risk-based test levels, approved seams, dependency fidelity, meaningful-red evidence, and residual risks across fresh agent contexts. Do not use for merely running an already-specified test command or for test work with no planning or delivery handoff.
---

# Verification-Driven Delivery

## Purpose

Bridge delivery orchestration with `design-high-value-tests`. Preserve a compact verification contract as work moves from specification to tickets, implementation, and review. Do not duplicate the testing corpus or replace the active phase skill.

## Establish Ownership

Apply instructions in this order:

1. Follow the user's directions and repository instructions.
2. Treat approved specification and ticket decisions as binding unless repository evidence makes them impossible or contradictory.
3. Let the active phase skill own its workflow, approvals, tracker operations, implementation sequence, review shape, and publication actions.
4. Let `design-high-value-tests` own test-design judgment: protected behavior, test level, observation boundary, dependency fidelity, meaningful-red evidence, and the test-value quality gate.
5. Use this skill only to carry those decisions between phases and resolve the overlaps below.

Use `design-high-value-tests` when a test-design decision remains. Load only the references it routes to for that decision. If it is unavailable, preserve existing verification decisions and report the missing dependency instead of silently reconstructing its full guidance.

Work only in the phase the user invoked or requested. Do not silently start another user-invoked skill. Reopen an approved decision only when new repository evidence materially invalidates it; explain the evidence and ask before making a consequential change.

## Preserve the Verification Contract

### Specification contract

Record the following under `## Testing Decisions`:

- **Protected behaviors:** observable scenarios and outcomes worth preserving.
- **Risks and evidence:** each distinct risk, its sufficient test level, and why that level retains the risk.
- **Approved observation seams:** stable public interfaces through which behavior is exercised and observed.
- **Boundary fidelity:** dependencies kept real, boundaries replaced, and where compatibility with a real provider is proved.
- **Meaningful-red approach:** what should fail at the starting revision or when the production change is reversed.
- **Prior art and constraints:** relevant repository test shapes, runners, and infrastructure.
- **Residual risks:** intentionally deferred or separately owned evidence.

Keep this contract architectural. Avoid commands, file paths, framework APIs, and code shapes that implementation must rediscover from the current repository.

### Ticket contract

Give each behavior-changing ticket a compact `## Verification contract`:

- **Protected behavior:** `When <observable scenario>, the system <observable outcome>.`
- **Risk and evidence:** the risk owned by this ticket and the chosen test level.
- **Observation seam:** the approved public interface used by the test.
- **Boundary fidelity:** which relevant dependencies are real or replaced.
- **Meaningful red:** the false observation at the starting revision or after reversing the change, or alternate evidence when direct red is impractical.
- **Success evidence:** the observable result that proves the ticket complete.
- **Residual risk:** relevant behavior or infrastructure this ticket does not exercise.

Acceptance criteria and success evidence must be false before this ticket, become true because of this ticket, and remain owned by this ticket rather than one of its blockers.

For a behavior-preserving prefactor or refactor, replace artificial red evidence with the existing green baseline and any necessary characterization evidence. Do not invent a failing behavior test when observable behavior must remain unchanged.

<verification-contract-example>

- **Protected behavior:** When the same payment event is delivered twice, the system records one charge.
- **Risk and evidence:** Transactional idempotency; integration test with the real managed database.
- **Observation seam:** Public event-handling operation, followed by a fresh durable read because persistence is the protected risk.
- **Boundary fidelity:** Real application and database; external payment provider replaced at the owned adapter.
- **Meaningful red:** Reversing the idempotency guard creates a second durable charge.
- **Success evidence:** Two deliveries complete with one durable charge.
- **Residual risk:** Provider protocol compatibility is covered separately.

</verification-contract-example>

## Apply the Contract by Phase

### With `to-spec`

- Inspect repository seams, prior test art, and relevant architecture decisions before proposing the contract.
- Ask the user to approve materially new seams, as the active skill requires.
- Observe behavior through the highest stable public interface while choosing the narrowest test boundary that still contains each protected risk.
- Record distinct evidence for domain rules, managed dependencies, external compatibility, and deployed journeys only when each protects a distinct risk.

### With `to-tickets`

- Consume the specification contract without redesigning it during decomposition.
- Make every vertical slice own the verification needed for the behavior it introduces.
- Keep test infrastructure with the first slice that needs it unless the infrastructure is independently valuable and verifiable.
- Allow several focused tests when they protect distinct risks. A vertical product slice does not require one end-to-end test or one test at every layer.
- Verify that every acceptance criterion belongs to the ticket and is false at its starting revision before publishing.
- Keep observable completion in acceptance criteria and test mechanics in the verification contract. Do not duplicate every contract field as an acceptance criterion.

### With `implement` or `tdd`

- Treat the ticket contract as the implementation boundary; rediscover exact files, APIs, and commands from the current repository.
- Let the active TDD workflow own red-green sequencing. Use `design-high-value-tests` to decide what evidence is worth retaining.
- Demonstrate meaningful red for behavior changes, then implement the smallest coherent slice and run broader affected verification before completion.
- Follow the active workflow's refactoring stage while applying the high-value test quality gate before handoff.
- Escalate a required seam, level, or fidelity change when it materially alters approved evidence; record the approved change in the ticket or specification.

### With `code-review`

- Preserve the review skill's separate Standards and Spec axes.
- Pass the relevant verification contract and checks into the owning sub-agent brief; do not assume a sub-agent can infer omitted context.
- Under Standards, evaluate retained tests for regression protection, refactoring resistance, feedback speed, maintainability, and adherence to repository conventions.
- Under Spec, verify that each promised behavior and verification contract is fulfilled without unrequested test layers or scope.
- Report missing evidence and inaccurate residual-risk claims within the owning axis. Do not add a third axis or rerank the review skill's findings.

## Resolve Common Overlaps

- **Highest seam versus narrowest level:** use the highest stable public interface that expresses the behavior, at the narrowest execution boundary that includes the relevant risk.
- **Public observation versus durable inspection:** prefer a public retrieval operation for consumer behavior. Use a fresh direct persistence read only in a scoped integration test when durable mapping, transactions, or constraints are themselves the protected risk.
- **Vertical delivery versus test duplication:** make the ticket end-to-end in delivered behavior, not in test topology. Assign each risk to the cheapest sufficient evidence instead of repeating an assertion at every level.
- **Workflow order versus test quality:** let the active phase skill decide when activities occur; let `design-high-value-tests` decide whether the resulting evidence is valuable.

## Hand Off Explicitly

At the end of the active phase, report only what the next fresh context needs:

- the verification contract created, followed, or changed;
- the evidence for any approved deviation;
- verification commands and results, when commands were actually run;
- dependencies or environments not exercised;
- residual risks and their owner or intended test level.

Name the next appropriate user-invoked phase without starting it automatically.
