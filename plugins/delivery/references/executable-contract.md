<!-- Synced from skills/delivery/deliver-issue-slice/references/executable-contract.md by scripts/sync-plugin-references.sh. Edit the source, not this copy. -->

# Establish the executable contract

Choose the branch matching the approved change. Apply your harness's same-issue worker continuity rule: create a Tester only when no matching capable session exists, then resume it through bounded modes. Each mode still requires its own exact-snapshot handoff and completion evidence.

## Behavior-changing work

1. Start the issue Tester's bounded `red-contract` assignment on the issue worktree, passing the assigned model routing, Agent Brief, Verification contract, immutable behavior-start SHA, branch, and delivery contract.
2. Leave test ownership with the Tester until the intended tests reach meaningful red for the protected behavior. Commit and push the test checkpoint; preserve evidence in the commit body and the matching ignored `verification.md` receipt.
3. Resolve the exact `test-contract` snapshot artifacts. Reuse a valid complete result for this `HEAD`; otherwise resume missing work or launch a Reviewer with the full handoff.
4. Return test-owned findings to the same Tester with the exact snapshot and finding IDs and repeat exact-snapshot review until the test contract is clean.
5. Implement the smallest coherent production change that makes the reviewed tests pass. Run focused and broader affected verification, then commit and push a recoverable implementation checkpoint.
6. Resume the same Tester in `green-finalization` mode with the committed production snapshot. It owns assertion inversion, restored-green evidence, test changes, and applicable gates.

A missing public surface is not meaningful red when tests cannot compile. Supply only the smallest behavior-free compilable shell before the Tester establishes red.

## Behavior-preserving work

1. Start the issue Tester's bounded `green-baseline` assignment to establish the smallest characterization portfolio at the immutable behavior-start SHA, prove new assertions by controlled inversion, restore green, and checkpoint any test changes.
2. Resolve the exact `test-contract` snapshot artifacts. Reuse a valid complete result for this `HEAD`; otherwise resume missing work or launch a Reviewer to assess the baseline contract and evidence.
3. Implement the smallest production change that preserves the reviewed contract. Run focused and broader verification, then commit and push the implementation checkpoint.
4. Resume the same Tester in `green-finalization` mode with the committed production snapshot to compare with the approved baseline, invert changed assertions, restore green, and run applicable gates.

## Infrastructure no-test exception

Use this exception only when no deterministic automated boundary faithfully exercises the changed infrastructure risk. Before implementation, record the reason, alternative verification, and residual risk for complete-change review. Difficulty, slowness, and inconvenience do not qualify. Skip only inapplicable test-authoring and test-contract-review tasks; production verification, complete-change review, and every applicable non-test gate remain required.

For behavior-changing or behavior-preserving work, this phase is complete when the test contract is recorded, test-contract review is clean, production satisfies that contract, green finalization is complete, and the exact candidate plus evidence have been returned to the Implementor.

For an infrastructure no-test exception, this phase is complete when the approved decision and residual risk are recorded, the production change passes its alternative verification and every applicable non-test gate, and the exact candidate plus evidence have been returned to the Implementor.
