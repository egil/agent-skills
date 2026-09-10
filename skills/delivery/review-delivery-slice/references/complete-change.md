# Complete-change review

Review all production and test changes, including any approved infrastructure exception, against the current comparison base. Require evidence that:

- when tests apply, Tester-owned tests remained intact and production satisfies them without weakening the contract;
- when tests apply, meaningful red or an approved green baseline was established, every new or changed assertion received deliberate-inversion failure, and all assertions were restored green;
- when an infrastructure no-test exception applies, no faithful deterministic automated boundary exists and the record includes alternative verification plus residual risk without waiving an applicable gate;
- focused and broader affected verification executed at the approved fidelity and discovered every intended test when tests apply;
- every applicable build, coverage, branch-coverage, analyzer, and other required gate passes without metric-filler tests;
- dependency and boundary fidelity match the contract and every unexercised environment is named;
- the complete solution and each changed component are as simple as the behavior, compatibility, operability, and approved seams permit; and
- the slice is independently mergeable, safe on the default branch, and has no hidden follow-on work needed for validity.

When a finding requires substantial redesign, new behavior, or another independently mergeable slice, identify that boundary for the Implementor rather than absorbing it into remediation.

This mode is clean only when both axes find no actionable issue in the exact complete snapshot and all required evidence is present at the approved fidelity.
