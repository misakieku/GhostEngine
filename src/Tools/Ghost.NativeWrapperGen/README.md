# Ghost.NativeWrapperGen

`Ghost.NativeWrapperGen` is a CLI tool that reads low-level generated C# native bindings (e.g. ClangSharp output) and emits a thin, config-driven wrapper layer that routes `DllImport` functions from the `Api` class onto the native struct types they belong to.

It is built for bindings that look like ClangSharp output:

- many `unsafe partial struct` native types
- raw pointer parameters like `NvttSurface*`
- native methods inside a static `Api` class decorated with `[DllImport]`

## What it generates

For each routed function the generator emits a method on the appropriate `partial struct` in a `*.nativegen.cs` file. No wrapper classes, no properties, no owned/marshalled types — just methods.

Example: `nvttDestroySurface(NvttSurface*)` → `public void Dispose()` on `NvttSurface`

```csharp
// NvttSurface.nativegen.cs  (auto-generated)
public unsafe partial struct NvttSurface : System.IDisposable
{
    // From: nvttCreateSurface()
    public static NvttSurface* Create() => Api.nvttCreateSurface();

    // From: nvttDestroySurface(NvttSurface*)
    public void Dispose()
    {
        Api.nvttDestroySurface((NvttSurface*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this));
    }

    // From: nvttSurfaceWidth(NvttSurface*)
    public int Width()
    {
        return Api.nvttSurfaceWidth((NvttSurface*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this));
    }
}
```

## CLI usage

```bash
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- \
  --config <config-path> \
  --input  <generated-binding-folder> \
  --output <wrapper-output-folder>
```

Arguments:

- `--config` — JSON config for one library
- `--input`  — folder containing generated `.cs` binding files (ClangSharp output)
- `--output` — folder where `*.nativegen.cs` files are written

The generator deletes all existing `*.nativegen.cs` files in the output folder before writing new ones.

### Validated commands

```bash
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/nvtt.json --input src/ThridParty/Ghost.Nvtt/Generated --output src/ThridParty/Ghost.Nvtt/WrapperGen
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json --input src/ThridParty/Ghost.Ufbx/Generated --output src/ThridParty/Ghost.Ufbx/Wrapper
```

## Important files

- `src/Tools/Ghost.NativeWrapperGen/Program.cs`
- `src/Tools/Ghost.NativeWrapperGen/Config/WrapperConfig.cs`
- `src/Tools/Ghost.NativeWrapperGen/Parsing/BindingParser.cs`
- `src/Tools/Ghost.NativeWrapperGen/Transform/NamingConventions.cs`
- `src/Tools/Ghost.NativeWrapperGen/Transform/JsonConverter.cs`
- `src/Tools/Ghost.NativeWrapperGen/Emit/WrapperGeneratorEmitter.cs`
- `src/Tools/Ghost.NativeWrapperGen/configs/nvtt.json`
- `src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json`

## Config schema

Top-level fields:

| Field             | Type            | Description |
|-------------------|-----------------|-------------|
| `libraryName`     | string          | Human-readable library name |
| `nativeNamespace` | string          | Namespace of the low-level generated bindings |
| `outputNamespace` | string          | Namespace used in emitted files |
| `nativeTypePrefix`| string          | Prefix stripped when deriving method names (e.g. `"Nvtt"`, `"ufbx_"`) |
| `skipTypes`       | string[]        | Types ignored by parsing/emission |
| `skipFunctions`   | string[]        | Native function names excluded from routing |
| `remaps`          | RemapConfig[]   | Parameter type remappings (e.g. `sbyte*` → `ReadOnlySpan<byte>`) |
| `actions`         | ActionConfig[]  | Routing rules, evaluated in order (first match wins) |

### `remaps`

Each remap entry replaces a native parameter type with a C# type at the call site.

```json
{
  "src": "sbyte*",
  "dst": "ReadOnlySpan<byte>",
  "scope": ["parameter"],
  "filter": [".*name", ".*path"],
  "adapter": {
    "convertBack": {
      "wrapCall": "fixed (byte* p$arg = $arg) { $CALL }",
      "passAs":   "(sbyte*)p$arg"
    }
  }
}
```

Fields:

- `src` — native C# type to match
- `dst` — public C# type to expose in the generated signature
- `scope` — `"parameter"` and/or `"return"`
- `filter` — optional regex list applied to parameter names; only matching parameters are remapped
- `adapter.convertBack.wrapCall` — wraps the whole call site; `$arg` = param name, `$CALL` = the Api call expression
- `adapter.convertBack.passAs` — expression passed as the native argument; `$arg` = param name
- `derivesFrom` — if set, a sibling parameter (matched by name suffix) is consumed and replaced by an expression

### `actions`

Actions are evaluated in order; the first one whose conditions all match is used.

```json
{
  "filter":     "EXTERN_API",
  "conditions": ["VOID_RETURN", "SELF_PTR", "NAME_CONDITION(.*Destroy$TBare)"],
  "targetType": "FIRST_PARAM_TYPE",
  "apply": [
    {
      "type": "INSTANCE_METHOD",
      "opts": {
        "removeFirstParam": true,
        "passAs": "($TSelf*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this)",
        "name": { "set": "Dispose" }
      }
    },
    {
      "type": "INHERITANCE",
      "opts": { "baseType": ["System.IDisposable"] }
    }
  ]
}
```

#### `filter`

Only `"EXTERN_API"` is supported — matches all `[DllImport]` methods on the `Api` class.

#### `conditions`

All conditions must be true for the action to match. Evaluated left-to-right.

| Condition | Description |
|-----------|-------------|
| `SELF_PTR` | First parameter is `T*` where `T` is a known binding struct |
| `FIRST_PARAM_OTHER_TYPE` | First parameter is NOT a known struct pointer |
| `RETURN_BINDING_TYPE` | Return type is `T*` where `T` is a known binding struct |
| `VOID_RETURN` | Return type is `void` |
| `NAME_CONDITION(regex)` | Native function name matches the regex (case-insensitive). Supports `$TSelf` (full struct name, e.g. `NvttSurface`) and `$TBare` (struct name with `nativeTypePrefix` stripped, e.g. `Surface`) |

#### `targetType`

Determines which struct the method is emitted on.

| Value | Description |
|-------|-------------|
| `FIRST_PARAM_TYPE` | The binding struct type of the first parameter |
| `RETURN_TYPE` | The binding struct type of the return value |

#### `apply`

Can be a single object or an array of objects. Each apply step has a `type` and optional `opts`.

**`opts` is dynamic** — fields are read directly from the JSON object by name. Missing fields return `null`.

##### Apply type: `INSTANCE_METHOD`

Emits a `public` instance method on the target struct.

| `opts` field       | Type    | Description |
|--------------------|---------|-------------|
| `removeFirstParam` | bool    | If true, skip the first native parameter from the public signature (it becomes the self pointer) |
| `passAs`           | string  | Expression used as the first native argument. `$TSelf` is replaced with the struct name |
| `name.set`         | string  | Fixed method name (overrides `name.remove`) |
| `name.remove`      | array   | Ordered list of removal tokens applied to derive the method name (see [Naming](#naming)) |

##### Apply type: `STATIC_METHOD`

Same as `INSTANCE_METHOD` but emits a `public static` method. No self pointer is injected.

| `opts` field  | Type   | Description |
|---------------|--------|-------------|
| `name.set`    | string | Fixed method name |
| `name.remove` | array  | Removal token list |

##### Apply type: `INHERITANCE`

Adds one or more base types to the target struct's partial declaration header. Does not emit a method.

| `opts` field | Type     | Description |
|--------------|----------|-------------|
| `baseType`   | string[] | Fully-qualified interface/type names to add (e.g. `["System.IDisposable"]`) |

### Naming

When `name.set` is given, it is used verbatim.

Otherwise `name.remove` is a list of tokens applied in sequence, each followed by underscore-trimming:

| Token | Effect |
|-------|--------|
| `PREFIX` | Strip `nativeTypePrefix` from the start of the name (case-insensitive) |
| `NO_PREFIX($TSelf)` | Strip the struct's bare name (struct name minus `nativeTypePrefix`) from the start or end of the name (case-insensitive) |

Examples with `nativeTypePrefix = "Nvtt"`, struct = `NvttSurface` (bare = `Surface`):

| Native name | Tokens | Result |
|---|---|---|
| `nvttSurfaceWidth` | `["PREFIX", "NO_PREFIX($TSelf)"]` | `Width` |
| `nvttCreateSurface` | `["PREFIX", "NO_PREFIX($TSelf)"]` | `Create` |

## Full config example

```json
{
  "libraryName": "nvtt",
  "nativeNamespace": "Ghost.Nvtt",
  "outputNamespace": "Ghost.Nvtt",
  "nativeTypePrefix": "Nvtt",
  "skipTypes": ["NativeAnnotationAttribute", "NativeTypeNameAttribute"],
  "skipFunctions": [],

  "remaps": [
    {
      "src": "sbyte*",
      "dst": "ReadOnlySpan<byte>",
      "scope": ["parameter"],
      "filter": [".*name", ".*filename", ".*path", ".*prop$"],
      "adapter": {
        "convertBack": {
          "wrapCall": "fixed (byte* p$arg = $arg) { $CALL }",
          "passAs":   "(sbyte*)p$arg"
        }
      }
    }
  ],

  "actions": [
    {
      "comment": "Dispose pattern: void return + T* param + name matches .*Destroy<Bare>",
      "filter":     "EXTERN_API",
      "conditions": ["VOID_RETURN", "SELF_PTR", "NAME_CONDITION(.*Destroy$TBare)"],
      "targetType": "FIRST_PARAM_TYPE",
      "apply": [
        {
          "type": "INSTANCE_METHOD",
          "opts": {
            "removeFirstParam": true,
            "passAs": "($TSelf*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this)",
            "name": { "set": "Dispose" }
          }
        },
        {
          "type": "INHERITANCE",
          "opts": { "baseType": ["System.IDisposable"] }
        }
      ]
    },
    {
      "comment": "First param is T* → instance method on T",
      "filter":     "EXTERN_API",
      "conditions": ["SELF_PTR"],
      "targetType": "FIRST_PARAM_TYPE",
      "apply": {
        "type": "INSTANCE_METHOD",
        "opts": {
          "removeFirstParam": true,
          "passAs": "($TSelf*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this)",
          "name": { "remove": ["PREFIX", "NO_PREFIX($TSelf)"] }
        }
      }
    },
    {
      "comment": "Return type is T* → static method on T",
      "filter":     "EXTERN_API",
      "conditions": ["FIRST_PARAM_OTHER_TYPE", "RETURN_BINDING_TYPE"],
      "targetType": "RETURN_TYPE",
      "apply": {
        "type": "STATIC_METHOD",
        "opts": {
          "name": { "remove": ["PREFIX", "NO_PREFIX($TSelf)"] }
        }
      }
    }
  ]
}
```

## Recommended workflow

```bash
# 1. Build the tool
dotnet build src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj

# 2. Regenerate wrappers
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/nvtt.json --input src/ThridParty/Ghost.Nvtt/Generated --output src/ThridParty/Ghost.Nvtt/WrapperGen
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json --input src/ThridParty/Ghost.Ufbx/Generated --output src/ThridParty/Ghost.Ufbx/Wrapper

# 3. Build the libraries
dotnet build src/ThridParty/Ghost.Nvtt/Ghost.Nvtt.csproj
dotnet build src/ThridParty/Ghost.Ufbx/Ghost.Ufbx.csproj
```

## Adding a new library

1. Create `src/Tools/Ghost.NativeWrapperGen/configs/mylib.json` following the schema above.
2. Run the generator against your ClangSharp output folder.
3. Add the output folder to your `.csproj` as a glob: `<Compile Include="WrapperGen\*.nativegen.cs" />`.
4. Build and iterate.

The key design principle: **keep policy in config, keep the emitter generic**.
