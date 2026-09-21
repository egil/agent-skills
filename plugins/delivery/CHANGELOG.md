# Changelog

## 0.2.0 (2026-09-16)

- Role guard closes bypasses found in review: `git -C <path>`, `gh api -f state=closed`, and `--force-with-lease`. Read-only roles now use an allowlist.
- Plugin is self-contained: runtime-neutral references are mirrored into `references/` and checked in CI.
- Component path overrides removed from `plugin.json` in favour of default discovery.

## 0.1.0 (2026-09-15)

- Initial release: three skills, four subagents, `PreToolUse` role guard, and per-agent model routing.
