#!/usr/bin/env bash
# PreToolUse guard for delivery roles.
#
# The subagent `tools:` allowlist is the primary restriction; this guard covers
# what an allowlist cannot express -- which *arguments* a role may pass to Bash,
# and where a role may write.
#
# Design: read-only roles (the review axes) and the planner get an ALLOWLIST of
# git/gh operations, because a denylist cannot anticipate every mutating verb.
# The tester, which legitimately commits and pushes, gets a denylist plus a
# write-path restriction.
#
# Fails closed: if the guard cannot classify a call, it blocks it.
#
# Requires `jq`. Without it no call can be classified, so every call is blocked,
# including the main session's -- install jq before enabling this plugin.

set -uo pipefail

AGENT_TYPE=""

deny() {
    printf 'Blocked by delivery role guard (%s): %s\n' "${AGENT_TYPE:-unclassified}" "$1" >&2
    exit 2
}

command -v jq >/dev/null 2>&1 \
    || deny "jq is required to evaluate delivery role boundaries but is not installed"

INPUT=$(cat) || deny "could not read hook input"

# Fail closed on anything that is not a JSON object carrying a tool name.
printf '%s' "$INPUT" | jq -e 'type == "object"' >/dev/null 2>&1 \
    || deny "hook input was not a JSON object; refusing to evaluate"

TOOL_NAME=$(printf '%s' "$INPUT" | jq -re '.tool_name // empty' 2>/dev/null) \
    || deny "hook input has no tool_name; refusing to evaluate"
[[ -n "$TOOL_NAME" ]] || deny "hook input has an empty tool_name"

# `.agent_type` is absent in the main session and present in a subagent. A
# *failed parse* is different from a legitimately absent field, so distinguish
# them rather than defaulting to allow.
if ! AGENT_TYPE=$(printf '%s' "$INPUT" | jq -re '.agent_type // ""' 2>/dev/null); then
    deny "could not read agent_type from hook input; refusing to evaluate"
fi

case "$AGENT_TYPE" in
    delivery-tester|delivery-planner|review-standards|review-spec) ;;
    # The main session and unrelated agents own their own boundaries.
    *) exit 0 ;;
esac

# ---------------------------------------------------------------- writes -----

# artifact_ok <path> <basename> -- <basename> inside a review snapshot directory.
artifact_ok() {
    local path="$1" name="$2"
    [[ "$path" == *"/$name" || "$path" == "$name" ]] || return 1
    [[ "$path" == artifacts/reviews/* || "$path" == */artifacts/reviews/* ]] || return 1
    [[ "$path" != *..* ]] || return 1
    return 0
}

# tester_path_ok <path> -- a test-owned path, or the tester's own receipt.
# DELIVERY_TEST_PATHS (colon-separated globs) extends the defaults for
# repositories whose test layout these patterns miss.
tester_path_ok() {
    local path="$1" pattern
    [[ "$path" != *..* ]] || return 1
    artifact_ok "$path" verification.md && return 0
    local -a patterns=(
        '*/test/*' '*/tests/*' '*/Test/*' '*/Tests/*' 'test/*' 'tests/*'
        '*/spec/*' '*/specs/*' 'spec/*' 'specs/*' '*/__tests__/*' '__tests__/*'
        '*.Tests/*' '*.Test/*' '*Tests/*' '*Test/*'
        '*Test.*' '*Tests.*' '*_test.*' '*.test.*' '*.spec.*' '*Spec.*' '*Fixture.*'
    )
    if [[ -n "${DELIVERY_TEST_PATHS:-}" ]]; then
        local IFS=':'; read -ra extra <<< "$DELIVERY_TEST_PATHS"
        patterns+=("${extra[@]}")
    fi
    for pattern in "${patterns[@]}"; do
        [[ "$path" == $pattern ]] && return 0
    done
    return 1
}

case "$TOOL_NAME" in
    Write|Edit|NotebookEdit|MultiEdit)
        FILE_PATH=$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // .tool_input.notebook_path // empty')
        FILE_PATH="${FILE_PATH//\\//}"
        [[ -n "$FILE_PATH" ]] || deny "$TOOL_NAME call has no file path; refusing to evaluate"
        case "$AGENT_TYPE" in
            review-standards)
                artifact_ok "$FILE_PATH" standards.md \
                    || deny "the Standards axis may write only standards.md in the review snapshot directory (got '$FILE_PATH')" ;;
            review-spec)
                artifact_ok "$FILE_PATH" spec.md \
                    || deny "the Spec axis may write only spec.md in the review snapshot directory (got '$FILE_PATH')" ;;
            delivery-planner)
                deny "the planner does not write files; persist the graph in the issue tracker" ;;
            delivery-tester)
                tester_path_ok "$FILE_PATH" \
                    || deny "the tester owns test code only; '$FILE_PATH' is not a test-owned path. Return a required production change to the implementor, or set DELIVERY_TEST_PATHS if this repository's test layout differs" ;;
        esac
        exit 0
        ;;
esac

[[ "$TOOL_NAME" == "Bash" ]] || exit 0

# ----------------------------------------------------------------- shell -----

COMMAND=$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty')
[[ -n "$COMMAND" ]] && [[ -n "${COMMAND// /}" ]] || exit 0

# Split into simple commands so a verb is matched where it is invoked, not
# wherever it appears as a substring. A guardrail against role drift, not a
# sandbox: it does not defend against eval, aliases, functions, or scripts.
mapfile -t SEGMENTS < <(
    printf '%s' "$COMMAND" \
        | tr '\n\t' '  ' \
        | sed -E 's/(\|\||&&|[;|&])/\n/g' \
        | sed -E 's/^[[:space:]]+//; s/[[:space:]]+$//' \
        | tr -s ' '
)

# classify <segment> -> "git <subcommand>" / "gh <sub> <obj>" / "" (not git/gh)
# Skips leading wrappers, environment assignments, and tool-level global options
# so that `git -C /w commit` and `gh --repo o/r pr merge` classify correctly.
classify() {
    local -a t; read -ra t <<< "$1"
    local i=0 tool=""

    # Wrappers and VAR=value prefixes.
    while (( i < ${#t[@]} )); do
        case "${t[i]}" in
            sudo|env|nice|time|command|builtin|exec|xargs|nohup|stdbuf) ((i++)) ;;
            *=*) [[ "${t[i]}" == -* ]] && break; ((i++)) ;;
            *) break ;;
        esac
    done
    (( i < ${#t[@]} )) || { printf ''; return; }

    tool="$(basename "${t[i]}")"; ((i++))
    case "$tool" in git|gh) ;; *) printf ''; return ;; esac

    # Tool-level global options, including the ones that consume a value.
    while (( i < ${#t[@]} )) && [[ "${t[i]}" == -* ]]; do
        case "${t[i]}" in
            -C|-c|--git-dir|--work-tree|--namespace|--exec-path|--repo|-R|--hostname)
                ((i += 2)) ;;
            *) ((i++)) ;;
        esac
    done
    (( i < ${#t[@]} )) || { printf '%s ?' "$tool"; return; }

    local sub="${t[i]}"; ((i++))
    if [[ "$tool" == "gh" ]]; then
        local obj=""
        while (( i < ${#t[@]} )) && [[ "${t[i]}" == -* ]]; do ((i++)); done
        (( i < ${#t[@]} )) && obj="${t[i]}"
        printf 'gh %s %s' "$sub" "$obj"
    else
        # Families where the read/write distinction is one token deeper.
        case "$sub" in
            worktree|branch|tag|stash|remote|notes|submodule|config|reflog|bisect)
                local obj=""
                while (( i < ${#t[@]} )) && [[ "${t[i]}" == -* ]]; do ((i++)); done
                (( i < ${#t[@]} )) && obj="${t[i]}"
                printf 'git %s %s' "$sub" "$obj" ;;
            *) printf 'git %s' "$sub" ;;
        esac
    fi
}

# A gh api call writes when it names a mutating method, or supplies body fields
# (gh defaults to POST whenever -f/-F/--field/--raw-field/--input is present).
gh_api_writes() {
    local seg="$1"
    [[ "$seg" =~ (-X|--method)([[:space:]]+|=)(POST|PATCH|PUT|DELETE|post|patch|put|delete) ]] && return 0
    [[ "$seg" =~ (^|[[:space:]])(-f|-F|--field|--raw-field|--input)([[:space:]]|=) ]] && return 0
    return 1
}

is_force_push() {
    [[ "$1" =~ (^|[[:space:]])(--force|--force-with-lease|--force-if-includes)([[:space:]]|=|$) ]] && return 0
    [[ "$1" =~ (^|[[:space:]])-[a-zA-Z]*f([[:space:]]|$) ]] && return 0
    return 1
}

in_list() { local n="$1"; shift; local v; for v in "$@"; do [[ "$n" == "$v" ]] && return 0; done; return 1; }

# Read-only git surface. Families needing a read verb are spelled with it.
GIT_READ=(
    "git diff" "git show" "git status" "git log" "git rev-parse" "git rev-list"
    "git ls-files" "git ls-tree" "git ls-remote" "git cat-file" "git blame"
    "git describe" "git shortlog" "git check-ignore" "git check-attr"
    "git symbolic-ref" "git for-each-ref" "git merge-base" "git name-rev"
    "git grep" "git whatchanged" "git show-ref" "git count-objects" "git fetch"
    "git worktree list" "git branch --list" "git branch " "git tag --list" "git tag "
    "git stash list" "git remote " "git remote -v" "git remote show" "git notes list"
    "git config --get" "git submodule status" "git reflog "
)
GH_READ_SUB=("view" "list" "status" "diff" "checks" "search" "browse" "api")

for SEG in "${SEGMENTS[@]}"; do
    [[ -z "${SEG// /}" ]] && continue
    V="$(classify "$SEG")"
    [[ -z "$V" ]] && continue   # not a git/gh invocation

    [[ "$V" == *" ?" ]] && deny "could not classify '$SEG'; refusing to evaluate"

    # Applies to every delegated role.
    if [[ "$V" == "git push"* ]] && is_force_push "$SEG"; then
        deny "force-push in any form is reserved to the issue-owning session ('$SEG')"
    fi
    case "$V" in
        "gh release"*|"gh workflow"*|"gh repo"*|"gh secret"*|"gh ssh-key"*|"gh auth"*)
            deny "release, workflow, repository, and credential operations are outside the delivery mandate ('$SEG')" ;;
        "git filter-branch"|"git update-ref"|"git gc"|"git prune")
            deny "history and object-store rewriting is outside the delivery mandate ('$SEG')" ;;
    esac

    case "$AGENT_TYPE" in
        review-standards|review-spec)
            if [[ "$V" == git* ]]; then
                in_list "$V" "${GIT_READ[@]}" \
                    || deny "review axes are read-only for Git state; '$SEG' is not a read operation"
            elif [[ "$V" == gh* ]]; then
                # `gh api <path>` is flat; `gh <noun> <verb>` puts the verb second.
                read -r _ sub obj <<< "$V"
                if [[ "$sub" == "api" ]]; then
                    gh_api_writes "$SEG" \
                        && deny "review axes never mutate the tracker; this gh api call writes ('$SEG')"
                else
                    in_list "$obj" "${GH_READ_SUB[@]}" \
                        || deny "review axes may only read from the tracker ('$SEG')"
                fi
            fi
            ;;

        delivery-planner)
            if [[ "$V" == git* ]]; then
                in_list "$V" "${GIT_READ[@]}" \
                    || deny "the planner persists issues only; it never touches code, branches, or worktrees ('$SEG')"
            elif [[ "$V" == gh* ]]; then
                case "$V" in
                    "gh pr"*)       deny "the planner does not create or advance pull requests ('$SEG')" ;;
                    "gh issue delete"*|"gh issue transfer"*|"gh issue close"*)
                        deny "the planner never closes, deletes, or transfers an issue ('$SEG')" ;;
                    "gh api"*)
                        # Issue-graph writes are the planner's job; anything else is not.
                        if gh_api_writes "$SEG"; then
                            [[ "$SEG" =~ (repos/[^[:space:]]+/(issues|labels|milestones)|/graphql|^[[:space:]]*gh[[:space:]]+api[[:space:]]+graphql) ]] \
                                || deny "the planner may write only issue-graph endpoints through gh api ('$SEG')"
                        fi ;;
                esac
            fi
            ;;

        delivery-tester)
            case "$V" in
                "gh pr"*|"gh issue"*)
                    deny "publication, review, and tracker mutations belong to the issue-owning session ('$SEG')" ;;
                "gh api"*)
                    gh_api_writes "$SEG" \
                        && deny "the tester does not mutate the tracker ('$SEG')" ;;
                "git merge"|"git rebase"|"git cherry-pick"|"git revert")
                    deny "the implementor controls integration; resolve only test-owned conflicts in place ('$SEG')" ;;
                "git checkout"|"git restore"|"git reset"|"git clean"|"git apply"|"git am")
                    deny "these can alter production files the tester does not own ('$SEG')" ;;
                "git branch "*|"git worktree "*|"git tag "*)
                    case "$V" in
                        "git worktree list"|"git branch --list"|"git tag --list") ;;
                        *) deny "the tester works on the issue's existing branch and worktree only ('$SEG')" ;;
                    esac ;;
            esac
            ;;
    esac
done

exit 0
