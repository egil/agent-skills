# Green finalization mode

1. Start from the exact committed production snapshot supplied by the Implementor. Run focused tests before editing and classify failures as production behavior, test mechanics, environment, or stale contract.
2. Correct only approved test code and test support. Return production failures or required production seams to the Implementor.
3. For every new or materially changed assertion, deliberately reverse it or apply an equivalent controlled behavioral reversal. Observe failure for the protected behavior, restore it, and observe green. Leave no reversal or production fault behind.
4. Run the smallest relevant tests during iteration, then broader affected verification and the repository quality entry point. Require intended discovery and every applicable coverage, branch-coverage, analyzer, and quality gate.
5. Record unexercised dependencies as unavailable evidence and residual risk. A no-test infrastructure exception requires the issue's documented alternative verification; difficulty alone is insufficient.
6. Apply the repository's authoring self-review. Commit and push any test changes with a context-preserving message; otherwise create no empty commit and retain the exact verified SHA.

Use this mode for later test-owned remediation, including an accepted automated-review suggestion. Work only on delegated finding IDs, preserve Reviewer-authored text, and return the exact test commit and evidence for the Implementor's `dispositions.md`.

This mode is complete when all approved assertions are restored and sensitive, required gates pass or accurately report unavailable dependencies, the linked branch contains any test changes, and the Implementor receives the exact verified SHA for independent complete-change review.
