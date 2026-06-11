# Contributing to ConfuserExx

Contributions of any kind are welcome. For bugfixes and unit tests, you can submit a PR directly. For larger changes, please open an issue first to discuss the approach.

## Getting Started

1. Fork the repository
2. Create a branch from `develop` (see naming conventions below)
3. Make your changes and ensure CI passes
4. Open a PR targeting `develop`

See [README.md](README.md#building-from-source) for build prerequisites.

## Branch Naming Convention

### With an issue (preferred)

Use GitHub's built-in branch creation:

```bash
gh issue develop 42 --checkout
# → creates branch: 42-fix-samba-timeout (auto-sanitized by GitHub)
```

### Without an issue

| Scenario | Pattern | Example |
|----------|---------|---------|
| Feature | `feature/{description}` | `feature/add-symbol-server` |
| Chore | `chore/{description}` | `chore/update-dependencies` |
| Documentation | `docs/{description}` | `docs/add-plugin-guide` |
| Hotfix (urgent) | `hotfix/{description}` | `hotfix/crash-on-startup` |
| Experiment | `experiment/{description}` | `experiment/avalonia-port` |

**Slug rules:** lowercase, words separated by `-`, no special characters, max ~50 chars.

All branches target `develop` except `hotfix/` which branches from and merges to `main`.

## Testing Policy

Every PR must maintain or improve test coverage. We use a **ratchet strategy** — coverage only goes up, never down.

### Coverage Targets

| Assembly | Current Goal | Long-term Goal |
|----------|-------------|----------------|
| Confuser.Core | 40% | 70% |
| Confuser.CLI | 50% | 70% |
| Confuser.Protections | 30% | 60% |
| Confuser.Renamer | 30% | 60% |
| Confuser.DynCipher | 20% | 50% |

These targets will be raised as coverage improves. CI reports coverage on every PR — check the comment.

### What Must Be Tested

**Always test:**
- New protection implementations (integration test: obfuscate + run + verify)
- Bug fixes (regression test proving the fix works)
- Assembly resolution and path handling logic
- CLI argument parsing and error handling
- Project file (`.crproj`) parsing edge cases

**Don't need tests:**
- Simple property getters/setters
- WPF UI layout or styling changes
- Third-party library behavior (dnlib, CommunityToolkit)

### Test Types

| Type | Location | Framework | Purpose |
|------|----------|-----------|---------|
| Unit tests | `Tests/Confuser.Core.Test/` | xunit + Moq | Test individual classes in isolation |
| Unit tests | `Tests/Confuser.Renamer.Test/` | xunit + Moq | Test renaming logic |
| CLI e2e | `Tests/Confuser.CLI.Test/` | xunit | End-to-end CLI obfuscation |
| GUI smoke | `Tests/Confuser.GUI.Test/` | xunit + FlaUI | WPF UI automation |
| Integration | `Tests/*_*.Test/` | xunit | Obfuscate sample app, run it, verify output |

### Writing a New Integration Test

Each integration test has two projects:
- **Subject** (`Tests/MyFeature/`): Small .NET Framework console app that prints `START`, some output, `END`, and returns exit code `42`
- **Test** (`Tests/MyFeature.Test/`): References `Confuser.UnitTest` and the subject, calls `Run()` with the desired protections

```csharp
public class MyFeatureTest : TestBase {
    public MyFeatureTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

    [Fact]
    public Task MyProtection_SampleApp_RunsCorrectly() =>
        Run("MyFeature.exe",
            new[] { "expected output line" },
            new SettingItem<Protection>("my-protection-id"));
}
```

### Writing a Unit Test

```csharp
[Fact]
public void MethodName_Condition_ExpectedResult() {
    // Arrange
    var sut = new MyService();

    // Act
    var result = sut.DoSomething(input);

    // Assert
    Assert.Equal(expected, result);
}
```

Test names follow `MethodName_Condition_ExpectedResult` convention.

### Running Tests Locally

```bash
# All tests
dotnet test Confuser2.sln -c Release

# Specific test project
dotnet test Tests/Confuser.CLI.Test/Confuser.CLI.Test.csproj -c Release

# With coverage
dotnet test Tests/Confuser.CLI.Test/Confuser.CLI.Test.csproj -c Release --collect:"XPlat Code Coverage"
```

### Coverage Reporting

Every PR receives an automatic coverage comment showing per-assembly line and branch coverage. The full HTML drill-down report is downloadable as the `coverage-report` artifact from the test workflow.

## Commit Format

```
<type>: <short description>

[optional body — what changed and why]
```

**Types** — full words, matching branch naming:

| Type | When to use |
|------|-------------|
| `feature` | New feature or capability added |
| `fix` | Bug fix |
| `test` | Adding or updating tests |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `chore` | Build config, dependencies, tooling, project setup |
| `docs` | Documentation only changes |
| `hotfix` | Urgent fix applied directly to main |
| `experiment` | Exploratory or throwaway work |

**Rules:**
- Full words only — no abbreviations (`feature` not `feat`)
- No parenthesized scopes — the description should be clear enough
- Reference issue number at end when applicable: `(#42)`
- Lowercase first word after colon

**Examples:**

```
feature: add symbol server support (#54)
fix: handle locked file exception on folder scan (#42)
test: add cross-framework integration tests
chore: update dependencies
docs: add plugin development guide
refactor: extract base repository to reduce duplication
```

## Pull Request Process

1. PRs target `develop`, not `main`
2. CI must pass (build + tests + coverage)
3. Coverage must not decrease
4. Only include files that are part of your change — no unrelated modifications
5. Reference the issue in the PR description
6. If fixing a community-reported issue, tag the reporter to test
7. Push fixes to the **same branch** — never close and create a replacement PR

## Code Quality

- Roslyn analyzers (NetAnalyzers + Roslynator) run during build — resolve all warnings
- No `ResolveThrow` calls in new code — use null-safe `Resolve` + handle null
- Follow existing code style (tabs, braces on same line, etc.)
