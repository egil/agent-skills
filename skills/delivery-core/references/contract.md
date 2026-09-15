# Consuming-repository delivery contract

This workflow is deliberately generic. It carries no repository, organization,
project, or user identity. Every such value comes from the consuming
repository's delivery contract, discoverable through its agent instructions
(`CLAUDE.md`, an `AGENTS.md` pointer, or the invocation itself).

Locate the contract before planning or any mutation. It must supply:

**Identity and tracker**

- the GitHub host, owner/repository, and authorized identity;
- issue relationship rules, and any milestone, project, and label conventions;
- the issue Agent Brief format and the Verification contract format; and
- the project field names and option values when a project projection is
  required.

**Git and publication**

- the default branch, branch naming rule, and native issue-linked-branch
  procedure;
- protected-branch rules and the permitted merge strategy;
- the commit and push policy, including any exact-lease requirement for
  rewriting a published branch; and
- the worktree lifecycle for ignored temporary review artifacts. No tracked
  repository review path is required, and its absence never blocks delivery.

**Verification**

- test, build, quality, coverage, and analyzer gates, with how each is proven to
  cover the current head;
- any approved green-baseline policy for behavior-preserving work; and
- required pull-request workflows, their bounded wait budgets, and their queued,
  stuck, cancelled, or unavailable paths.

**Review**

- any configured GitHub pull-request automation, its trigger, how a review is
  proven to cover the current head, its bounded wait budget, its unavailable or
  non-response path, and the required comment and thread-resolution protocol.

**Authority and recovery**

- the authorization boundary for issue, branch, pull-request, and merge
  mutations, including deployment and release exclusions; and
- the durable phase-checkpoint location and format used to resume after
  interruption or compaction.

If the contract is absent, incomplete, or contradictory, do not invent values.
Stop before the affected mutation and report the missing decision with its
durable issue context. Specifically, never invent a project, account, branch
prefix, default branch, label, merge strategy, or review service.
