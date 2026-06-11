# Getting Started

## Installation

### Download a Release

Grab the latest build from [Releases](https://github.com/mcpolo99/ConfuserExx/releases):

| Package | Contents |
|---------|----------|
| `ConfuserEx.zip` | CLI + GUI combined |
| `ConfuserEx-CLI.zip` | CLI only (for CI/CD and automation) |
| `ConfuserEx-GUI.zip` | GUI only (Windows, WPF) |

Extract the zip anywhere. No installer required.

### Pre-release Builds

The `pre-release` tag on the [Releases](https://github.com/mcpolo99/ConfuserExx/releases) page is updated automatically with every push to the `pre-release` branch. Use these to test recent fixes before they are merged into a stable release.

### Requirements

- **Windows 10/11** (GUI requires WPF; CLI works cross-platform on .NET 10 runtime)
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

## Using the GUI

1. Run `ConfuserEx.exe`
2. Drag your `.exe` or `.dll` onto the main window (or click **Add** under the Modules section)
3. Set the **Output Directory**
4. Choose a **Preset** (`minimum`, `normal`, `aggressive`, or `maximum`) — or add individual protections
5. Click **Protect!**

The protected assembly is written to the output directory.

### Adding Probe Paths

If your assembly depends on DLLs that aren't in the same folder, add their directories as **Probe Paths** so ConfuserEx can resolve them.

## Using the CLI

```bash
Confuser.CLI.exe my-project.crproj
```

The CLI reads a [project file](ProjectFormat.md) (`.crproj`) that describes which modules to protect and which protections to apply.

### Minimal Project File

```xml
<project outputDir=".\Confused" baseDir=".\bin\Release\net8.0">
  <rule preset="normal" pattern="true" />
  <module path="MyApp.exe" />
</project>
```

This applies the `normal` preset to `MyApp.exe` and writes the output to `.\Confused`.

### Overriding Output Directory

```bash
Confuser.CLI.exe my-project.crproj -o .\output
```

See the [CLI Reference](cli-reference.md) for all options.

## Using Declarative Obfuscation (Attributes)

You can configure protections directly in source code using `[Obfuscation]` attributes:

```csharp
using System.Reflection;

[assembly: Obfuscation(Exclude = false, Feature = "preset(normal);+rename;+ctrl flow")]

namespace MyApp {
    [Obfuscation(Exclude = false, Feature = "constants")]
    class Program {
        static void Main() {
            Console.WriteLine("Hello, obfuscated world!");
        }
    }
}
```

See [Declarative Obfuscation](declarative-obfuscation.md) for the full attribute syntax.

## MSBuild Integration

Add the NuGet package to your project:

```xml
<PackageReference Include="Confuser.MSBuild.Tasks" Version="1.7.0-*" />
```

This runs obfuscation as a post-build step. Configure protections via a `.crproj` file referenced in your project settings.

## Framework Auto-Detection

ConfuserExx automatically resolves runtime assembly paths for all supported frameworks:

- .NET Framework 2.0 through 4.8 (from the GAC / reference assemblies)
- .NET Core 3.x, .NET 5, 6, 7, 8, 10+ (from the installed runtime packs)

No manual probe paths are needed for standard framework assemblies. If your target uses a custom runtime location, add it as a probe path.

## Next Steps

- [Protection Reference](protections.md) — understand what each protection does and how to configure it
- [Project File Format](ProjectFormat.md) — full `.crproj` schema with pattern expressions and rule inheritance
- [CLI Reference](cli-reference.md) — all command-line options
