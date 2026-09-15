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
    case $rc in
        0) got="allow" ;;
        2) got="DENY" ;;
        *) got="ERROR($rc)" ;;   # a runtime fault must never read as allow
    esac
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

raw() { # <raw stdin> <allow|DENY> <label>
    local json="$1" expect="$2" label="$3" out rc got
    out=$(printf '%s' "$json" | bash "$GUARD" 2>&1); rc=$?
    case $rc in 0) got="allow" ;; 2) got="DENY" ;; *) got="ERROR($rc)" ;; esac
    if [[ "$got" == "$expect" ]]; then pass=$((pass+1))
    else fail=$((fail+1)); printf 'FAIL %-64s got %s want %s\n' "$label" "$got" "$expect" >&2; fi
}

# --- Findings from PR review -------------------------------------------------

# Malformed or unclassifiable input must fail closed, not fall through to allow.
raw 'not json at all'                                    DENY "malformed input"
raw '[]'                                                 DENY "JSON array, not object"
raw '{"agent_type":"review-spec"}'                       DENY "no tool_name"
raw '{"tool_name":"Bash","agent_type":"review-spec","tool_input":{}}' allow "no command is a no-op"

# git global options must not hide the subcommand.
check review-standards Bash 'git -C /worktree commit -m x'          DENY
check review-standards Bash 'git -C /worktree diff HEAD'            allow
check delivery-planner Bash 'git --git-dir=/w/.git commit -m x'     DENY
check review-spec      Bash 'git -c user.name=x commit -m y'        DENY
check review-standards Bash '/usr/bin/git push origin main'         DENY
check review-standards Bash 'sudo git reset --hard'                 DENY

# gh global options must not hide the subcommand.
check delivery-tester  Bash 'gh --repo o/r pr merge 3'              DENY
check delivery-planner Bash 'gh -R o/r pr create'                   DENY

# gh api writes without -X: field flags imply POST.
check review-standards Bash 'gh api repos/o/r/issues/1 -f state=closed'      DENY
check review-standards Bash 'gh api --method=POST repos/o/r/issues'          DENY
check review-standards Bash 'gh api -X patch repos/o/r/issues/1'             DENY
check delivery-tester  Bash 'gh api repos/o/r/pulls -f title=x'              DENY
check review-spec      Bash 'gh api repos/o/r/issues/1 --jq .title'          allow

# Every force-push spelling, including --force-with-lease.
check delivery-tester  Bash 'git push --force-with-lease origin feat'        DENY
check delivery-tester  Bash 'git push --force-if-includes origin feat'       DENY
check delivery-tester  Bash 'git push -f origin feat'                        DENY
check delivery-planner Bash 'git push --force origin feat'                   DENY

# Ref and worktree mutations are not read operations.
check review-standards Bash 'git branch -f main abc123'             DENY
check review-standards Bash 'git worktree add -b x /tmp/w'          DENY
check review-standards Bash 'git tag -d v1'                         DENY
check delivery-planner Bash 'git branch -D feature'                 DENY
check review-standards Bash 'git worktree list --porcelain'         allow
check review-standards Bash 'git branch --list'                     allow

# Tester must not reach production code through Git or the editor tools.
check delivery-tester  Bash 'git checkout -- src/Foo.cs'            DENY
check delivery-tester  Bash 'git reset --hard HEAD~1'               DENY
check delivery-tester  Bash 'git apply patch.diff'                  DENY
check delivery-tester  Bash 'gh pr edit 3 --title x'                DENY
check delivery-tester  Bash 'gh pr comment 3 -b hi'                 DENY
check delivery-tester  Edit 'src/Foo.cs'                            DENY
check delivery-tester  Write 'src/Foo.cs'                           DENY
check delivery-tester  Edit 'tests/FooTests.cs'                     allow
check delivery-tester  Write 'src/Foo.Tests/BarTests.cs'            allow
check delivery-tester  Write 'spec/foo.spec.ts'                     allow
check delivery-tester  Write 'artifacts/reviews/issue-47/complete-change/abc/verification.md' allow

# Edit is gated for the review axes too, not just Write.
check review-standards Edit  'src/Foo.cs'                           DENY
check review-spec      Edit  'artifacts/reviews/i/m/s/standards.md' DENY
check review-spec      Edit  'artifacts/reviews/i/m/s/spec.md'      allow

# Planner: destructive tracker verbs and non-issue API writes.
check delivery-planner Bash 'gh issue close 12'                     DENY
check delivery-planner Bash 'gh issue delete 12'                    DENY
check delivery-planner Bash 'gh issue transfer 12 o/r'              DENY
check delivery-planner Bash 'gh api repos/o/r/contents/f -f m=x'    DENY
check delivery-planner Bash 'gh api repos/o/r/issues/1/sub_issues -f sub_issue_id=5' allow
check delivery-planner Bash 'gh api graphql -f query=mutation'      allow
check delivery-planner Bash 'gh issue create --title x'             allow
check delivery-planner Write 'notes.md'                             DENY

# Nobody in a delegated role touches releases, workflows, repos, or credentials.
check delivery-tester  Bash 'gh repo delete o/r'                    DENY
check review-spec      Bash 'gh auth token'                         DENY
check delivery-planner Bash 'git filter-branch --all'               DENY

printf '\n%d passed, %d failed\n' "$pass" "$fail"
[[ $fail -eq 0 ]]
