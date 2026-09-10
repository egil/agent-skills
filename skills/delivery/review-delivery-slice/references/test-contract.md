# Test-contract review

Review the executable contract before production behavior is implemented. Production still being absent is expected.

For behavior-changing work, inspect the tests, any minimal compilable behavior-free shell, test-design decisions, and meaningful-red evidence. Require:

- one-slice observable behavior at the approved public seam;
- the narrowest level that retains the named risk and required dependency fidelity;
- observations that avoid implementation detail and unnecessary collaborator choreography;
- discovery and failure at the intended behavior for the expected reason, rather than compilation, setup, infrastructure, an earlier assertion, or zero discovery;
- a portfolio covering each material risk without mechanically repeated assertions; and
- a credible deliberate-inversion plan for every new or materially changed assertion while all contract gates remain achievable with high-value tests.

For behavior-preserving work, require the named preservation boundary, green discovery evidence at the immutable behavior-start SHA, controlled inversion and restored-green evidence for each new or changed assertion, and concrete residual risk. Do not manufacture red.

This mode is clean only when the tests and evidence form a faithful executable contract for the exact snapshot. It does not establish that production is implemented or publishable.
