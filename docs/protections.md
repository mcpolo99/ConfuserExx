# Protection Reference

ConfuserExx ships with a comprehensive set of protections that can be applied individually or combined via presets.

## Presets

Presets are predefined combinations of protections. Apply them in a project file rule or via `[Obfuscation]` attributes.

| Preset | Protections Included |
|--------|---------------------|
| `none` | No protections |
| `minimum` | Anti-IL Disassembly |
| `normal` | Minimum + Constants encryption, Control flow, Reference proxy |
| `aggressive` | Normal + Anti-debug, Anti-dump, Anti-tamper, Invalid metadata |
| `maximum` | Aggressive + Renaming, Resources encryption, Type scrambler |

```xml
<!-- In .crproj -->
<rule preset="normal" pattern="true" />
```

```csharp
// In source code
[assembly: Obfuscation(Exclude = false, Feature = "preset(aggressive)")]
```

## Protections

### Anti-Debug

**ID:** `anti debug`

Detects and prevents debugger and profiler attachment at runtime. Injects runtime checks that terminate the process if a debugger is detected.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `safe`, `win32`, `antinet` | `safe` | Detection method. `safe` uses managed APIs, `win32` uses native API calls, `antinet` uses anti-.NET-profiler techniques. |

```xml
<protection id="anti debug">
  <argument name="mode" value="antinet" />
</protection>
```

---

### Anti-Dump

**ID:** `anti dump`

Prevents memory dump tools from extracting the assembly image from a running process. Erases PE headers and metadata from memory after the CLR has loaded the assembly.

No configurable options.

```xml
<protection id="anti dump" />
```

---

### Anti-IL Disassembly

**ID:** `anti ildasm`

Adds the `SuppressIldasmAttribute` to the assembly, which prevents ILDasm from disassembling it. Also injects metadata structures that crash other common IL disassemblers.

No configurable options.

```xml
<protection id="anti ildasm" />
```

---

### Anti-Tamper

**ID:** `anti tamper`

Encrypts method bodies at build time and decrypts them at runtime via a JIT hook. If the assembly is modified after build, decryption fails and the application crashes — providing tamper resistance.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `jit`, `native` | `jit` | `jit` hooks the JIT compiler to decrypt methods on demand. `native` pre-compiles methods to native code. |
| `key` | `normal`, `dynamic` | `normal` | Key derivation mode. `dynamic` derives the key from the assembly contents for stronger tamper detection. |

```xml
<protection id="anti tamper">
  <argument name="mode" value="jit" />
  <argument name="key" value="dynamic" />
</protection>
```

---

### Constants Encryption

**ID:** `constants`

Encrypts string literals, numeric constants, and other constant values in the assembly. They are decrypted at runtime when accessed.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `normal`, `dynamic`, `x86` | `normal` | Decryption method. `dynamic` generates unique decoders per constant. `x86` uses native x86 code for decryption (Windows only). |
| `decoderCount` | integer | `5` | Number of decoder methods generated. |
| `elements` | `S`, `N`, `P`, `B`, `I` | `SNP` | Which constant types to encrypt: **S**trings, **N**umbers, **P**rimitives, **B**yte arrays, **I**nitializers. |
| `cfg` | `true`, `false` | `true` | Whether to apply control flow obfuscation within decoder methods. |

```xml
<protection id="constants">
  <argument name="elements" value="SNI" />
  <argument name="mode" value="dynamic" />
</protection>
```

---

### Control Flow Obfuscation

**ID:** `ctrl flow`

Transforms the control flow of methods using opaque predicates, switch dispatchers, and expression obfuscation to make decompilation unreliable.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `type` | `switch`, `jump` | `switch` | Obfuscation strategy. `switch` uses switch-based dispatchers, `jump` uses jump-based flow. |
| `predicate` | `normal`, `expression`, `x86` | `normal` | Predicate type. `expression` uses arithmetic expressions. `x86` uses native x86 code (Windows only). |
| `intensity` | `0`–`10` | `5` | How aggressively to obfuscate (higher = more obfuscation, slower execution). |
| `junk` | `true`, `false` | `false` | Insert junk code into obfuscated methods. |

```xml
<protection id="ctrl flow">
  <argument name="type" value="switch" />
  <argument name="predicate" value="expression" />
  <argument name="intensity" value="7" />
</protection>
```

---

### Hardening

**ID:** `harden`

Applies additional runtime integrity checks. Strengthens other protections by adding redundant verification layers.

No configurable options.

```xml
<protection id="harden" />
```

---

### Invalid Metadata

**ID:** `invalid metadata`

Injects malformed metadata entries that cause decompilers and analysis tools to crash or produce incorrect output, while remaining valid enough for the CLR to load.

No configurable options.

```xml
<protection id="invalid metadata" />
```

---

### Reference Proxy

**ID:** `ref proxy`

Replaces direct method and field references with calls through generated proxy delegates, hiding the actual call targets from static analysis.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `mild`, `strong`, `ftn` | `mild` | Proxy generation mode. `strong` adds additional indirection. `ftn` uses function pointer-based proxies. |
| `encoding` | `normal`, `expression`, `x86` | `normal` | How proxy targets are encoded. |
| `internal` | `true`, `false` | `false` | Whether to proxy internal (same-assembly) calls. |
| `typeErasure` | `true`, `false` | `false` | Erase type information in proxy signatures. |
| `depth` | integer | `3` | Proxy chain depth (higher = more indirection). |

```xml
<protection id="ref proxy">
  <argument name="mode" value="strong" />
  <argument name="encoding" value="expression" />
</protection>
```

---

### Renaming

**ID:** `rename`

Renames types, methods, fields, properties, and events to meaningless identifiers. Supports WPF/BAML-aware renaming and respects serialization attributes.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `unicode`, `ascii`, `letters`, `debug`, `sequential`, `reversible`, `decodable` | `unicode` | Naming strategy. `unicode` uses unprintable Unicode characters. `letters` uses readable random names. `reversible` allows decoding with a map file. |
| `flatten` | `true`, `false` | `true` | Flatten namespace hierarchy into a single namespace. |
| `forceRen` | `true`, `false` | `false` | Rename even when the Reflection API is used. |
| `renXaml` | `true`, `false` | `true` | Rename XAML/BAML references (WPF support). |
| `renEnum` | `true`, `false` | `false` | Rename enum members. |
| `password` | string | — | Password for `reversible` mode decoding. |

Serialization attributes that are automatically respected:
- `[DataContract]` / `[DataMember]`
- `[JsonProperty]` (Newtonsoft.Json)
- `[Serializable]`

```xml
<protection id="rename">
  <argument name="mode" value="unicode" />
  <argument name="flatten" value="true" />
  <argument name="renEnum" value="true" />
</protection>
```

---

### Resources Encryption

**ID:** `resources`

Encrypts and compresses embedded resources (`.resx`, images, etc.). Resources are decrypted at runtime when first accessed.

No configurable options.

```xml
<protection id="resources" />
```

---

### Type Scrambler

**ID:** `typescramble`

Replaces concrete types with generic type parameters where possible, making decompiled code harder to understand by obscuring type relationships.

No configurable options.

```xml
<protection id="typescramble" />
```

---

### Compressor (Packer)

**ID:** `compressor`

Compresses the entire output assembly and wraps it in a native stub that decompresses at startup. Reduces file size and adds another layer of obfuscation.

**Options:**

| Name | Values | Default | Description |
|------|--------|---------|-------------|
| `mode` | `normal`, `dynamic` | `normal` | Stub type. `dynamic` generates a unique decompression routine. |
| `key` | `normal`, `dynamic` | `normal` | Compression key derivation. |

```xml
<packer id="compressor">
  <argument name="mode" value="dynamic" />
</packer>
```

## Combining Protections

Protections stack. You can start from a preset and add/remove individual protections:

```xml
<rule preset="normal" pattern="true">
  <protection id="anti tamper">
    <argument name="mode" value="jit" />
  </protection>
  <protection id="rename" action="remove" />
</rule>
```

This applies the `normal` preset, adds anti-tamper with JIT mode, and removes renaming.

## Pattern Expressions

Rules use pattern expressions to target specific types or members:

| Pattern | Matches |
|---------|---------|
| `true` | Everything |
| `name('MyClass')` | Items named `MyClass` |
| `full-name('MyNamespace.MyClass')` | Items with full name `MyNamespace.MyClass` |
| `member-type('type')` | Types only |
| `member-type('method')` | Methods only |
| `has-attr('SerializableAttribute')` | Items with a specific attribute |
| `is-public` | Public members |

Combine with `and`, `or`, `not`:

```xml
<rule preset="none" pattern="member-type('type') and not name('Program')">
  <protection id="rename" />
</rule>
```

See the [Project File Format](ProjectFormat.md) for the full pattern syntax and rule inheritance model.
