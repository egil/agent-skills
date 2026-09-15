#!/usr/bin/env bash
# Validate the Claude delivery plugin's structure and internal consistency.
#
# Deliberately separate from validate-delivery-package.ps1: that script enforces
# Codex rules (notably that a role profile must NOT pin a model), which are the
# opposite of what this plugin needs.

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLUGIN="$ROOT/plugins/delivery"
failures=0

fail() { printf '  FAIL %s\n' "$1" >&2; failures=$((failures + 1)); }
ok()   { printf '  ok   %s\n' "$1"; }

need_file() { [[ -f "$PLUGIN/$1" ]] && ok "$1" || fail "missing $1"; }

echo "Structure"
need_file ".claude-plugin/plugin.json"
need_file "hooks/hooks.json"
need_file "scripts/role-guard.sh"

# Only plugin.json may live in .claude-plugin/ -- a common packaging mistake
# that silently disables every component.
for stray in skills agents commands hooks; do
    [[ -e "$PLUGIN/.claude-plugin/$stray" ]] \
        && fail ".claude-plugin/$stray must live at the plugin root, not inside .claude-plugin/"
done

echo "Manifests"
for j in "$PLUGIN/.claude-plugin/plugin.json" "$PLUGIN/hooks/hooks.json" "$ROOT/.claude-plugin/marketplace.json"; do
    if python3 -c "import json,sys;json.load(open(sys.argv[1]))" "$j" 2>/dev/null; then
        ok "$(basename "$(dirname "$j")")/$(basename "$j") parses"
    else
        fail "$j is not valid JSON"
    fi
done

# Component path overrides have a schema the docs do not pin down (an "agents"
# directory string is rejected at install as invalid input). The layout already
# matches default discovery, so the overrides buy nothing and can break install.
python3 - "$PLUGIN/.claude-plugin/plugin.json" <<'MANIFEST' || failures=$((failures + 1))
import json, sys
d = json.load(open(sys.argv[1]))
risky = [k for k in ("skills","agents","commands","hooks","mcpServers","lspServers") if k in d]
if risky:
    print(f"  FAIL plugin.json sets component path override(s) {risky}; rely on default discovery", file=sys.stderr)
    sys.exit(1)
print("  ok   plugin.json uses default component discovery")
MANIFEST

PLUGIN_NAME=$(python3 -c "import json;print(json.load(open('$PLUGIN/.claude-plugin/plugin.json')).get('name',''))" 2>/dev/null)
[[ -n "$PLUGIN_NAME" ]] && ok "plugin name '$PLUGIN_NAME'" || fail "plugin.json has no name"

# The marketplace must point at this plugin by path.
python3 - "$ROOT/.claude-plugin/marketplace.json" "$PLUGIN_NAME" <<'PY' || failures=$((failures + 1))
import json, sys
mk = json.load(open(sys.argv[1]))
names = [p.get("name") for p in mk.get("plugins", [])]
if sys.argv[2] not in names:
    print(f"  FAIL marketplace.json does not list plugin '{sys.argv[2]}' (has {names})", file=sys.stderr)
    sys.exit(1)
print(f"  ok   marketplace lists '{sys.argv[2]}'")
PY

echo "Agents"
declare -A AGENT_SEEN=()
for f in "$PLUGIN"/agents/*.md; do
    [[ -e "$f" ]] || { fail "no agent definitions found"; break; }
    base=$(basename "$f" .md)
    fm_name=$(sed -n '/^---$/,/^---$/p' "$f" | sed -n 's/^name:[[:space:]]*//p' | head -1)
    [[ "$fm_name" == "$base" ]] || fail "$base.md declares name '$fm_name'"
    sed -n '/^---$/,/^---$/p' "$f" | grep -q '^description:' || fail "$base.md has no description"
    model=$(sed -n '/^---$/,/^---$/p' "$f" | sed -n 's/^model:[[:space:]]*//p' | head -1)
    effort=$(sed -n '/^---$/,/^---$/p' "$f" | sed -n 's/^effort:[[:space:]]*//p' | head -1)
    [[ -n "$model" ]] || fail "$base.md does not declare a model (routing lives in frontmatter, not at spawn time)"
    # Effort is resolvable only from the definition, so an omission is silent drift.
    [[ -n "$effort" ]] || fail "$base.md does not declare an effort level"
    case "$effort" in ""|low|medium|high|xhigh|max) ;; *) fail "$base.md has invalid effort '$effort'" ;; esac
    # Haiku rejects the effort parameter.
    [[ "$model" == haiku* && -n "$effort" ]] \
        && fail "$base.md pairs $model with effort '$effort'; Haiku does not accept effort"
    AGENT_SEEN["$base"]=1
    ok "agents/$base.md ($model/$effort)"
done

echo "Skills"
for d in "$PLUGIN"/skills/*/; do
    name=$(basename "$d")
    [[ -f "$d/SKILL.md" ]] || { fail "skills/$name has no SKILL.md"; continue; }
    head -1 "$d/SKILL.md" | grep -q '^---$' || fail "skills/$name/SKILL.md must open with frontmatter on line 1"
    fm_name=$(sed -n '/^---$/,/^---$/p' "$d/SKILL.md" | sed -n 's/^name:[[:space:]]*//p' | head -1)
    [[ "$fm_name" == "$name" ]] || fail "skills/$name declares name '$fm_name'"
    ok "skills/$name"
done

# Entry points must not be model-invocable: this workflow pushes and merges.
for entry in deliver-milestone deliver-issue; do
    f="$PLUGIN/skills/$entry/SKILL.md"
    [[ -f "$f" ]] || continue
    grep -q '^disable-model-invocation:[[:space:]]*true' "$f" \
        && ok "$entry is user-invoked only" \
        || fail "$entry must set disable-model-invocation: true"
done

echo "Cross-references"
# Every agent named in a skill must exist, and every reference path must resolve.
while IFS= read -r ref; do
    src="${ref%%:*}"; path="${ref#*:}"
    target="$(realpath -m "$(dirname "$src")/$path")"
    [[ -f "$target" ]] && ok "$(basename "$(dirname "$src")") -> $(basename "$path")" \
        || fail "$src references missing $path"
done < <(grep -roh --include='SKILL.md' -E '\.\./\.\./references/[a-z-]+\.md' "$PLUGIN/skills" \
         | sort -u | while read -r p; do
             for s in "$PLUGIN"/skills/*/SKILL.md; do grep -q "$p" "$s" && echo "$s:$p"; done
           done)

# Agent files reference the synced directory as ../references/<file>.md
while IFS= read -r line; do
    src="${line%%:*}"; rel="${line#*:}"
    target="$(realpath -m "$(dirname "$src")/$rel")"
    [[ -f "$target" ]] && ok "$(basename "$src" .md) -> $(basename "$rel")" \
        || fail "$src references missing $rel"
done < <(for a in "$PLUGIN"/agents/*.md; do
             grep -oE '\.\./references/[a-z-]+\.md' "$a" | sort -u | while read -r r; do echo "$a:$r"; done
         done)

for agent in "${!AGENT_SEEN[@]}"; do
    grep -rq "$agent" "$PLUGIN/skills" || printf '  note %s is defined but never named by a skill\n' "$agent"
done

echo "Role guard"
bash -n "$PLUGIN/scripts/role-guard.sh" && ok "role-guard.sh parses" || fail "role-guard.sh has a syntax error"
[[ -x "$PLUGIN/scripts/role-guard.sh" ]] && ok "role-guard.sh is executable" || fail "role-guard.sh is not executable"
grep -q 'agent_type' "$PLUGIN/scripts/role-guard.sh" || fail "role-guard.sh does not read agent_type"

echo
if [[ $failures -gt 0 ]]; then
    printf 'Plugin validation FAILED with %d issue(s).\n' "$failures" >&2
    exit 1
fi
printf 'Plugin validation passed.\n'
