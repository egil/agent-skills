# Model routing (Claude)

A **starting policy to calibrate against accepted issues**, not a measured
optimum. Nothing here is proven for this workflow yet; the point is to have a
defensible baseline and a short list of experiments that would move it.

Unlike the Codex routing policy, model and effort are **not** passed per spawn.
Claude Code resolves effort only from frontmatter, so routing lives in
`agents/*.md` for the subagents and in the two entry skills
(`deliver-milestone`, `deliver-issue`) for the main session. The Agent tool can
override `model` at call time; it cannot override `effort`.

## Current assignments

| Role | Model | Effort | Set in | Why |
| --- | --- | --- | --- | --- |
| Main session (supervisor + implementor) | `claude-opus-5-5` | `xhigh` | entry skill frontmatter | Long-horizon agentic work holding the branch, the merge, and every disposition decision. Pinned so a run does not inherit whatever model the user last picked, which is often Fable 5.1 at 2.5× the price. |
| `delivery-planner` | `claude-opus-5-5` | `high` | agent frontmatter | Decomposition errors are the most expensive to unwind. A bad slice boundary contaminates every downstream role. Lowest call volume of any role, so the unit cost barely registers. |
| `delivery-tester` | `claude-sonnet-5` | `high` | agent frontmatter | Highest-volume role (four modes, repeated across remediation cycles), and its judgment is bounded by an explicit written verification contract rather than open-ended. |
| `review-standards` | `claude-opus-5-5` | `high` | agent frontmatter | Last gate before merge under standing authority. An escaped defect costs more than the model delta. |
| `review-spec` | `claude-opus-5-5` | `high` | agent frontmatter | Same gate. Kept equal to the Standards axis on purpose. The design treats the two as peers, and splitting capability between them would quietly make one authoritative. |

Rationale for `high` rather than `xhigh` on the delegated roles: each receives a
pinned snapshot and an explicit contract, so the task is well-specified.
`xhigh`/`max` earn their cost on open-ended problems, and the main session is
where those live.

Why full model IDs rather than aliases: the `opus` alias resolves per provider
(Opus 5.5 on the Anthropic API, Bedrock, and Vertex; Opus 4.6 on Foundry) and
moves when a new model ships. Either silently changes routing that this file
claims to control. Bump the IDs here deliberately.

`review-slice` sets no model. Claude may invoke it mid-session, and a model
override there would switch the model under the session that called it.

## Pricing basis

Anthropic first-party rates per million tokens, as of 2026-09:

| Model | Input | Output | Context |
| --- | --- | --- | --- |
| `claude-fable-5-1` | $10.00 | $50.00 | 1M |
| `claude-opus-5-5` | $4.00 | $20.00 | 1M |
| `claude-sonnet-5` | $2.00 | $10.00 | 1M |
| `claude-haiku-4-5` | $1.00 | $5.00 | 200K |

Opus 5.5 costs **2× Sonnet 5** per token on both input and output, down from
2.5× for Opus 5. At an unchanged token mix it therefore needs 50% fewer tokens
to break even. Fable 5.1 costs 2.5× Opus 5.5. That is arithmetic, not a
prediction about retry behavior. Reasoning output counts toward consumption,
and a cheaper request that needs more cycles to finish the issue is not
cheaper.

**Judge cost per accepted issue, not per request.** Compare total consumption
across attempts, workers, and developer corrections.

## Constraints worth knowing

- **Opus 5.5 defaults to `medium` effort**, one level below Opus 5. An agent
  or entry skill that omits `effort` runs lower than this table says. The
  plugin validator rejects an omission for that reason.
- **Opus 5.5 thinking cannot be disabled.** Effort is the only depth control.
- **Haiku 4.5 does not accept `effort`.** Pairing it with an `effort:` key in
  frontmatter will error. It also has a 200K context, which is a poor fit for a
  review axis that must hold a pinned snapshot plus untracked files.
- **Caches are model-scoped.** Mixing models across roles costs less here than
  in a single conversation, because each subagent starts a fresh context
  anyway. Repeated invocations of the *same* role do benefit from staying on
  one model.
- **Lower effort on a newer model often beats higher effort on an older one.**
  Measure that before reaching for a more expensive tier.

## Experiments to run, in order

Each should be measured as cost per accepted issue across a run of comparable
slices, with developer corrections counted.

1. **Reviewers: `claude-opus-5-5`/`medium` vs the current `/high`.** The
   cheapest possible win: same model, same cache namespace, no capability
   question. `medium` is also the model's own default. Run this first.
2. **Tester: `claude-opus-5-5`/`medium` vs `claude-sonnet-5`/`high`.** The price
   gap narrowed to 2×, and Opus at lower effort often matches Sonnet at higher
   effort in fewer turns. If it holds, every delegated role shares one cache
   namespace.
3. **Reviewers: `claude-sonnet-5`/`xhigh` vs `claude-opus-5-5`/`high`.** The
   cascade question. Run it only if experiment 1 shows the reviewers need
   `high`, since the capable model at lower effort is the cheaper answer to test
   first.
4. **Main session: `claude-fable-5-1`/`high` vs `claude-opus-5-5`/`xhigh`.**
   Fable needs to close issues in 60% fewer tokens, or with fewer developer
   corrections, to pay for itself. Only worth running on milestones where the
   main session visibly struggles with disposition or rebase decisions.
5. **Tester: `claude-sonnet-5`/`medium` for `green-finalization` and
   `rebase-conflict`.** These two modes are the most mechanical (an explicit
   finding ID or a specific conflict) and are the likeliest place a lower
   setting holds. Would require splitting the tester into two agent
   definitions, since effort is per-definition. Moot if experiment 2 moves the
   tester to Opus.

Do not treat an unmeasured change to this table as an improvement, and do not
add routing metrics to any delivery gate.
