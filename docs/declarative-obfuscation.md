# Declarative Obfuscation

You can configure ConfuserEx protections directly in source code using the standard `System.Reflection.ObfuscationAttribute`, without a separate `.crproj` file.

## Attribute Semantics

| Property | Effect |
|----------|--------|
| `Exclude = false` | Apply protections to this item (required — the default `true` means "exclude from obfuscation") |
| `ApplyToMembers = true` | Children inherit these protection settings as their base |
| `Exclude = true, ApplyToMembers = true` | This item and all children have no protection |
| `Exclude = false, ApplyToMembers = false` | Protections apply only to this specific item |

## Feature String Syntax

The `Feature` property uses a compact syntax:

```
preset(<level>);+<protection>;-<protection>;<protection>(<arg>=<value>,<arg>=<value>)
```

- `+` adds a protection
- `-` removes a protection
- Arguments are passed in parentheses as comma-separated `key=value` pairs
- Multiple directives are separated by `;`

## Examples

### Assembly-level preset with overrides

```csharp
[assembly: Obfuscation(Exclude = false,
    Feature = "preset(minimum);+ctrl flow;-anti debug;+rename(mode=letters,flatten=false)")]
```

### Set random seed

```csharp
[assembly: Obfuscation(Exclude = false, Feature = "random seed: Hello!")]
```

### Exclude a namespace from renaming

```csharp
[assembly: Obfuscation(Exclude = false, Feature = "namespace 'Test':-rename")]
```

### Apply constants encryption to a single class

```csharp
[Obfuscation(Exclude = false, Feature = "constants")]
class Program {
    public static void Main() {
        Console.WriteLine("Hi");
    }
}
```

### Generate debug symbols

```csharp
[assembly: Obfuscation(Exclude = false, Feature = "generate debug symbol:true")]
```

### Strong name signing

```csharp
[assembly: Obfuscation(Exclude = false, Feature = "strong name key:C:\\key.snk")]
[assembly: Obfuscation(Exclude = false, Feature = "strong name key password:hunter2")]
```

### Packer

```csharp
[assembly: Obfuscation(Exclude = false, Feature = "packer:compressor(mode=dynamic)")]
```

### Namespace-scoped rules

```csharp
[assembly: Obfuscation(Exclude = false,
    Feature = "namespace 'ConfuserEx.CLI':preset(normal);+rename;anti tamper(mode=jit,key=dynamic);-anti debug")]
```

## Complete Example

```csharp
using System.Reflection;

[assembly: Obfuscation(Exclude = false, Feature = "preset(normal);+rename;+ctrl flow")]
[assembly: Obfuscation(Exclude = false, Feature = "random seed: ABCDEFG")]

namespace MyApp {
    [Obfuscation(Exclude = false, Feature = "constants")]
    class Program {
        public static void Main() {
            Console.WriteLine("Protected!");
        }
    }

    // Exclude a class from all obfuscation
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class PublicApiSurface {
        public void ExposedMethod() { }
    }
}
```

## Notes

- `Exclude = false` is required on every attribute — the CLR default is `true` (exclude).
- Declarative and project-file configurations can be combined. Project-file rules are applied first, then attribute-based rules override per-item.
- See the [Protection Reference](protections.md) for all protection IDs and their arguments.
