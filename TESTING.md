# Unit testing plan and status

This document is the restart checkpoint for unit-test development. Update the status table and commit at the end of every phase.

The tests follow Arrange/Act/Assert, use behavior-focused names, keep one Act per test, avoid infrastructure dependencies, and prefer small explicit test data. xUnit is the test framework, FluentAssertions supplies assertions, and Moq supplies test doubles.

For void methods, Moq callbacks capture emitted arguments for state assertions. `Verify` is reserved for invocation counts and uses `It.IsAny<T>()` arguments rather than embedding behavior assertions in verification expressions.

## Phases

| Phase | Scope | Status | Completion commit |
|---|---|---|---|
| 0 | Record strategy, conventions, and restart points | Complete | `3975427` |
| 1 | Add solution/test project, package references, internals access, and smoke test | Complete | Current phase commit |
| 2 | Test `TelemetryEvent` and `TelemetrySerializer`, including escaping, invariant numbers, nulls, and non-finite values | Not started | — |
| 3 | Extract/test bounded generic collection logic and deterministic timed-cache behavior | Not started | — |
| 4 | Test sink-driven tracker behavior with Moq callbacks, beginning with world-key transitions and suppression | Not started | — |
| 5 | Add practical tests for other isolated decisions; document Unity/runtime boundaries rather than creating brittle pseudo-unit tests | Not started | — |
| 6 | Run clean build/test, update README, review package warnings, commit, and push | Not started | — |

## Commands

The intended final commands are:

```bash
dotnet build ValheimTelemetry.sln -c Release
dotnet test ValheimTelemetry.sln -c Release --no-build
```

## Boundaries

Harmony patch binding, Unity prefab/component classification, ZDO replication ordering, and dedicated-server ownership behavior require the installed Valheim integration environment. They remain startup/manual integration tests. Unit tests should cover deterministic logic around those boundaries and must not attempt to boot Unity or mutate a world.
