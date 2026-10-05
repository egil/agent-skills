# Changelog

## 0.4.0 (2026-09-29)

- Issue worktrees are native: Claude Code creates them under `.claude/worktrees/` through `EnterWorktree`, Codex under `~/.codex/worktrees/`. The new `native-worktrees.md` reference is the single definition; the skills and the delivery contract refer to it, and a worktree path in a repository contract is treated as stale.
- Resume locates the worktree from `git worktree list --porcelain` and the linked branch instead of a recorded path. A worktree outside the native location is reported for a user decision, never moved.
- After a verified merge the owning session removes its clean worktree and local branch, and reports one that is dirty or holds commits the remote lacks instead of removing it.

## 0.3.0 (2026-09-23)

- Main session pinned to `claude-opus-5-5` at `xhigh` through the `deliver-milestone` and `deliver-issue` frontmatter, so a run no longer inherits Fable 5.1 or whatever model the user last picked.
- Planner and both review axes move to `claude-opus-5-5`; the tester stays on `claude-sonnet-5`. All agents use full model IDs instead of the `opus`/`sonnet` aliases.
- `model-routing.md` updated for Opus 5.5 pricing and its `medium` default effort, with a reordered experiment list.
- Validator requires the entry skills to declare model and effort, and catches Haiku paired with effort under a full model ID.

## 0.2.0 (2026-09-16)

- Role guard closes bypasses found in review: `git -C <path>`, `gh api -f state=closed`, and `--force-with-lease`. Read-only roles now use an allowlist.
- Plugin is self-contained: runtime-neutral references are mirrored into `references/` and checked in CI.
- Component path overrides removed from `plugin.json` in favour of default discovery.

## 0.1.0 (2026-09-15)

- Initial release: three skills, four subagents, `PreToolUse` role guard, and per-agent model routing.
