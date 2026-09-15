#!/usr/bin/env bash
# Regression cases for role-guard.sh. Each case asserts allow (exit 0) or
# deny (exit 2) for one agent/tool/argument combination.

set -uo pipefail
GUARD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/role-guard.sh"
pass=0; fail=0

check() { # <agent> <tool> <value> <allow|DENY>
    local agent="$1" tool="$2" value="$3" expect="$4" json out rc got
    if [[ "$tool" == "Bash" ]]; then
        json=$(jq -nc --arg a "$agent" --arg c "$value" \
            '{agent_type:$a,tool_name:"Bash",tool_input:{command:$c}}')
    else
        json=$(jq -nc --arg a "$agent" --arg t "$tool" --arg f "$value" \
            '{agent_type:$a,tool_name:$t,tool_input:{file_path:$f}}')
    fi
    out=$(printf '%s' "$json" | bash "$GUARD" 2>&1); rc=$?
    got="allow"; [[ $rc -eq 2 ]] && got="DENY"
    if [[ "$got" == "$expect" ]]; then
        pass=$((pass + 1))
    else
        fail=$((fail + 1))
        printf 'FAIL %-16s %-5s %-50s got %s want %s\n' "$agent" "$tool" "${value:0:50}" "$got" "$expect" >&2
        [[ -n "$out" ]] && printf '     %s\n' "$out" >&2
    fi
}

# Reviewers: read-only inspection must pass.
check review-standards Bash 'git diff --stat abc123..HEAD'              allow
check review-standards Bash 'git show HEAD --name-only'                 allow
check review-standards Bash 'git status --porcelain'                    allow
check review-standards Bash 'git worktree list --porcelain'             allow
check review-standards Bash 'git log --oneline --grep="git push"'       allow
check review-standards Bash 'gh issue view 47 --json body'              allow
check review-standards Bash 'gh api repos/o/r/pulls/3 --jq .head.sha'   allow
check review-spec      Bash 'cat src/Foo.cs && rg -n TODO src/'         allow

# Reviewers: any mutation must block, including in a compound command.
check review-standards Bash 'git add -A'                                DENY
check review-standards Bash 'git commit -m "fix"'                       DENY
check review-spec      Bash 'git diff HEAD && git push origin feat'     DENY
check review-spec      Bash 'gh pr merge 3 --squash'                    DENY
check review-standards Bash 'gh pr review 3 --approve'                  DENY
check review-standards Bash 'gh api repos/o/r/issues -X POST -f t=x'    DENY
check review-standards Bash 'git checkout -- .'                         DENY
check review-standards Bash 'git stash'                                 DENY

# Reviewers write exactly one artifact, in a review snapshot directory.
check review-standards Write 'artifacts/reviews/issue-47/complete-change/abc/standards.md'            allow
check review-standards Write '/repo/artifacts/reviews/issue-47/complete-change/abc/standards.md'      allow
check review-spec      Write 'artifacts/reviews/issue-47/complete-change/abc/spec.md'                 allow
check review-standards Write 'artifacts/reviews/issue-47/complete-change/abc/spec.md'                 DENY
check review-spec      Write 'artifacts/reviews/issue-47/complete-change/abc/standards.md'            DENY
check review-standards Write 'src/Foo.cs'                                                             DENY
check review-standards Write 'artifacts/reviews/../../../etc/standards.md'                            DENY
check review-spec      Write 'notes/spec.md'                                                          DENY

# Tester owns test commits on its own branch, never publication or rebase.
check delivery-tester Bash 'git commit -m "test: red" && git push'      allow
check delivery-tester Bash 'dotnet test'                                allow
check delivery-tester Bash 'git add tests/FooTests.cs'                  allow
check delivery-tester Bash 'gh pr create --draft'                       DENY
check delivery-tester Bash 'gh pr merge 3'                              DENY
check delivery-tester Bash 'git rebase main'                            DENY
check delivery-tester Bash 'git push --force origin feat'               DENY
check delivery-tester Write 'tests/FooTests.cs'                         allow

# Planner persists issues only.
check delivery-planner Bash 'gh issue create --title x'                 allow
check delivery-planner Bash 'gh issue view 12'                          allow
check delivery-planner Bash 'git log --oneline -20'                     allow
check delivery-planner Bash 'git commit -m x'                           DENY
check delivery-planner Bash 'gh pr create'                              DENY
check delivery-planner Write 'notes.md'                                 DENY

# Nobody releases or dispatches workflows.
check delivery-tester  Bash 'gh release create v1'                      DENY
check delivery-planner Bash 'gh workflow run ci.yml'                    DENY

# The main session and unrelated agents are untouched.
check ''                Bash 'git push --force-with-lease origin feat'  allow
check ''                Bash 'gh pr merge 3 --squash'                   allow
check general-purpose   Bash 'git push'                                 allow
check general-purpose   Write 'src/Foo.cs'                              allow

printf '\n%d passed, %d failed\n' "$pass" "$fail"
[[ $fail -eq 0 ]]
