# Green baseline mode

State each behavior or contract to preserve and map its risk, observation seam, dependency fidelity, and narrowest sufficient test level.

1. Inspect existing tests at the immutable behavior-start SHA and add only characterization needed for material preservation risks.
2. Run the intended baseline tests at that SHA and require discovery plus a green result at the approved fidelity.
3. For every new or materially changed assertion, deliberately reverse it or apply an equivalent controlled behavioral reversal. Observe the intended failure, restore the assertion, and observe green. Never commit the reversal.
4. Run focused and broader affected baseline suites plus applicable contract gates. Record unexercised dependencies and residual risk as unavailable evidence, not success.
5. Apply the repository's authoring self-review. When tests changed, commit and push a checkpoint whose body preserves the objective, immutable start SHA, decisions, changes, inversion and green evidence, exact commands, and residual risk. When nothing changed, create no empty commit.

This mode is complete when the preservation boundary has discovered green evidence, every new or changed assertion has restored sensitivity evidence, no reversal remains, and the Implementor receives the exact checkpoint or verified start SHA.
