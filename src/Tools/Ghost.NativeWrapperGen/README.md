# Ghost.NativeWrapperGen

`Ghost.NativeWrapperGen` is a CLI tool that reads low-level generated C# native bindings and emits a configurable wrapper layer.

It is built for bindings that look like ClangSharp output:

- many `partial struct` native types
- raw pointers like `foo_node*`
- list structs shaped like `{ T* data; nuint count; }`
- native methods inside a static `Api` class

The generator is now config-driven in three key areas:

- wrapper kinds are configurable per type
- string/blob special handling is configurable by type shape
- static methods are generated from discovered native methods plus parameter adapters from config

## Current capabilities

Implemented now:

- parse top-level structs, enums, and `Api` methods
- detect list structs automatically
- generate wrapper types with configurable kinds: `class`, `struct`, `ref struct`, `readonly ref struct`
- generate pointer-list wrappers for `T** + count`
- generate raw void-list wrappers for `void* + count`
- generate string/blob accessors from config instead of hardcoded type names
- generate static wrapper methods from config-driven parameter adapters
- generate owned-resource disposal from config
- regenerate `ufbx` successfully and build `Ghost.Ufbx`

Validated commands:

```bash
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json --input src/ThridParty/Ghost.Ufbx/Generated --output src/ThridParty/Ghost.Ufbx/Warper
dotnet build src/ThridParty/Ghost.Ufbx/Ghost.Ufbx.csproj
```

## CLI usage

```bash
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config <config-path> --input <generated-binding-folder> --output <wrapper-output-folder>
```

Arguments:

- `--config`: JSON config for one library
- `--input`: folder containing generated `.cs` binding files
- `--output`: folder where `*.nativegen.cs` files are written

The generator deletes all existing `*.nativegen.cs` files inside the output folder before writing new ones.

## Important files

- `src/Tools/Ghost.NativeWrapperGen/Program.cs`
- `src/Tools/Ghost.NativeWrapperGen/Config/WrapperConfig.cs`
- `src/Tools/Ghost.NativeWrapperGen/Parsing/BindingParser.cs`
- `src/Tools/Ghost.NativeWrapperGen/Transform/NamingConventions.cs`
- `src/Tools/Ghost.NativeWrapperGen/Transform/PublicTypeResolver.cs`
- `src/Tools/Ghost.NativeWrapperGen/Emit/WrapperGeneratorEmitter.cs`
- `src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json`

## Config schema

Example: `src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json`

```json
{
  "libraryName": "ufbx",
  "nativeNamespace": "Ghost.Ufbx",
  "wrapperNamespace": "Ghost.Ufbx",
  "nativeTypePrefix": "ufbx_",
  "staticApiClassName": "Ufbx",
  "skipTypes": [
    "NativeAnnotationAttribute",
    "NativeTypeNameAttribute"
  ],
  "wrappers": {
    "defaultKind": "struct",
    "defaultOwnedKind": "class",
    "kinds": {
      "ufbx_node": "ref struct",
      "ufbx_mesh": "ref struct"
    }
  },
  "specialTypes": {
    "strings": [
      {
        "type": "ufbx_string",
        "dataField": "data",
        "lengthField": "length",
        "charSize": 8,
        "encoding": "utf8",
        "emitRawSpanProperty": true,
        "emitStringProperty": true
      }
    ],
    "blobs": [
      {
        "type": "ufbx_blob",
        "dataField": "data",
        "lengthField": "size",
        "elementType": "byte"
      }
    ]
  },
  "ownedTypes": [
    {
      "nativeType": "ufbx_scene",
      "freeFunction": "ufbx_free_scene",
      "retainFunction": "ufbx_retain_scene",
      "wrapperKind": "class"
    }
  ],
  "staticMethods": [
    {
      "nativeFunction": "ufbx_load_file_len",
      "methodName": "LoadFile",
      "throwOnNullReturn": true,
      "failureMessageMember": "description",
      "parameters": [
        { "native": "filename", "adapter": "utf8Path", "publicName": "pathUtf8" },
        { "native": "filename_len", "adapter": "utf8Length", "source": "pathUtf8" },
        { "native": "opts", "adapter": "inValue", "type": "ufbx_load_opts", "publicName": "options", "optionalDefault": true },
        { "native": "error", "adapter": "errorOut", "type": "ufbx_error", "publicName": "error" }
      ]
    }
  ]
}
```

## Base config fields

### `libraryName`

Human-readable library name.

### `nativeNamespace`

Namespace containing the low-level generated bindings.

### `wrapperNamespace`

Namespace used for emitted wrapper files.

### `nativeTypePrefix`

Prefix removed before PascalCase naming.

Examples:

- `ufbx_scene` -> `Scene`
- `ufbx_node_list` -> `NodeList`

### `staticApiClassName`

Name of the global static wrapper class used for methods that do not target a specific wrapper type.

Example:

- `Ufbx`

### `skipTypes`

Types ignored by parsing/emission.

Use this for helper attributes or internal generated artifacts you do not want wrapped.

### `typeNameOverrides`

Overrides wrapper type names.

Example:

- `ufbx_string` -> `UfbxString`

### `publicTypeOverrides`

Overrides member property types.

This is mainly useful for raw native value types that should expose another public type.

## Wrapper kind configuration

The generator no longer assumes everything should be `ref struct`.

Config section:

```json
"wrappers": {
  "defaultKind": "struct",
  "defaultOwnedKind": "class",
  "kinds": {
    "ufbx_scene": "class",
    "ufbx_node": "ref struct",
    "ufbx_mesh": "ref struct"
  }
}
```

Supported values:

- `class`
- `struct`
- `ref struct`
- `readonly ref struct`

Resolution order:

1. `ownedTypes[].wrapperKind`
2. `wrappers.kinds[nativeType]`
3. `wrappers.defaultOwnedKind` for owned types
4. `wrappers.defaultKind` for all other types

Recommended defaults:

- owning types: `class`
- lightweight non-owning value wrappers: `struct`
- hot traversal-only wrappers: `ref struct`

## Special type configuration

This replaces hardcoded `ufbx_string` and `ufbx_blob` behavior.

### String special types

Config shape:

```json
{
  "type": "ufbx_string",
  "dataField": "data",
  "lengthField": "length",
  "charSize": 8,
  "encoding": "utf8",
  "emitRawSpanProperty": true,
  "emitStringProperty": true
}
```

Fields:

- `type`: native struct type name
- `dataField`: pointer field containing character data
- `lengthField`: element count field
- `charSize`: 8, 16, or 32
- `encoding`: `utf8`, `utf16`, or `utf32`
- `emitRawSpanProperty`: whether to emit `<Name>Bytes`
- `emitStringProperty`: whether to emit `<Name>` string property

Generated helper methods look like:

```csharp
public static ReadOnlySpan<byte> AsByteSpan(ufbx_string value)
public static string GetString(ufbx_string value)
```

Generated wrapper members look like:

```csharp
public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
public string Name => NativeWrapperHelpers.GetString(_ptr->name);
```

### Blob special types

Config shape:

```json
{
  "type": "ufbx_blob",
  "dataField": "data",
  "lengthField": "size",
  "elementType": "byte"
}
```

Generated helper methods look like:

```csharp
public static ReadOnlySpan<byte> AsSpan(ufbx_blob value)
```

Generated wrapper members look like:

```csharp
public ReadOnlySpan<byte> Data => NativeWrapperHelpers.AsSpan(_ptr->data);
```

## Owned type configuration

Config shape:

```json
{
  "nativeType": "ufbx_scene",
  "freeFunction": "ufbx_free_scene",
  "retainFunction": "ufbx_retain_scene",
  "wrapperKind": "class"
}
```

Fields:

- `nativeType`: native pointer target type
- `freeFunction`: optional free function in `Api`
- `retainFunction`: reserved for future ownership helpers
- `wrapperKind`: optional per-owned-type wrapper override
- `staticType`: reserved target override for generated static methods

Current effect:

- if `freeFunction` is present, generator emits `Dispose()`
- if wrapper kind is not otherwise specified, owning types use `wrappers.defaultOwnedKind`

## Static method generation

This replaces the old fixed `EntryPointConfig` model.

Now you configure native methods by name and provide parameter adapters.

Config shape:

```json
{
  "nativeFunction": "ufbx_load_memory",
  "methodName": "LoadMemory",
  "throwOnNullReturn": true,
  "failureMessageMember": "description",
  "parameters": [
    { "native": "data", "adapter": "byteSpan", "publicName": "data" },
    { "native": "data_size", "adapter": "byteSpanLength", "source": "data" },
    { "native": "opts", "adapter": "inValue", "type": "ufbx_load_opts", "publicName": "options", "optionalDefault": true },
    { "native": "error", "adapter": "errorOut", "type": "ufbx_error", "publicName": "error" }
  ]
}
```

### Target selection

By default:

- if a native method returns `T*` and `T` has a wrapper, the static method is generated on `T`
- otherwise it is generated on the global static type from `staticApiClassName`

You can override that later with `staticType`.

### Current parameter adapters

Implemented adapters:

- `utf8Path`
  - public parameter: `ReadOnlySpan<byte>`
  - native argument: `sbyte*`
- `utf8Length`
  - emits `(nuint)<source>.Length`
- `byteSpan`
  - public parameter: `ReadOnlySpan<byte>`
  - native argument: `byte*`
- `byteSpanLength`
  - emits `(nuint)<source>.Length`
- `inValue`
  - public parameter: `in T`
  - native argument: `&localCopy`
- `errorOut`
  - hides error buffer from public signature
  - emits a local native error value and passes pointer to native call

### Return handling

Current behavior:

- if native return is `T*` and `T` has a wrapper, emitted return type is wrapper type
- if `throwOnNullReturn` is `true`, a null pointer return throws `InvalidOperationException`
- if `failureMessageMember` is set and the method has an `errorOut` parameter, the generator uses that configured member to build the exception message
- primitive and raw returns are forwarded directly

### Example: `ufbx_load_file_len`

Native signature:

```csharp
public static extern ufbx_scene* ufbx_load_file_len(sbyte* filename, nuint filename_len, ufbx_load_opts* opts, ufbx_error* error);
```

Generated method:

```csharp
public static Scene LoadFile(ReadOnlySpan<byte> pathUtf8, in ufbx_load_opts options = default)
```

### Example: `ufbx_load_memory`

Native signature:

```csharp
public static extern ufbx_scene* ufbx_load_memory(void* data, nuint data_size, ufbx_load_opts* opts, ufbx_error* error);
```

Generated method:

```csharp
public static Scene LoadMemory(ReadOnlySpan<byte> data, in ufbx_load_opts options = default)
```

## List handling

The parser recognizes list structs by shape:

- a `data` field
- a `count` field
- `count` type is `nuint`
- `data` is a pointer type

### Value lists: `T* + count`

Generated as `ReadOnlySpan<T>` properties.

### Pointer lists: `T** + count`

Generated as iterable wrapper list types with:

- `Count`
- indexer
- `foreach` enumerator

### Void lists: `void* + count`

Generated as lightweight wrappers exposing:

- `Count`
- `Data`

## Current naming rules

By default:

1. remove `nativeTypePrefix`
2. split by `_`
3. convert to PascalCase

Examples:

- `ufbx_scene` -> `Scene`
- `ufbx_node` -> `Node`
- `ufbx_node_list` -> `NodeList`

If a member name would equal the wrapper type name, `Value` is appended.

Example:

- wrapper `Props` with field `props` becomes `PropsValue`

## How to use for another library

Create a new config, for example `src/Tools/Ghost.NativeWrapperGen/configs/somelib.json`:

```json
{
  "libraryName": "somelib",
  "nativeNamespace": "Ghost.SomeLib",
  "wrapperNamespace": "Ghost.SomeLib",
  "nativeTypePrefix": "somelib_",
  "staticApiClassName": "SomeLib",
  "wrappers": {
    "defaultKind": "struct",
    "defaultOwnedKind": "class",
    "kinds": {}
  },
  "specialTypes": {
    "strings": [
      {
        "type": "somelib_string",
        "dataField": "data",
        "lengthField": "length",
        "charSize": 8,
        "encoding": "utf8"
      }
    ],
    "blobs": []
  },
  "ownedTypes": [
    {
      "nativeType": "somelib_context",
      "freeFunction": "somelib_free_context",
      "wrapperKind": "class"
    }
  ],
  "staticMethods": [
    {
      "nativeFunction": "somelib_load_file_len",
      "methodName": "LoadFile",
      "throwOnNullReturn": true,
      "failureMessageMember": "message",
      "parameters": [
        { "native": "path", "adapter": "utf8Path", "publicName": "pathUtf8" },
        { "native": "path_len", "adapter": "utf8Length", "source": "pathUtf8" },
        { "native": "options", "adapter": "inValue", "type": "somelib_load_options", "optionalDefault": true },
        { "native": "error", "adapter": "errorOut", "type": "somelib_error" }
      ]
    }
  ]
}
```

Run:

```bash
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/somelib.json --input src/ThridParty/Ghost.SomeLib/Generated --output src/ThridParty/Ghost.SomeLib/Wrapper
```

## Current limitations

This README documents the current implementation, not a future idealized version.

Not implemented yet:

- automatic ownership inference
- automatic retain/release policy generation
- rich enum wrapper generation
- source generator mode
- MSBuild auto-hook integration
- custom adapter plugins outside the built-in adapter set
- direct generation from C headers
- per-member exclusion config
- fixed-buffer string decoding rules

Also note:

- `struct` wrappers that own native resources are possible, but not recommended as a default because copy semantics can duplicate ownership
- current string support assumes pointer + length string structs
- pointer-list wrappers are still emitted as `readonly ref struct` views for performance

## Recommended workflow

When changing the generator:

```bash
dotnet build src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj
dotnet run --project src/Tools/Ghost.NativeWrapperGen/Ghost.NativeWrapperGen.csproj -- --config src/Tools/Ghost.NativeWrapperGen/configs/ufbx.json --input src/ThridParty/Ghost.Ufbx/Generated --output src/ThridParty/Ghost.Ufbx/Warper
dotnet build src/ThridParty/Ghost.Ufbx/Ghost.Ufbx.csproj
```

## Summary

Use `Ghost.NativeWrapperGen` when you already have raw generated C# bindings and want a configurable modern wrapper layer without hand-writing hundreds of types.

The important design idea is now:

- keep parsing generic
- move policy into config
- keep adapters explicit when native signatures need shaping
