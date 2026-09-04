# Establish the executable contract

Choose the branch matching the approved change.

## Behavior-changing work

1. Launch a short-lived Tester in `red-contract` mode on the issue worktree, passing the selected model and reasoning, Agent Brief, Verification contract, immutable behavior-start SHA, branch, and delivery contract.
2. Leave test ownership with the Tester until the intended tests reach meaningful red for the protected behavior. Checkpoint and push its completed test code and evidence.
3. Resolve the exact `test-contract` snapshot artifacts. Reuse a valid complete result for this `HEAD`; otherwise resume missing work or launch a Reviewer with the full handoff.
4. Route findings to a fresh Tester and repeat exact-snapshot review until the test contract is clean.
5. Implement the smallest coherent production change that makes the reviewed tests pass. Run focused and broader affected verification, then commit and push a recoverable implementation checkpoint.
6. Launch a fresh Tester in `green-finalization` mode. It owns assertion inversion, restored-green evidence, test changes, and applicable gates.

A missing public surface is not meaningful red when tests cannot compile. Supply only the smallest behavior-free compilable shell before the Tester establishes red.

## Behavior-preserving work

1. Launch a Tester in `green-baseline` mode to establish the smallest characterization portfolio at the immutable behavior-start SHA, prove new assertions by controlled inversion, restore green, and checkpoint any test changes.
2. Resolve the exact `test-contract` snapshot artifacts. Reuse a valid complete result for this `HEAD`; otherwise resume missing work or launch a Reviewer to assess the baseline contract and evidence.
3. Implement the smallest production change that preserves the reviewed contract. Run focused and broader verification, then commit and push the implementation checkpoint.
4. Launch a fresh Tester in `green-finalization` mode to compare with the approved baseline, invert changed assertions, restore green, and run applicable gates.

## Infrastructure no-test exception

Use this exception only when no deterministic automated boundary faithfully exercises the changed infrastructure risk. Before implementation, record the reason, alternative verification, and residual risk for complete-change review. Difficulty, slowness, and inconvenience do not qualify. Skip only inapplicable test-authoring and test-contract-review tasks; production verification, complete-change review, and every contract gate remain required.

This phase is complete when the test contract or approved exception is recorded, test-contract review is clean when applicable, production satisfies that contract, green finalization is complete, and the exact candidate plus evidence have been returned to the Implementor.
