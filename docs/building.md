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

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push to `master`, `pre-release`, `feature/**`, `fix/**` + PRs to `master`/`pre-release` | Build, package, create releases |
| `test.yml` | Push to `master`, `pre-release`, `feature/**`, `fix/**` + PRs to `master`/`pre-release` | Build, test, coverage report |
| `format.yml` | Every push | Code style and Roslyn analyzer checks |
| `codeql-analysis.yml` | Weekly + manual | Security analysis |

Releases are created automatically by `ci.yml`:
- Push to `main` branch creates a stable versioned release with a git tag

Versioning is handled by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) from `version.json`.
