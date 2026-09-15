# Model routing (Claude)

A **starting policy to calibrate against accepted issues**, not a measured
optimum. Nothing here is proven for this workflow yet; the point is to have a
defensible baseline and a short list of experiments that would move it.

Unlike the Codex routing policy, model and effort are **not** passed per spawn.
Claude Code resolves effort only from a subagent definition's frontmatter, so
routing lives in `agents/*.md`. The Agent tool can override `model` at call
time; it cannot override `effort`.

## Current assignments

| Role | Model | Effort | Why |
| --- | --- | --- | --- |
| Main session (supervisor + implementor) | `opus` | `xhigh` | Long-horizon agentic work holding the branch, the merge, and every disposition decision. `xhigh` is Claude Code's own default and is documented as the best setting for most coding and agentic use. |
| `delivery-planner` | `opus` | `high` | Decomposition errors are the most expensive to unwind — a bad slice boundary contaminates every downstream role. Lowest call volume of any role, so the unit cost barely registers. |
| `delivery-tester` | `sonnet` | `high` | Highest-volume role (four modes, repeated across remediation cycles), and its judgment is bounded by an explicit written verification contract rather than open-ended. |
| `review-standards` | `opus` | `high` | Last gate before merge under standing authority. An escaped defect costs more than the model delta. |
| `review-spec` | `opus` | `high` | Same gate. Kept equal to the Standards axis on purpose — the design treats the two as peers, and splitting capability between them would quietly make one authoritative. |

Rationale for `high` rather than `xhigh` on the delegated roles: each receives a
pinned snapshot and an explicit contract, so the task is well-specified.
`xhigh`/`max` earn their cost on open-ended problems, and the main session is
where those live.

## Pricing basis

Anthropic first-party rates per million tokens, as of 2026-06-24:

| Model | Input | Output | Context |
| --- | --- | --- | --- |
| `claude-opus-5` | $5.00 | $25.00 | 1M |
| `claude-sonnet-5` | $2.00 | $10.00 | 1M |
| `claude-haiku-4-5` | $1.00 | $5.00 | 200K |

Opus 5 costs **2.5× Sonnet 5** per token on both input and output. At an
unchanged token mix it therefore needs ~60% fewer tokens to break even. That is
arithmetic, not a prediction about retry behavior — reasoning output counts
toward consumption, and a cheaper request that needs more cycles to finish the
issue is not cheaper.

**Judge cost per accepted issue, not per request.** Compare total consumption
across attempts, workers, and developer corrections.

## Constraints worth knowing

- **Haiku 4.5 does not accept `effort`.** Pairing it with an `effort:` key in
  frontmatter will error. It also has a 200K context, which is a poor fit for a
  review axis that must hold a pinned snapshot plus untracked files.
- **Caches are model-scoped.** Mixing models across roles costs less here than
  in a single conversation, because each subagent starts a fresh context
  anyway — but repeated invocations of the *same* role do benefit from staying
  on one model.
- **Lower effort on a newer model often beats higher effort on an older one.**
  Measure that before reaching for a more expensive tier.

## Experiments to run, in order

Each should be measured as cost per accepted issue across a run of comparable
slices, with developer corrections counted.

1. **Reviewers: `opus`/`medium` vs the current `opus`/`high`.** The cheapest
   possible win — same model, same cache namespace, no capability question. Run
   this first.
2. **Reviewers: `sonnet`/`xhigh` vs `opus`/`high`.** The real cascade question.
   Anthropic's guidance is to test the capable-model-at-lower-effort side before
   committing to a cheaper model, so treat experiment 1 as the prerequisite.
3. **Tester: `sonnet`/`medium` for `green-finalization` and `rebase-conflict`.**
   These two modes are the most mechanical — an explicit finding ID or a
   specific conflict — and are the likeliest place a lower setting holds. Would
   require splitting the tester into two agent definitions, since effort is
   per-definition.

Do not treat an unmeasured change to this table as an improvement, and do not
add routing metrics to any delivery gate.
