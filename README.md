# ConfuserExx

[![CI][img_ci]][ci]
[![Tests][img_test]][test]
[![CodeQL][img_codeql]][codeql]
[![MIT License][img_license]][license]

An actively maintained .NET obfuscator. Protects assemblies from .NET Framework 2.0 through .NET 10+.

The original [mkaring/ConfuserEx][mkaring] has been dormant since 2022. This fork ships bug fixes, modern framework support, and an automated release pipeline.

## Get Started

Download the latest build from [Releases][releases], extract, and run:

```bash
# GUI
ConfuserEx.exe

# CLI
Confuser.CLI.exe my-project.crproj
```

See the [Getting Started Guide](docs/getting-started.md) for walkthroughs and configuration.

## Documentation

| | |
|---|---|
| [Getting Started](docs/getting-started.md) | Installation, first run, basic configuration |
| [Protections](docs/protections.md) | All protections with options, presets, and examples |
| [CLI Reference](docs/cli-reference.md) | Command-line options and usage |
| [Project File Format](docs/ProjectFormat.md) | `.crproj` XML schema, rules, and pattern expressions |
| [Declarative Obfuscation](docs/declarative-obfuscation.md) | Attribute-based configuration via `[Obfuscation]` |
| [Building from Source](docs/building.md) | Prerequisites, build commands, and project layout |
| [Contributing](CONTRIBUTING.md) | How to contribute, test policy, and PR workflow |

## Issues

Check [existing issues][issues] first, then [open a new one][new_issue].

## License

MIT. See [LICENSE.md][license].

## Credits

- **[0xd4d][0xd4d]** — [dnlib][dnlib]
- **[Ki (yck1509)][ki]** — original ConfuserEx
- **[Martin Karing][mkaring]** — maintained through v1.6

## Contributors

<a href="https://github.com/mcpolo99/ConfuserExx/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=mcpolo99/ConfuserExx" />
</a>

<!-- links -->
[0xd4d]: https://github.com/0xd4d
[dnlib]: https://github.com/0xd4d/dnlib
[ki]: https://github.com/yck1509
[mkaring]: https://github.com/mkaring/ConfuserEx
[ci]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/ci.yml
[test]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/test.yml
[codeql]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/codeql-analysis.yml
[issues]: https://github.com/mcpolo99/ConfuserExx/issues
[new_issue]: https://github.com/mcpolo99/ConfuserExx/issues/new/choose
[releases]: https://github.com/mcpolo99/ConfuserExx/releases
[license]: LICENSE.md

[img_ci]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/ci.yml/badge.svg
[img_test]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/test.yml/badge.svg
[img_codeql]: https://github.com/mcpolo99/ConfuserExx/actions/workflows/codeql-analysis.yml/badge.svg
[img_license]: https://img.shields.io/github/license/mcpolo99/ConfuserExx.svg
