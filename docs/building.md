# Building from Source

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)
- [Visual Studio 2025+](https://visualstudio.microsoft.com/) with **Desktop development with C++** workload (only needed for the C++/CLI test project)
- Windows 10/11 (WPF GUI is Windows-only)

## Build

```bash
# Full solution (requires VS 2025+ / MSBuild 18 for C++/CLI test project)
msbuild Confuser2.sln -p:Configuration=Release

# .NET projects only (skips C++/CLI test — works without VS C++ workload)
dotnet build Confuser2.sln -c Release
```

## Test

```bash
# All tests
dotnet test Confuser2.sln -c Release

# Specific test project
dotnet test Tests/Confuser.CLI.Test/Confuser.CLI.Test.csproj -c Release

# With coverage
dotnet test Confuser2.sln -c Release --collect:"XPlat Code Coverage"
```

## Clean

```bash
./scripts/clean-build-artifacts.sh
```

## Project Layout

```
Confuser.Core/          Core obfuscation engine
Confuser.Protections/   Built-in protection implementations
Confuser.Renamer/       Renaming protection (separate due to complexity)
Confuser.DynCipher/     Dynamic cipher generation for protections
Confuser.Runtime/       Runtime stubs injected into protected assemblies (net20)
Confuser.CLI/           Command-line interface
ConfuserEx/             WPF GUI application
Confuser.MSBuild.Tasks/ MSBuild integration NuGet package
Tests/                  Unit, integration, and end-to-end tests
docs/                   Documentation
scripts/                Build and maintenance scripts
additional/             Example .crproj files
```

## Target Frameworks

| Project | TFM | Notes |
|---------|-----|-------|
| Core, Protections, Renamer, DynCipher | `net48` + `netstandard2.0` | Multi-targeted for broad compatibility |
| GUI (ConfuserEx) | `net10.0-windows` | WPF, Windows-only |
| CLI (Confuser.CLI) | `net10.0` | Cross-platform |
| Runtime | `net20` | Injected into target assemblies at any framework level |
| MSBuild Tasks | `netstandard2.0` | Runs inside MSBuild process |

## CI/CD

GitHub Actions minutes are limited, so the cloud pipeline runs the full build only
where it's actually required: the `develop → main` release path. Day-to-day
validation on `develop` is done locally with [`scripts/local-ci.sh`](../scripts/local-ci.sh),
which mirrors the same build, test and coverage steps offline.

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | PR into `main` + push to `main` (release). Manual: `run-ci` label on a `develop` PR, or `workflow_dispatch` | Build, package, create releases |
| `test.yml` | PR into `main`. Manual: `run-ci` label on a `develop` PR, or `workflow_dispatch` | Build, test, coverage report |
| `lint.yml` | PR into `main`. Manual: `run-ci` label on a `develop` PR, or `workflow_dispatch` | Whitespace, style, Roslyn analyzers |
| `codeql-analysis.yml` | Weekly + manual | Security analysis |

**PRs into `develop` do not run the cloud pipeline automatically** — validate them
with `scripts/local-ci.sh`. To run a workflow on a specific `develop` PR anyway, an
admin adds the `run-ci` label (re-add it to trigger each subsequent run) or
dispatches the workflow manually from the Actions tab.

Releases are created by `ci.yml` when `develop` is merged into `main`. Versioning is
handled by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) from `version.json`.
