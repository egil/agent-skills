# Native worktrees

Every issue worktree is a **native worktree**: one the running agent's own runtime creates, tracks, and removes. Its location is a runtime fact, never a repository contract value.

| Runtime | Location | Created by | Removed by |
| --- | --- | --- | --- |
| Claude Code | `<repo>/.claude/worktrees/<name>`, first on branch `worktree-<name>` | `claude --worktree <name>`, the `EnterWorktree` tool, or subagent `isolation: worktree` | exit-time removal, the `cleanupPeriodDays` sweep, or the owning session after merge |
| Codex | `~/.codex/worktrees/<id>/<repo>`, detached at creation | a Codex managed worktree for a project task | Codex |

Create a worktree only through the runtime's mechanism. A worktree at a caller-chosen path, including `git worktree add <path>`, requires an explicit user request naming that path for this run. A path in a repository contract, checkpoint, prompt template, or earlier run is not that request: provision natively and report the stale path to the immediate Supervisor.

## Provision

- **Claude Code.** Call `EnterWorktree` with name `issue-<number>`. When the session already sits in a worktree, leave it with `ExitWorktree` (`keep`) first. Inside the new worktree, fetch the linked remote branch, switch to it with upstream tracking, then delete the unused `worktree-issue-<number>` branch once it has no commits of its own.
- **Codex.** The Supervisor creates the Implementor as a Codex project task; Codex provisions the managed worktree detached. The Implementor attaches the linked branch as its recovery procedure describes.

Delivery subagents work in the issue worktree they are handed. None of them uses `isolation: worktree`.

## Locate on resume

Locate the worktree from Git, not from a remembered or recorded path. Run `git worktree list --porcelain` and select the entry whose `branch` line is `refs/heads/<linked-branch>`. A Codex worktree that is still detached matches by its owning task and its `HEAD` OID instead.

- One native entry: resume there. Claude Code enters it with `EnterWorktree` and `path`; Codex resumes the owning task.
- No entry: provision a new native worktree.
- An entry outside the native location: it is a legacy worktree. Leave it where it is, and report `human-action` with its path and state, asking whether to resume there. Git refuses to check out the same branch in a second worktree, so a native replacement needs that decision first.
- A `prunable` entry: its directory is gone. Report it; pruning is the user's call.

Record the observed path in the phase checkpoint as evidence of where the work was, not as an instruction for the next run.

## Retain and remove

The worktree holds the ignored review receipts, so preserve it at least until the pull request is ready to merge. After the merge is verified, the owning session removes it:

1. Check that it is clean: `git status --porcelain` is empty, and the local branch head equals the merged pull-request head or is contained in the remote default branch. Ignored review artifacts do not count. A worktree that is dirty or holds commits the remote lacks is **retained**: leave it, and include its path, branch, and which of the two states applies in the completion report.
2. **Claude Code.** Leave with `ExitWorktree` (`keep`), then from the main checkout run `git worktree remove <path>` and `git branch -D <branch>`. Step 1 makes `-D` safe; `-d` refuses after a rebase merge rewrites the commits.
3. **Codex.** Codex owns the directory. Run `git switch --detach` and `git branch -D <branch>` in the worktree, and leave directory removal to Codex.

This removal is part of issue completion, not destructive cleanup. It covers only the issue's own worktree and local branch.

## Repository readiness

Native Claude Code worktrees live inside the repository, so root-level globs reach them. The consuming repository keeps `.claude/worktrees/` in `.gitignore` and excludes `.claude/**` from builds, test discovery, solution filters, and glob-based scripts and linters. For MSBuild, the root `Directory.Build.props` carries:

```xml
<PropertyGroup>
  <DefaultItemExcludes>$(DefaultItemExcludes);.claude/**</DefaultItemExcludes>
</PropertyGroup>
```

Roslyn also loads every `.globalconfig` above each source file, so a build inside a nested worktree loads the enclosing checkout's copy too and duplicate keys are silently unset. A repository with a root `.globalconfig` drops the enclosing copies in the same file. Setting `DiscoverGlobalAnalyzerConfigFiles` to `false` instead also drops package-supplied global configs.

```xml
<Target Name="ExcludeEnclosingCheckoutGlobalConfig" BeforeTargets="CoreCompile">
  <ItemGroup>
    <EditorConfigFiles Remove="@(EditorConfigFiles)"
      Condition="'%(Filename)%(Extension)' == '.globalconfig' and !$([System.String]::Copy('%(FullPath)').StartsWith('$(MSBuildThisFileDirectory)'))" />
  </ItemGroup>
</Target>
```

Filter glob-based scans on the path relative to the repository root. The checkout itself may sit under `.claude/worktrees/`, so an absolute-path match excludes everything.

A missing exclusion is a repository fix. Report it as a blocker; it never justifies a worktree outside the native location.
