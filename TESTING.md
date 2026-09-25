# Unit testing plan and status

This document is the restart checkpoint for unit-test development. Update the status table and commit at the end of every phase.

The tests follow Arrange/Act/Assert, use behavior-focused names, keep one Act per test, avoid infrastructure dependencies, and prefer small explicit test data. xUnit is the test framework, FluentAssertions supplies assertions, and Moq supplies test doubles.

For void methods, Moq callbacks capture emitted arguments for state assertions. `Verify` is reserved for invocation counts and uses `It.IsAny<T>()` arguments rather than embedding behavior assertions in verification expressions.

## Phases

| Phase | Scope | Status | Completion commit |
|---|---|---|---|
| 0 | Record strategy, conventions, and restart points | Complete | `3975427` |
| 1 | Add solution/test project, package references, internals access, and smoke test | Complete | `9f623b1` |
| 2 | Test `TelemetryEvent` and `TelemetrySerializer`, including escaping, invariant numbers, nulls, and non-finite values | Complete | `013534f` |
| 3 | Extract/test bounded generic collection logic and deterministic timed-cache behavior | Complete | `53e2d78` |
| 4 | Test sink-driven tracker behavior with Moq callbacks, beginning with world-key transitions and suppression | Complete | `2d7eb69` |
| 5 | Add practical tests for other isolated decisions; document Unity/runtime boundaries rather than creating brittle pseudo-unit tests | Complete | `8943f74` |
| 6 | Run clean build/test, update README, review package warnings, commit, and push | Complete | Current phase commit |

## Commands

The intended final commands are:

```bash
dotnet build ValheimTelemetry.sln -c Release
dotnet test ValheimTelemetry.sln -c Release --no-build
```

## Test dependencies

| Package | Version | Purpose |
|---|---:|---|
| `xunit.v3` | 3.2.2 | Test framework; selected as a stable .NET 8-compatible xUnit v3 release |
| `xunit.runner.visualstudio` | 3.1.5 | `dotnet test` / VSTest discovery |
| `Microsoft.NET.Test.Sdk` | 18.10.1 | .NET test host integration |
| `FluentAssertions` | 7.2.2 | Readable assertions on the Apache-2.0 licensed line |
| `Moq` | 4.20.72 | Interface test doubles and callback argument capture |

## Boundaries

Harmony patch binding, Unity prefab/component classification, ZDO replication ordering, and dedicated-server ownership behavior require the installed Valheim integration environment. They remain startup/manual integration tests. Unit tests should cover deterministic logic around those boundaries and must not attempt to boot Unity or mutate a world.

## Mutation testing

Stryker.NET 4.16.0 is pinned as a repository-local tool. The checked-in configuration mutates the deterministic core covered by unit tests; Unity/Harmony integration code remains under dedicated-server validation rather than producing misleading no-coverage mutants. It uses Stryker's Microsoft Testing Platform runner because the VSTest runner did not activate mutations with this xUnit v3 test assembly.

```bash
dotnet tool restore
cd tests/ValheimTelemetry.Tests
dotnet stryker
```

Generated `StrykerOutput` reports are intentionally excluded from Git.

### Mutation result for 1.1.2

The final deterministic-core run on 2026-09-25 created 171 in-scope mutants:

| Result | Count |
|---|---:|
| Killed | 168 |
| Timed out (detected infinite loop) | 1 |
| Survived | 2 |
| No coverage | 2 |
| Mutation score | **97.69%** |

The two survivors replace the `"R"` floating-point format string with the empty/default format. On the .NET 8 test runtime both formats use the same shortest round-trippable representation, so the mutants are behaviorally equivalent there. The two no-coverage mutants are in the production `ZNet.instance?.GetWorldName()` fail-safe wrapper, which requires the Unity/Valheim runtime and remains an integration-test boundary. The timeout removed the bounded-set eviction statement, producing the infinite loop Stryker was expected to detect.

The mutation scope deliberately excludes Unity/Harmony/ZDO integration classes. The reported score must not be interpreted as whole-plugin code coverage.
