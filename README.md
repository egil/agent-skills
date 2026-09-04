# Agent Skills

Reusable skills for coding agents that support the Agent Skills format.

## Install

These commands use Vercel Labs' [`skills` CLI](https://github.com/vercel-labs/skills). Run project-local commands from the root of the repository that will use the skills.

```shell
# List the available skills without installing anything
npx skills@latest add egil/agent-skills --list

# Choose skills and target agents interactively
npx skills@latest add egil/agent-skills

# Install one skill
npx skills@latest add egil/agent-skills --skill design-high-value-tests

# Install the testing and delivery pair
npx skills@latest add egil/agent-skills --skill design-high-value-tests --skill verification-driven-delivery

# Install every skill while still choosing the target agents
npx skills@latest add egil/agent-skills --skill "*"
```

For an unattended installation, select the agent explicitly and add `--yes`:

```shell
npx skills@latest add egil/agent-skills --skill design-high-value-tests --agent codex --yes
```

`--all` is the broadest shortcut: it installs every discovered skill for every supported agent without prompting. Use it only when that scope is intentional:

```shell
npx skills@latest add egil/agent-skills --all
```

### Project-local or global

Project-local installation is the default. Prefer it when a repository or team should share the workflow. In the default link mode, the CLI keeps canonical copies under `.agents/skills` and creates agent-specific links; `--copy` writes directly to agent-specific directories instead. It also writes `skills-lock.json` in the project root. Review those generated files and commit them deliberately if the repository treats agent configuration as shared.

Add `--global` when a skill is a personal default that should be available across repositories:

```shell
npx skills@latest add egil/agent-skills --global --skill design-high-value-tests
npx skills@latest add egil/agent-skills --global --skill design-high-value-tests --skill verification-driven-delivery
```

`design-high-value-tests` is a good global default. Install `verification-driven-delivery` globally when you use the same delivery workflow in most repositories; otherwise install the pair locally only where that workflow applies. Prefer one scope per skill name unless you have deliberately verified how your agent resolves a project-local and global copy. Use `npx skills list` or `npx skills list --global` to inspect what is installed.

## Skills index

Each entry includes tags for quick scanning.

- **design-high-value-tests** — Design risk-based, maintainable unit, integration, contract, and end-to-end tests around observable behavior: [skills/testing/design-high-value-tests/SKILL.md](skills/testing/design-high-value-tests/SKILL.md)<br>
  Tags: testing, test-design, unit-testing, integration-testing, contract-testing, end-to-end-testing

- **verification-driven-delivery** — Carry approved test decisions through specifications, tracer-bullet tickets, implementation, TDD, and code review: [skills/testing/verification-driven-delivery/SKILL.md](skills/testing/verification-driven-delivery/SKILL.md)<br>
  Tags: testing, planning, tickets, tdd, code-review, verification

- **orchestrate-milestone-delivery** — Coordinate a GitHub milestone or explicit issue set through small Codex-owned slices and merge: [skills/delivery/orchestrate-milestone-delivery/SKILL.md](skills/delivery/orchestrate-milestone-delivery/SKILL.md)<br>
  Tags: codex, github, orchestration, delivery, model-routing

- **plan-delivery-slices** — Persist an oversized issue as small, independently deliverable GitHub child issues: [skills/delivery/plan-delivery-slices/SKILL.md](skills/delivery/plan-delivery-slices/SKILL.md)<br>
  Tags: codex, github, planning, issues, vertical-slices

- **deliver-issue-slice** — Own one bounded issue through acceptance tests, independent review, publication, and merge: [skills/delivery/deliver-issue-slice/SKILL.md](skills/delivery/deliver-issue-slice/SKILL.md)<br>
  Tags: codex, github, implementation, pull-requests, delivery

- **author-slice-tests** — Author and validate high-value tests for one issue slice without owning production changes: [skills/delivery/author-slice-tests/SKILL.md](skills/delivery/author-slice-tests/SKILL.md)<br>
  Tags: codex, testing, acceptance-tests, verification

- **review-delivery-slice** — Independently review one fixed local delivery snapshot against Standards and Spec: [skills/delivery/review-delivery-slice/SKILL.md](skills/delivery/review-delivery-slice/SKILL.md)<br>
  Tags: codex, code-review, standards, specification

- **development-session-observability** — Measure per-work-item time, turns, work cycles, routing, quality, and exact available usage: [skills/observability/development-session-observability/SKILL.md](skills/observability/development-session-observability/SKILL.md)<br>
  Tags: observability, codex, tokens, credits, delivery-metrics

- **orleans-grainservice-cache** — Persist GrainService subscriptions across grain deactivation, silo restarts, and migration; includes an testing sample: [skills/dotnet/orleans/orleans-grainservice-cache/SKILL.md](skills/dotnet/orleans/orleans-grainservice-cache/SKILL.md)  
  Tags: orleans, dotnet, grains, grainservice, caching, testing

- **async-assertion** — WaitForAssertionAsync pattern using the Egil.Orleans.Testing package to drive deterministic assertion retries in Orleans tests without polling: [skills/dotnet/orleans/async-assertion/SKILL.md](skills/dotnet/orleans/async-assertion/SKILL.md)  
  Tags: testing, orleans, dotnet, async, grain-activity-collector, egil-orleans-testing

## Companion workflow

`verification-driven-delivery` is designed to compose with Matt Pocock's [Skills for Real Engineers](https://github.com/mattpocock/skills), especially `to-spec`, `to-tickets`, `implement`, `tdd`, and `code-review`. Those skills own their delivery phases; `verification-driven-delivery` carries approved verification decisions between them. They are not bundled or installed automatically by this repository.

Install the companion skills separately, using the same project-local or global scope chosen above:

```shell
npx skills@latest add mattpocock/skills --skill setup-matt-pocock-skills --skill to-spec --skill to-tickets --skill implement --skill tdd --skill code-review
```

Add `--global` to that command if the companion skills should be user-level defaults. Run `/setup-matt-pocock-skills` once in each consuming repository before first using `to-spec` or `to-tickets`.

## Codex delivery bundle

The delivery workflow is a generic, Codex-specific bundle for moving small, independently mergeable issue slices through planning, implementation, testing, review, and publication. It contains six owner-authored workflow packages (the five delivery roles plus session observability) and the two shared testing packages. The reusable skill and profile contents contain no consuming repository, organization, project, or user identity. A consuming repository supplies those details through its normal project instructions and issue-tracker adapter.

### Role profiles and primary skills

The TOML files under [`codex/agents`](codex/agents) are thin launch profiles. They identify the role, point it at one primary skill, and preserve the role boundary; they deliberately omit `model` and `model_reasoning_effort` so the Supervisor can route each bounded task from the issue's complexity and risk. The Reviewer remains read-only for production and test code, but may write only the ignored local artifacts defined by its skill so fixed-snapshot review can resume after interruption. The Implementor records dispositions in that same snapshot protocol; those artifacts are never staged or published.

| Codex agent | Profile | Primary skill | Role boundary |
| --- | --- | --- | --- |
| `delivery_milestone_supervisor` | [`delivery-milestone-supervisor.toml`](codex/agents/delivery-milestone-supervisor.toml) | [`$orchestrate-milestone-delivery`](skills/delivery/orchestrate-milestone-delivery/SKILL.md) | Coordinates durable issue slices and routes work; does not implement or review changed code. |
| `delivery_slice_planner` | [`delivery-slice-planner.toml`](codex/agents/delivery-slice-planner.toml) | [`$plan-delivery-slices`](skills/delivery/plan-delivery-slices/SKILL.md) | Persists small issue slices and contracts; does not code or review. |
| `delivery_slice_implementor` | [`delivery-slice-implementor.toml`](codex/agents/delivery-slice-implementor.toml) | [`$deliver-issue-slice`](skills/delivery/deliver-issue-slice/SKILL.md) | Owns one slice's production delivery and coordinates Tester and Reviewer agents. |
| `delivery_slice_tester` | [`delivery-slice-tester.toml`](codex/agents/delivery-slice-tester.toml) | [`$author-slice-tests`](skills/delivery/author-slice-tests/SKILL.md) | Owns the slice's test code and verification evidence, not production fixes or review. |
| `delivery_slice_reviewer` | [`delivery-slice-reviewer.toml`](codex/agents/delivery-slice-reviewer.toml) | [`$review-delivery-slice`](skills/delivery/review-delivery-slice/SKILL.md) | Independently reviews a fixed snapshot and persists findings for the Implementor without changing production or test code. |

The workflow packages form this local dependency closure:

- Delivery roles: `orchestrate-milestone-delivery`, `plan-delivery-slices`, `deliver-issue-slice`, `author-slice-tests`, and `review-delivery-slice`.
- Shared local support: `development-session-observability`, `design-high-value-tests`, and `verification-driven-delivery`.
- Every `$skill-name` reference in a delivery `SKILL.md` must resolve to one of those local packages or to an explicitly declared external dependency below. Run the validator after changing the bundle:

```shell
pwsh -NoProfile -File ./scripts/validate-delivery-package.ps1
```

The Supervisor assigns a stable run and work-item identity, then owns passive analysis of explicitly supplied Codex transcripts. Bounded roles emit only sparse semantic markers when given the emitter command; they never maintain a ledger or dashboard. The analyzer keeps wall time distinct from summed active turn time, derives sessions, turns, tools, compactions, models, effort, and exact tokens from native telemetry, and joins marker-derived phases, cycles, routing, quality, and outcomes. It reports source/schema coverage and never exposes transcript content. Account credit balances are never treated as task consumption, so task credit coverage remains unavailable without a future exact attributable source.

### Install skills and agents separately

Install the skills with the Agent Skills CLI. Choose the local or global scope deliberately for the consuming repository:

```shell
npx skills@latest add egil/agent-skills \
  --skill orchestrate-milestone-delivery \
  --skill plan-delivery-slices \
  --skill deliver-issue-slice \
  --skill author-slice-tests \
  --skill review-delivery-slice \
  --skill development-session-observability \
  --skill design-high-value-tests \
  --skill verification-driven-delivery \
  --agent codex --yes
```

Codex custom agents are standalone TOML files, not Agent Skills CLI entries. Copy the profiles to the Codex agent directory you intend to use:

```shell
# User-level profiles shared by your Codex sessions
mkdir -p ~/.codex/agents
cp codex/agents/*.toml ~/.codex/agents/

# Or, from a consuming repository, keep profiles project-scoped
mkdir -p .codex/agents
cp /path/to/agent-skills/codex/agents/*.toml .codex/agents/
```

The same files can be copied with `New-Item -ItemType Directory -Force "$HOME/.codex/agents"` and `Copy-Item codex/agents/*.toml "$HOME/.codex/agents/"` in PowerShell. Review the project instructions and selected permission profile before launching a role. The Supervisor chooses the model and reasoning effort for each bounded assignment; profiles do not impose a fixed model default.

### External delivery dependencies

The delivery bundle explicitly depends on these two skills from Matt Pocock's [Skills for Real Engineers](https://github.com/mattpocock/skills). They are not copied into this repository, and the validator treats the table as the dependency declaration:

| Skill | Status | Source |
| --- | --- | --- |
| `$to-tickets` | external | Matt Pocock's Skills for Real Engineers |
| `$code-review` | external | Matt Pocock's Skills for Real Engineers |

Install them separately when the delivery workflow requires them:

```shell
npx skills@latest add mattpocock/skills \
  --skill to-tickets \
  --skill code-review \
  --agent codex --yes
```
