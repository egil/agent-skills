#!/usr/bin/env bash
# PreToolUse guard for delivery roles.
#
# Enforces the role boundaries the delivery workflow depends on. The subagent
# `tools:` allowlist is the primary restriction; this guard covers what an
# allowlist cannot express -- which *arguments* a role may pass to Bash, and
# where a role may write.
#
# Fails closed: if the guard cannot evaluate a call, it blocks it.

set -uo pipefail

deny() {
    printf 'Blocked by delivery role guard (%s): %s\n' "${AGENT_TYPE:-unknown}" "$1" >&2
    exit 2
}

if ! command -v jq >/dev/null 2>&1; then
    printf 'Blocked by delivery role guard: jq is required but not installed.\n' >&2
    exit 2
fi

INPUT=$(cat) || deny "could not read hook input"

AGENT_TYPE=$(printf '%s' "$INPUT" | jq -r '.agent_type // empty')
TOOL_NAME=$(printf '%s' "$INPUT" | jq -r '.tool_name // empty')

# Not one of ours, or the main session: the delivery session legitimately owns
# every mutation, so this guard has nothing to say.
case "$AGENT_TYPE" in
    delivery-tester|delivery-planner|review-standards|review-spec) ;;
    *) exit 0 ;;
esac

COMMAND=$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty')
FILE_PATH=$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // empty')
FILE_PATH="${FILE_PATH//\\//}"

# artifact_ok <path> <basename> -- true when the path is <basename> inside a
# review snapshot directory, whether the tool reported it absolute or relative.
artifact_ok() {
    local path="$1" name="$2"
    [[ "$path" == *"/$name" || "$path" == "$name" ]] || return 1
    [[ "$path" == artifacts/reviews/* || "$path" == */artifacts/reviews/* ]] || return 1
    [[ "$path" != *..* ]] || return 1
    return 0
}

# --- Writes -------------------------------------------------------------

# Reviewers may write exactly one artifact, named for their axis.
if [[ "$TOOL_NAME" == "Write" ]]; then
    case "$AGENT_TYPE" in
        review-standards)
            artifact_ok "$FILE_PATH" standards.md \
                || deny "the Standards axis may write only standards.md in the review snapshot directory (got '${FILE_PATH:-<none>}')"
            ;;
        review-spec)
            artifact_ok "$FILE_PATH" spec.md \
                || deny "the Spec axis may write only spec.md in the review snapshot directory (got '${FILE_PATH:-<none>}')"
            ;;
        delivery-planner)
            deny "the planner does not write files; persist the graph in the issue tracker"
            ;;
    esac
fi

# --- Shell --------------------------------------------------------------

[[ "$TOOL_NAME" != "Bash" ]] && exit 0
[[ -z "$COMMAND" ]] && exit 0

# Split the command line into simple commands so that a mutating verb is matched
# where it is actually invoked, not wherever it appears as a substring. This is a
# guardrail against an agent drifting out of its role, not a sandbox: it does not
# defend against deliberate evasion (aliases, eval, scripts, $(...) ).
mapfile -t SEGMENTS < <(
    printf '%s' "$COMMAND" \
        | tr '\n\t' '  ' \
        | sed -E 's/(\|\||&&|[;|&])/\n/g' \
        | sed -E 's/^[[:space:]]*(sudo|env|nice|time|command|xargs)[[:space:]]+//' \
        | sed -E 's/^[[:space:]]+//; s/[[:space:]]+$//' \
        | tr -s ' '
)

# verb <segment> -> "git push", "gh pr merge", or "" for anything else.
verb() {
    local seg="$1" tool sub obj
    read -r tool sub obj _ <<< "$seg"
    case "$tool" in
        git) printf '%s %s' "git" "${sub:-}" ;;
        gh)  printf '%s %s %s' "gh" "${sub:-}" "${obj:-}" ;;
        *)   printf '' ;;
    esac
}

matches() {
    local needle="$1"; shift
    local v
    for v in "$@"; do
        [[ "$needle" == "$v"* ]] && return 0
    done
    return 1
}

GIT_WRITE=(
    "git add" "git commit" "git push" "git rebase" "git merge" "git checkout"
    "git switch" "git reset" "git stash" "git restore" "git cherry-pick"
    "git revert" "git clean" "git tag" "git am" "git apply" "git gc"
    "git update-ref" "git filter-branch"
)
GH_WRITE=(
    "gh pr merge" "gh pr create" "gh pr ready" "gh pr edit" "gh pr close"
    "gh pr review" "gh pr comment" "gh pr reopen"
    "gh issue create" "gh issue edit" "gh issue close" "gh issue comment"
    "gh issue delete" "gh issue reopen" "gh issue transfer" "gh issue pin"
    "gh release" "gh workflow run" "gh repo"
)

for SEG in "${SEGMENTS[@]}"; do
    [[ -z "$SEG" ]] && continue
    V=$(verb "$SEG")
    [[ -z "${V// /}" ]] && continue

    # A writing `gh api` call is identified by its method flag; plain reads pass.
    if [[ "$V" == "gh api"* ]] && [[ "$SEG" =~ (-X|--method)[[:space:]]+(POST|PATCH|PUT|DELETE) ]]; then
        V="gh api write"
    fi

    # Nobody in a delivery role force-pushes, releases, or dispatches workflows.
    if [[ "$V" == "git push"* ]] && [[ "$SEG" =~ (--force([^-]|$)|[[:space:]]-f([[:space:]]|$)) ]]; then
        deny "force-push is reserved to the issue-owning session under an exact-SHA lease"
    fi
    matches "$V" "gh release" "gh workflow run" \
        && deny "release and workflow dispatch are outside the delivery mandate"

    case "$AGENT_TYPE" in
        review-standards|review-spec)
            matches "$V" "${GIT_WRITE[@]}" \
                && deny "review axes are read-only for Git state ('$SEG')"
            matches "$V" "${GH_WRITE[@]}" "gh api write" \
                && deny "review axes return findings to the implementor and never mutate the tracker ('$SEG')"
            ;;
        delivery-tester)
            matches "$V" "gh pr merge" "gh pr create" "gh pr ready" "gh pr close" \
                && deny "publication and merge belong to the issue-owning session ('$SEG')"
            matches "$V" "git merge" "git rebase" \
                && deny "the implementor controls the rebase; resolve only test-owned conflicts in place ('$SEG')"
            ;;
        delivery-planner)
            matches "$V" "${GIT_WRITE[@]}" \
                && deny "the planner persists issues only; it never touches code or branches ('$SEG')"
            matches "$V" "gh pr merge" "gh pr create" "gh pr ready" \
                && deny "the planner does not create or advance pull requests ('$SEG')"
            ;;
    esac
done

exit 0
