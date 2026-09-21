# delivery

Drive a GitHub milestone or issue set through small, independently mergeable slices. The main session supervises and implements; context-isolated subagents plan, test, and review under a hook-enforced role boundary.

## Install

```shell
/plugin marketplace add egil/agent-skills
/plugin install delivery@egil-agent-skills
```

Or, without installing: `claude --plugin-dir /path/to/agent-skills/plugins/delivery`.

`jq` must be on `PATH`. The role guard parses hook input with it on every tool call and blocks the call when it is missing.

The subagents preload two skills that are not bundled. Install them at user or project scope before first use:

```shell
npx skills@latest add egil/agent-skills \
  --skill design-high-value-tests \
  --skill verification-driven-delivery \
  --agent claude --yes
```

## Use

| Command | Does |
| --- | --- |
| `/delivery:deliver-milestone <milestone or issue list>` | Supervise a milestone: mode, snapshot, frontier, completion. |
| `/delivery:deliver-issue <issue number>` | Deliver one slice from its linked branch through merge. |
| `/delivery:review-slice <test-contract\|complete-change>` | Pin a snapshot, run both review axes, aggregate the receipt. |

The first two are user-invoked only (`disable-model-invocation: true`) because they push and merge. `review-slice` is read-only and may be invoked by Claude.

## Layout

```
.claude-plugin/plugin.json   manifest
agents/                      delivery-planner, delivery-tester, review-standards, review-spec
skills/                      the three commands above
hooks/hooks.json             PreToolUse role guard
scripts/role-guard.sh        the guard, pinned by role-guard.test.sh
references/                  runtime-neutral procedures, synced from the repository root
model-routing.md             per-agent model and effort, with the experiments that would change it
```

Files under `references/` are copies. Edit the source named in each file's header, then run `scripts/sync-plugin-references.sh` from the repository root.

## Role boundary

| Role | Git | Tracker | Files |
| --- | --- | --- | --- |
| review axes | read-only allowlist | read-only | only its own axis artifact |
| `delivery-tester` | test checkpoints only | none | test-owned paths only |
| `delivery-planner` | read-only allowlist | issue-graph writes only | none |

The guard is a guardrail against role drift, not a sandbox. Set `DELIVERY_TEST_PATHS` (colon-separated globs) where a repository's test layout differs from the defaults.

Full design notes, including why the guard uses allowlists, are in the [repository README](https://github.com/egil/agent-skills#claude-delivery-plugin).

## License

MIT. See [LICENSE](https://github.com/egil/agent-skills/blob/main/LICENSE).
