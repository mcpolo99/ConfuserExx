# CLI Reference

## Usage

```bash
Confuser.CLI.exe [options] <project-file>
```

The project file is a `.crproj` XML file describing which modules to protect and which protections to apply. See [Project File Format](ProjectFormat.md).

## Options

| Option | Description |
|--------|-------------|
| `-n`, `--noPause` | Do not pause after finishing (useful in CI/CD) |
| `-o`, `--out <dir>` | Override the output directory from the project file |
| `-probe <dir>` | Add a probe directory for dependency resolution |
| `-plugin <path>` | Load a protection plugin from the given path |
| `-debug` | Generate debug symbols (`.pdb`) for the protected output |
| `-snkey <path>` | Strong name key file (`.snk` or `.pfx`) for signing |
| `-snkeypass <password>` | Password for the strong name key (when using `.pfx`) |

## Examples

### Basic protection

```bash
Confuser.CLI.exe MyApp.crproj
```

### Override output directory

```bash
Confuser.CLI.exe MyApp.crproj -o ./protected
```

### CI/CD with strong-name signing

```bash
Confuser.CLI.exe MyApp.crproj -n -snkey signing.snk
```

### With a plugin

```bash
Confuser.CLI.exe MyApp.crproj -plugin ./MyCustomProtection.dll
```

### Multiple probe paths

```bash
Confuser.CLI.exe MyApp.crproj -probe ./libs -probe ./shared
```

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Protection completed successfully |
| non-zero | Protection failed (check console output for details) |

## MSBuild Integration

The `Confuser.MSBuild.Tasks` NuGet package runs the CLI as a post-build step:

```xml
<PackageReference Include="Confuser.MSBuild.Tasks" Version="1.7.0-*" />
```
