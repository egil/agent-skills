# .NET Testing Guidance

Apply the core skill before framework mechanics. Match the repository's SDK, test framework, runner, assertion library, and fixture conventions rather than introducing a second stack.

## Inspect Before Running

Inspect solution and project files plus central package configuration to determine:

- target frameworks and SDK version;
- xUnit.net, NUnit, MSTest, or another framework;
- VSTest or Microsoft Testing Platform (MTP);
- existing unit/integration project separation, traits, categories, and fixtures;
- container, host, database, or environment prerequisites.

Do not assume VSTest and MTP filter arguments are interchangeable.

## Run the Smallest Relevant Scope

Start with the affected project:

```powershell
dotnet test path\to\Project.Tests.csproj
```

Then run the broader affected solution or suite. Use `--no-build` or `--no-restore` only when the required outputs or restored assets are already valid; report that limitation rather than implying build or restore succeeded.

For VSTest-mode projects, repository-supported filters commonly use:

```powershell
dotnet test path\to\Project.Tests.csproj --filter "FullyQualifiedName~BehaviorName"
```

MTP runner arguments may be passed after `--`, and supported filters depend on the framework extensions. Inspect the project's current help before selecting syntax:

```powershell
dotnet test -?
dotnet run --project path\to\Project.Tests.csproj -- -?
```

Runner flags are version-specific. Use the exact options displayed by the repository's installed runner, and verify framework-specific examples against current documentation before repeating them. Always confirm that the intended tests were discovered and executed; zero selected tests is not a pass.

## Organize by Runtime Boundary

- Keep fast domain tests free of hosts, files, sockets, and databases.
- Put real database, web-host, serialization, and infrastructure behavior in clearly discoverable integration projects or categories.
- Mark live external or slow tests explicitly so local and CI stages make their inclusion visible.
- Reuse the repository's host and container fixtures; do not create a competing fixture architecture for one change.

## Model Time and Other Inputs

Use `TimeProvider` when production behavior genuinely depends on current time and the repository's target framework supports it. Resolve time through that service at the application boundary, then pass explicit timestamps into domain logic where that is the simpler contract.

Prefer the [`TimeProviderExtensions`](https://www.nuget.org/packages/TimeProviderExtensions) package's `ManualTimeProvider` as the primary test implementation when the repository can take the dependency. Inject `TimeProvider.System` in production and `ManualTimeProvider` in tests. Use Microsoft's `FakeTimeProvider` only when existing repository or dependency constraints favor it.

Use `Advance` for ordinary deterministic timer tests: scheduled callbacks observe their logical due times, including intermediate periodic ticks when advancing across more than one interval. Use `Jump` only when the scenario intentionally models the clock moving ahead before overdue callbacks execute, such as a delayed or suspended process.

For timer- or schedule-driven behavior:

- make each application-owned timer or delay whose behavior is under test consume the injected provider;
- start the operation and use an existing readiness mechanism that fires after the relevant timer or delay is registered, not merely when the worker starts; if none exists, split the scheduling shell from the due-work operation instead of adding a test-only hook;
- advance `ManualTimeProvider`, or the repository-supported equivalent, explicitly instead of waiting for wall-clock time;
- await the resulting observable outcome under an independent real-time deadline or cancellation token so a frozen virtual clock cannot also freeze the test's safety watchdog. Real time is only a finite failure watchdog, never the expected delay or evidence that behavior completed.

For `BackgroundService` tests, retain and await `ExecuteTask` or another application-owned execution task so late faults fail the test. `StopAsync` or `Task.WhenAny` alone is not proof of success: await the completed worker task or the `WhenAny` winner to propagate its exception.

Do not replace static ambient time in tests. Treat culture, time zone, randomness, and generated identifiers the same way: control them only when they influence behavior.

## Test Orleans Applications

For compatible Microsoft Orleans integration tests, prefer [`Egil.Orleans.Testing`](https://www.nuget.org/packages/Egil.Orleans.Testing) for deterministic asynchronous observation around the repository's existing test-cluster and fixture setup.

- Register `GrainActivityCollector`; enable storage observation only when relevant. Prefer grain-scoped `WaitForAssertionAsync` overloads, which subscribe before the first assertion attempt and re-evaluate after matching activity.
- Use this observation only when work produces a matching signal after the directly awaited call. Activity triggers re-evaluation; it is not success evidence or a general-purpose polling mechanism. To prove persistence, read durable state afresh instead of asserting operation counts or activated-grain memory.
- Keep the finite timeout as a safety bound. Use `ManualTimeProvider` only for application-owned timers that consume it; use `ReminderTestClock` separately for the Orleans reminder scheduler.
- Use real Orleans wiring when activation, serialization, persistence, streams, reminders, or runtime configuration are part of the risk.

## Run Mutation Testing

Use [Stryker.NET](https://github.com/stryker-mutator/stryker-net) as the primary .NET mutation-testing tool. Prefer a repository-pinned tool manifest and run `dotnet stryker` against the smallest relevant test project before widening scope. Treat surviving mutants as investigation leads for missing cases or weak assertions, not as a score to maximize mechanically.

Inspect runner support and defaults in the repository's pinned Stryker.NET version before choosing the mutation path. Do not assume an MTP suite is supported. Use an MTP runner only when current documentation for that version and a local verification establish correct discovery and mutation results. Otherwise, use a repository-owned VSTest-compatible mutation project or report that mutation testing was not exercised.

## Keep Framework Usage Modest

- Use theories or parameterized tests for multiple cases of the same rule.
- Use fixture lifetimes intentionally; shared fixtures must not leak mutable state between tests.
- Avoid framework-specific extensibility until ordinary test code cannot express the need clearly.
- Prefer the assertion style already used by the repository.
- Avoid mocking non-virtual concrete implementation details merely to satisfy a mocking library.
- With EF Core or another unit-of-work ORM, use a fresh context for arrange, act, and assert so tracking cannot substitute for a committed database read. Do not mock `DbSet` or use an in-memory provider as proof of production database behavior.

## Current Documentation Sources

- [.NET `dotnet test` documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-test)
- [Microsoft Testing Platform overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
- [xUnit.net v3 with Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [.NET `TimeProvider` testing](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) and [`TimeProviderExtensions`](https://www.nuget.org/packages/TimeProviderExtensions)
- [`Egil.Orleans.Testing`](https://www.nuget.org/packages/Egil.Orleans.Testing)
- [Stryker.NET documentation](https://stryker-mutator.io/docs/stryker-net/) and [configuration](https://stryker-mutator.io/docs/stryker-net/configuration/)
- [.NET worker services](https://learn.microsoft.com/dotnet/core/extensions/workers) and [`BackgroundService.ExecuteTask` API](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice.executetask)
