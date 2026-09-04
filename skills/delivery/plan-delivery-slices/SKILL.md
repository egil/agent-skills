---
name: plan-delivery-slices
description: Decompose one delivery issue into durable, independently mergeable GitHub slices.
---

# Plan delivery slices

Turn an oversized issue or newly discovered prerequisite into a durable GitHub issue graph. GitHub must contain enough context and native structure for a fresh Codex task to resume after compaction, interruption, or loss of the originating task.

This is a bounded planning role. Inspect code and documentation as needed, but do not edit code, create branches or pull requests, test changes, review a diff, or close the parent issue. Report the resulting graph to the issue-owning Supervisor and stop.

## Load the consuming-repository contract

Before any GitHub mutation, locate the delivery contract through an applicable `AGENTS.md` pointer or require it in the assignment. It must define the GitHub host, authorized identity, repository, milestone and project conventions, issue relationship mechanism, label names, Agent Brief format, and the Codex-to-Supervisor status-signal format. It must also define the default branch, branch naming and merge policy, verification gates, and mutation authority. If it is absent or contradictory, report `blocked` or `human-action` rather than inventing values.

## Dependencies

- External required companion: `$to-tickets` from [Matt Pocock's Skills for Real Engineers](https://github.com/mattpocock/skills), for tracer-bullet decomposition.
- Internal required companions: `$design-high-value-tests` for test-design judgment, `$verification-driven-delivery` for each slice's Verification or approved green-baseline contract, and `$development-session-observability` for sparse semantic markers when the invoking owner supplies its command.

Load the companions when their decisions are needed. `$to-tickets` supplies decomposition guidance; it does not replace the consuming repository's GitHub or delivery contract.

When a marker command is supplied, load `$development-session-observability` and emit only material planning markers. Never delay issue persistence or duplicate native telemetry.

## Establish the planning boundary

Require these inputs from the delegating task:

- the parent or dependent issue and its complete Agent Brief;
- the explicitly selected milestone or other durable delivery scope;
- why decomposition or a prerequisite is needed;
- known dependency and ownership boundaries; and
- the consuming-repository delivery contract or its discoverable location.

Verify the full issue body and comments, existing native sub-issues and dependencies, milestone, project item, assignees, linked branches and pull requests, checks, and related Codex tasks. Paginate every relationship and reconcile any reported total count; a successful default-sized page is not complete evidence. Inspect the repository and domain documentation only far enough to make the slices accurate. Immediately before creating anything, re-query GitHub for existing matching children or prerequisites and reuse or reconcile them instead of creating duplicates.

If the parent is already one small coherent slice, create nothing. Report that conclusion and the evidence to the delegating task.

## Design small production-ready slices

Each child issue must:

- deliver one observable behavior or preservation boundary through a narrow, complete vertical path;
- own one compact Verification contract or approved green-baseline contract and acceptance boundary;
- be independently mergeable into the contract-defined default branch, safe to deploy, and production-ready, optionally disabled behind a feature flag;
- fit comfortably in one fresh Codex context and avoid a long implementation or review cycle;
- avoid overlapping code ownership or an unresolved interface, schema, migration, or design decision with a supposedly parallel slice; and
- state behavior it does not own as explicitly out of scope.

Do not split work into horizontal implementation layers. Keep test infrastructure in the first slice that needs it unless the infrastructure has independently verifiable value. Record native dependency edges for ordered work. Dependent slices wait for blockers to merge and start from updated default branch; do not design a pull-request stack.

Several children may be described as parallel only when they are clearly independent and unlikely to influence one another's implementation or design. Prefer the smallest useful frontier over maximizing active work.

Acceptance criteria and success evidence must be false at a behavior-changing child's starting revision, become true because of that child, and not belong to a blocker. For behavior-preserving refactors, use a verified green baseline and characterization evidence instead of inventing a false behavior.

## Persist the graph in GitHub

Before mutation, verify the contract-defined GitHub identity with `gh auth status --hostname <github-host>` and `gh api --hostname <github-host> user --jq .login`. Require complete, consistent identity evidence. Do not change Git credentials, request or reveal a token, or continue after an incomplete query.

For every required child or prerequisite:

1. Create a self-contained GitHub issue with its parent, user-visible behavior, acceptance criteria, explicit out-of-scope boundary, blockers, and contract-defined milestone or scope.
2. Attach it to the originating issue with GitHub's native sub-issue relationship and verify the relationship by reading it back. Use the identifier type required by the selected GitHub API; do not substitute an issue number for a database or node ID.
3. Create and verify GitHub-native dependency edges. Use the required identifier type, and use the repository's documented fallback only when GitHub genuinely lacks the native relationship. Report any degraded relationship.
4. Add the issue to the contract-defined project and apply its exact field and option values when a project projection is required. Apply the contract-defined category and readiness labels. A human-action label means the issue requires human decision or human implementation; record the exact decision or implementation boundary and the supporting evidence.
5. Post the repository-required AI-labelled Agent Brief as the durable implementation contract. For behavior-changing work, include `## Verification contract` with protected behavior, risk and evidence level, observation seam, boundary fidelity, meaningful-red approach, success evidence, and residual risk. For behavior-preserving work, record the approved green-baseline contract, preservation boundary, characterization evidence, and residual risk.
6. Set unstarted project status and delivery state using the contract's exact values. A fully specified unblocked issue is ready for an agent; a ready issue with an open dependency is blocked; a human decision or implementation is human action.
7. Read back the issue, labels, milestone or scope, every page of native relationships, and project item, reconciling counts where available. Partial or inconsistent GitHub results are a persistence failure, never success.

The delegated planning assignment authorizes only the scoped issue, relationship, milestone, label, and project mutations required to persist this graph. It does not authorize code, branch, pull-request, merge, deployment, release, or unrelated tracker work.

## Escalate decisions durably

Never invent readiness to keep delivery moving. When a missing product, domain, architecture, priority, ownership, or scope decision blocks one or more slices:

- persist the question and delivery impact in the relevant GitHub issue;
- apply the contract-defined human-action/readiness state for a human decision or human implementation;
- add dependency edges needed to prevent premature launch; and
- notify the delegating Supervisor so the question reaches the top-level Supervisor.

Do not ask the repository owner directly from this nested task. The top-level Supervisor owns human communication and routes the answer back.

The Planning agent may write the initial human-action state while persisting its graph. The Supervisor owns subsequent reconciliation, user communication, and restoration of readiness after a decision.

## Hand off and stop

Report only what the delegating task needs:

- created or reused issue numbers and links;
- native parent and dependency graph;
- milestone or scope, project state, and readiness state for each issue;
- which slices are safely parallel; and
- any human decision, human implementation boundary, or persistence failure that prevents progress.

Do not include speculative implementation instructions beyond the durable Agent Brief and Verification or approved green-baseline contract, and do not begin delivery of a child issue.
