# Red contract mode

Protect each approved behavior as `When <observable scenario>, the system <observable outcome>` and map its risk, observation seam, dependency fidelity, and narrowest sufficient test level.

1. Inspect relevant repository test shapes and design the smallest portfolio that covers every material risk without repeating assertions across levels.
2. Author tests against the approved public behavior. A minimal production shell is allowed only when it compiles without implementing the behavior.
3. Run every intended test at the starting revision. Require discovery, execution at the intended seam, and failure because the observable outcome is absent. Compilation, setup, infrastructure, earlier assertion, and zero-discovery failures are not meaningful red.
4. When no compilable seam exists, request the smallest behavior-free shell from the Implementor and stop. When the behavior is already green, report a stale contract or baseline instead of manufacturing failure.
5. Restore and retain only correct expected assertions. Remove every deliberate reversal or injected fault.
6. Apply the repository's authoring self-review, then commit and push the test checkpoint on the existing linked branch. The commit body preserves the objective, starting SHA, design decisions, relevant session context, rejected paths, exact red evidence, unexercised dependencies, and residual risk.

This mode is complete when the linked branch contains the intended tests, every retained test has meaningful-red evidence for the expected reason, no deliberate fault remains, and the Implementor receives the exact checkpoint SHA. A pull request remains premature until local implementation and review are complete.
