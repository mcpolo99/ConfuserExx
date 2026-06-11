# ConfuserExx

[![CI][img_ci]][ci]
[![CodeQL][img_codeql]][codeql]
[![MIT License][img_license]][license]

An actively maintained fork of [ConfuserEx][upstream] — an open-source protector for .NET applications.

> The original [mkaring/ConfuserEx][upstream] has been dormant since 2022. This fork includes bug fixes, new features, and modern .NET support.

## Features

* Supports .NET Framework 2.0/3.5/4.x, .NET Standard 2.0, .NET Core, .NET 5+/6/7/8+
* Symbol renaming (Support WPF/BAML)
* Protection against debuggers/profilers
* Protection against memory dumping
* Protection against tampering (method encryption)
* Control flow obfuscation
* Constant/resources encryption
* Reference hiding proxies
* Disable decompilers
* Embedding dependency
* Compressing output
* Extensible plugin API
* Roslyn code analysis integration
* Auto-detection of .NET runtime paths for modern framework support

## What's improved over upstream

* Fixed WPF resource renaming, .NET Standard obfuscation, control flow protection
* Serialization-aware renaming (DataContract, DataMember, JsonProperty)
* Graceful handling of external/unresolvable assemblies (no more crashes)
* Auto-detection of .NET Core/5+/6/7/8+ runtime assembly paths
* CLI `--snkey` and `--snkeypass` options for CI/CD signing
* Wildcard module loading in `.crproj` files
* Roslyn analyzers (NetAnalyzers + Roslynator) for code quality
* GitHub Actions CI/CD with automatic releases
* Better error messages for .NET 6+ native host executables

## Usage

```bash
Confuser.CLI.exe <path to project file>
```

The project file is a ConfuserEx Project (`*.crproj`).
The format of project file can be found in [docs/ProjectFormat.md][project_format].

### CLI Options

```
-n|noPause   : no pause after finishing protection
-o|out       : specifies output directory
-probe       : specifies probe directory
-plugin      : specifies plugin path
-debug       : specifies debug symbol generation
-snkey       : specifies strong name key file path
-snkeypass   : specifies strong name key password
```

## Building from Source

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
* [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48) (for library targets and test subjects)
* [Visual Studio 2025+](https://visualstudio.microsoft.com/) with **Desktop development with C++** workload (for the C++/CLI test project)
* Windows 10/11 (WPF GUI is Windows-only)

### Build

```bash
# Full solution (requires VS 2025+ MSBuild 18)
msbuild Confuser2.sln -p:Configuration=Release

# .NET projects only (without C++/CLI test project)
dotnet build Confuser2.sln -c Release

# Run tests
dotnet test Confuser2.sln -c Release
```

### Target Frameworks

| Project | TFM |
|---------|-----|
| Core, Protections, Renamer, DynCipher | net48 + netstandard2.0 |
| GUI (ConfuserEx) | net10.0-windows |
| CLI (Confuser.CLI) | net10.0 |
| Runtime | net20 (injected into targets) |

## Bug Report

See the [Issues][issues] section. Please check existing issues before filing a new one.

## Contributing

1. Fork the repository
2. Create a branch from `develop` (see [CONTRIBUTING.md](CONTRIBUTING.md) for naming conventions)
3. Make your changes and ensure CI passes
4. Open a PR targeting `develop`

## License

Licensed under the MIT license. See [LICENSE.md][license] for details.

## Contributors

<a href="https://github.com/mcpolo99/ConfuserExx/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=mcpolo99/ConfuserExx" />
</a>

## Credits

**[0xd4d]** for [dnlib][dnlib] and extensive .NET metadata knowledge.
**[Ki (yck1509)][ki]** for the original ConfuserEx.
**[Martin Karing][mkaring]** for maintaining the project through v1.6.

[0xd4d]: https://github.com/0xd4d
[dnlib]: https://github.com/0xd4d/dnlib
[ki]: https://github.com/yck1509
[mkaring]: https://github.com/mkaring
[upstream]: https://github.com/mkaring/ConfuserEx
[ci]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/ci.yml
[codeql]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/codeql-analysis.yml
[issues]: https://github.com/mcpolo99/ConfuserExx/issues
[license]: LICENSE.md
[project_format]: docs/ProjectFormat.md

[img_ci]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/ci.yml/badge.svg?branch=main
[img_codeql]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/codeql-analysis.yml/badge.svg
[img_license]: https://img.shields.io/github/license/mcpolo99/ConfuserExx.svg?style=flat
