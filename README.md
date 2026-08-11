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

- **orleans-grainservice-cache** — Persist GrainService subscriptions across grain deactivation, silo restarts, and migration; includes an testing sample: [skills/dotnet/orleans/orleans-grainservice-cache/SKILL.md](skills/dotnet/orleans/orleans-grainservice-cache/SKILL.md)  
  Tags: orleans, dotnet, grains, grainservice, caching, testing

- **async-assertion** — WaitForAssertionAsync pattern using an IIncomingGrainCallFilter and per-subscriber channels to drive deterministic assertion retries in Orleans tests without polling: [skills/dotnet/orleans/async-assertion/SKILL.md](skills/dotnet/orleans/async-assertion/SKILL.md)  
  Tags: testing, orleans, dotnet, async, grain-call-filter, channel

## Companion workflow

`verification-driven-delivery` is designed to compose with Matt Pocock's [Skills for Real Engineers](https://github.com/mattpocock/skills), especially `to-spec`, `to-tickets`, `implement`, `tdd`, and `code-review`. Those skills own their delivery phases; `verification-driven-delivery` carries approved verification decisions between them. They are not bundled or installed automatically by this repository.

Install the companion skills separately, using the same project-local or global scope chosen above:

```shell
npx skills@latest add mattpocock/skills --skill setup-matt-pocock-skills --skill to-spec --skill to-tickets --skill implement --skill tdd --skill code-review
```

Add `--global` to that command if the companion skills should be user-level defaults. Run `/setup-matt-pocock-skills` once in each consuming repository before first using `to-spec` or `to-tickets`.
