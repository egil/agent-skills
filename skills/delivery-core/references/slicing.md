# What counts as one slice

A launchable slice owns:

- one observable behavior or preservation boundary;
- one Verification contract or approved green-baseline contract;
- one native linked branch and one pull request; and
- a change that is independently safe to merge and deploy, optionally disabled
  behind a feature flag.

It must fit comfortably in one fresh context and avoid a long implementation or
review cycle. Incomplete parent functionality may remain disabled behind a flag,
but every merged slice stays compatible and verified on the default branch.

## Testing the boundary

Before authoring tests or making broad production edits, test whether the issue
is genuinely one slice. Signals that it is not:

- two or more behaviors that could each merge on their own;
- an unresolved interface, schema, migration, or design decision shared with
  work someone else is doing;
- acceptance criteria that cannot all become true from a single starting
  revision; or
- a change whose review would not fit one sitting.

If a finding or discovery materially expands the slice mid-flight, checkpoint
the recoverable work and return to planning rather than growing the cycle.

## Designing children

Each child issue must deliver one observable behavior through a narrow, complete
vertical path, own one compact acceptance boundary, be independently mergeable
and production-ready, and state the behavior it does not own as explicitly out
of scope.

Do not split into horizontal implementation layers, unsafe partial behavior, or
speculative abstractions. Keep test infrastructure in the first slice that needs
it unless that infrastructure has independently verifiable value.

Acceptance criteria and success evidence must be false at a behavior-changing
child's starting revision, become true because of that child, and not belong to
a blocker. For behavior-preserving refactors, use a verified green baseline and
characterization evidence instead of inventing a false behavior.

## Ordering the frontier

Rank unblocked slices by:

1. foundations that unlock other work;
2. transitive blockers, weighted by work unlocked and rework avoided; then
3. independent leaves.

Apply the deletion test: a foundation ranks first only when removing it would
block or materially distort downstream work. Do not elevate speculative
abstractions.

Prefer finishing nearly complete work over starting new work. Describe several
children as safely parallel only when they share no dependency, unresolved
design decision, code ownership, schema or migration work, or test
infrastructure, and are unlikely to influence one another's design. Prefer the
smallest useful frontier over maximizing active work, and stop expanding when
coordination becomes the limiting factor.
