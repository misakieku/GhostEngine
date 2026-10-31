# Shader DSL Authoring Guide

**Modules**: `Ghost.DSL`, `Ghost.AssetForge.Core`, `Ghost.Generator`  
**Target Platform**: .NET 10 / Windows / DirectX 12  
**Primary locations**: `src/Editor/Ghost.DSL`, `src/Editor/Ghost.AssetForge.Core/Bakers/ShaderBaker.cs`

This is a syntax and authoring guide for GhostEngine's shader DSL. It is the "how to write a
shader file" companion to [`shader_system_architecture.md`](./shader_system_architecture.md),
which covers the runtime/baking identity model.

The DSL is intentionally small. It glues HLSL to the engine's reflection, property buffers,
pipeline state, and variant system. Almost every mistake below is a *silently ignored block* or
a *silently wrong fallback*, not a compile error.

---

## 1. The three shader file types

| Extension | Declaration | Parser | Baked type |
| --- | --- | --- | --- |
| `.gshdr` | `shader "Name" { ... }` | `GhostShaderParser.g4` | `AssetType.Shader` |
| `.gcomp` | `compute "Name" { ... }` | `GhostComputeShaderParser.g4` | `AssetType.ComputeShader` |
| `.ggraph` | `compute "Name" { ... }` | `GhostComputeShaderParser.g4` | `AssetType.WorkGraph` |

All three are added as `AdditionalFiles` in `src/Directory.Build.props`, so the
`ShaderPropertiesGenerator` sees them and emits the C# property structs.

```xml
<AdditionalFiles Include="**\*.gshdr" />
<AdditionalFiles Include="**\*.gcomp" />
<AdditionalFiles Include="**\*.ggraph" />
```

`Name` is the persistent shader name used for shader IDs, family IDs, and the generated C#
property struct name. Keep it unique and stable.

---

## 2. Hard requirements

These are enforced by `AntlrShaderCompiler` / `DSLShaderCompiler` / `TemplateStitcher`.

1. A `.gshdr` **pass** must declare a mesh shader entry (`ms`). Pixel (`ps`) and amplification
   (`as`) entries are optional.
2. A `.gcomp` must declare at least one `cs` entry, and no more than 8.
3. A `.ggraph` is parsed by the compute grammar, so it must also declare a `cs` entry even though
   it is compiled as a library. See [§5](#5-compute-shaders-gcomp).
4. Entry paths are either the literal `"hlsl_block"` (use the `hlsl { ... }` block in the same
   pass/file) or a relative path to an HLSL file. The entry string is the HLSL function name.
5. Properties go in `properties { ... }`. Do **not** write `cbuffer` / `float4x4` constants
   yourself; the compiler generates the cbuffer and the C# struct.
6. Only `.gshdr` supports templates, and only the built-in `Lit` and `Unlit` templates exist.
7. Use exactly one block of each kind. Repeats do not merge: the visitor takes the first
   `properties` block, the first shader/compute-level `hlsl` block, and the last `includes`,
   `defines`, `keywords`, and pass `hlsl` block. A second `includes` block replaces the first.

---

## 3. Common syntax

```text
shader "Category/Name" [ : "Template" ]
{
    properties { ... }
    includes { ... }
    pipeline { ... }
    keywords { ... }
    strategy "SLOT" "Type" ("path.hlsl");
    strategy "SLOT" "Type" { ...hlsl... }
    hlsl { ... }
    pass "Forward" { ... }
}
```

Block validity differs by file kind:

| Block | `.gshdr` top level | `.gshdr` pass | `.gcomp` / `.ggraph` |
| --- | --- | --- | --- |
| `properties` | yes | no | yes |
| `includes` | yes (templates only) | yes | yes |
| `pipeline` | yes (templates only) | yes | no |
| `defines` | no | yes | yes |
| `keywords` | yes (non-template only) | yes | yes |
| `hlsl` | not used (see §4.3) | yes | yes |
| `strategy` | yes (templates only) | no | no |
| `pass` | yes (non-template only) | - | no |
| `cs` / `ms` / `ps` / `as` | no | yes | `cs` only |

The "templates only" / "non-template only" notes are covered in
[§4.3](#43-scope-gotcha-what-is-actually-used) and [§8](#8-templates-and-strategies).
Do not assume a block that parses is also consumed.

---

## 4. Graphics shaders (`.gshdr`)

### 4.1 Minimal non-template shader

This is the pattern used by `Hidden/Blit`, `Hidden/DebugClassification`, and `Hidden/HDRISky`:

```text
shader "Hidden/Blit"
{
    properties
    {
        Texture2D mainTex;
        SamplerState sampler_mainTex;
        bool linearToSRGB;
    }

    pass "Forward"
    {
        pipeline
        {
            ztest = disabled;
            zwrite = off;
            cull = off;
            blend = opaque;
            color_mask = all;
        }

        includes
        {
            "EngineResources/Shaders/Properties.hlsl";
            "EngineResources/Shaders/Utilities/Color.hlsl";
        }

        hlsl
        {
            struct PSInput
            {
                float4 position : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            [numthreads(4, 1, 1)]
            [outputtopology("triangle")]
            void MSMain(uint gtid : SV_GroupThreadID, out vertices PSInput verts[4], out indices uint3 tris[2])
            {
                SetMeshOutputCounts(4, 2);
                // ...
            }

            float4 PSMain(PSInput input) : SV_TARGET
            {
                float4 color = SampleTexture2D<float4>(g_Properties.mainTex, g_Properties.sampler_mainTex, input.uv.xy);
                return g_Properties.linearToSRGB ? LinearToSRGB(color) : color;
            }
        }

        ms "hlsl_block" : "MSMain";
        ps "hlsl_block" : "PSMain";
    }
}
```

Rules for a non-template shader:

- `pipeline` and `includes` belong **inside each pass**, not at the shader top level (see §4.3).
- `defines` belongs inside the pass.
- A pass can declare `as`, `ms`, and `ps` in any order. Re-declaring the same stage replaces it.
- `ps` is optional. Without it there is no pixel stage; the visibility/classification passes use
  only `ms` in that case.
- There is no `cs` entry type in a `.gshdr` pass. `cs` in a graphics pass is a hard error. Use a
  `.gcomp` instead. Compute passes inside material shaders are generated by templates, not
  hand-written in the DSL.

### 4.2 Pass names and semantics

`PassSemanticExtensions.FromName` is **case-sensitive**:

| Pass name | Semantic |
| --- | --- |
| `"Forward"` | `Forward` |
| `"Visibility"` | `Visibility` |
| `"Shadow"` | `Shadow` |
| `"DeferredTexturing"` | `DeferredTexturing` |
| `"DeferredLighting"` | `DeferredLighting` |
| anything else (`"forward"`, `"Main"`, ...) | `Custom` |

A typo like `pass "forward"` does not fail the build. It produces a `Custom` pass that the render
pipeline will never select.

### 4.3 Scope gotcha: what is actually used

For **non-template** `.gshdr`, only pass-scoped data is used:

- `pass.includes` — yes
- `pass.hlsl` — yes
- `pass.pipeline` — yes, merged over `PipelineState.Default`
- `pass.defines` — yes
- `pass.keywords` — yes, combined after shader-level `keywords`

The following parse but are **ignored** for non-template shaders:

- shader-level `includes`
- shader-level `hlsl`
- shader-level `pipeline`
- shader-level `strategy`

For **template** `.gshdr`, the opposite is true:

- shader-level `includes` — yes, injected into every generated stage
- shader-level `pipeline` — yes, overrides template pass defaults
- shader-level `strategy` — yes
- shader-level `hlsl` — only scanned for `ShadingModelID`; not injected
- `pass` blocks — ignored (the template supplies passes)

This mismatch is the single most common authoring mistake.

### 4.4 Pipeline block

```text
pipeline
{
    ztest = less_equal;
    zwrite = on;
    cull = back;
    blend = opaque;
    color_mask = all;
}
```

Accepted values are identifiers (lower-case is fine because values are lower-cased):

| Key | Values |
| --- | --- |
| `ztest` | `disabled`, `less`, `less_equal`, `equal`, `greater_equal`, `greater`, `not_equal`, `always` |
| `zwrite` | `on`, `off` |
| `cull` | `off`, `front`, `back` |
| `blend` | `opaque`, `alpha`, `additive`, `multiply`, `premultiplied_alpha` |
| `color_mask` | `all`, `none`, or any combination of `r`, `g`, `b`, `a` (e.g. `rgb`) |

Defaults (`PipelineState.Default`): `ztest = less_equal`, `zwrite = on`, `cull = back`,
`blend = opaque`, `color_mask = all`.

**Gotcha**: unknown values silently fall back — an unknown `ztest` becomes `disabled`, an unknown
`cull` becomes `off`, an unknown `blend` becomes `opaque`. A typo changes rendering without an
error.

### 4.5 Entry declarations and file paths

```text
ms "hlsl_block" : "MSMain";
ps "hlsl_block" : "PSMain";
as "Amplification.hlsl" : "ASMain";
```

- `"hlsl_block"` uses the enclosing `pass`'s `hlsl { ... }` block.
- Any other path is read as a file, resolved relative to the `.gshdr` directory (or used
  absolute). The file's contents are appended after the pass `includes`, the generated
  properties, and the pass `hlsl` block. In other words, the pass `hlsl` block is still injected
  above a file-based entry.
- The entry string must match the HLSL function name. It is passed to DXC's `-E`.
- Only the declared stages are compiled. An `ms "..."` is mandatory.

HLSL `#include` paths resolve against the `.gshdr` directory first, then the asset roots
(`-I` directories, e.g. `Assets`). That is why `"EngineResources/Shaders/Properties.hlsl"`
works from any shader.

### 4.6 Multiple passes

```text
shader "Hidden/HDRISky"
{
    properties { ... }

    pass "SkyBaking"
    {
        includes { "EngineResources/Shaders/Properties.hlsl"; }
        hlsl { ... }
        ms "hlsl_block" : "MSMain";
        ps "hlsl_block" : "PSMain_Baking";
    }

    pass "SkyRender"
    {
        includes { "EngineResources/Shaders/Properties.hlsl"; }
        hlsl { ... }
        ms "hlsl_block" : "MSMain";
        ps "hlsl_block" : "PSMain_Render";
    }
}
```

Each pass gets its own pipeline state and its own set of compiled stages. Pass names map to
semantics as described in §4.2. The `properties` block is shared by all passes and lives at the
shader top level.

---

## 5. Compute shaders (`.gcomp`)

### 5.1 Minimal compute shader

```text
compute "Internal/UpdateGPUScene"
{
    properties
    {
        uint gpuSceneBuffer;
        uint updateBuffer;
        uint updateCount;
        uint removeBuffer;
        uint removeCount;
    }

    includes
    {
        "EngineResources/Shaders/Properties.hlsl";
        "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl";
    }

    hlsl
    {
        [numthreads(64, 1, 1)]
        void CSMain(uint3 dispatchThreadID : SV_DispatchThreadID)
        {
            RWByteAddressBuffer gpuSceneBuffer = GET_BUFFER(g_Properties.gpuSceneBuffer);

            if (dispatchThreadID.x < g_Properties.updateCount)
            {
                StructuredBuffer<UpdateInstanceData> updateBuffer = GET_BUFFER(g_Properties.updateBuffer);
                UpdateInstanceData updateData = updateBuffer[dispatchThreadID.x];
                // ...
            }
        }
    }

    cs "hlsl_block" : "CSMain";
}
```

Rules:

- `includes`, `defines`, `properties`, and `keywords` are top-level and apply to every entry.
- `hlsl` is top-level. All entries may point at functions in that same block.
- Multiple `cs` entries are allowed (max 8). They are compiled into separate DXIL blobs and
  selected at runtime by entry index:

  ```csharp
  computeContext.SetActiveCompute(computeShader, entryIndex);
  computeContext.DispatchCompute(x, y, z);
  ```

- The entry type token is `cs`. The error message mentions `compute` but the parser only accepts
  `cs`; do not write `compute "hlsl_block" : "CSMain";`.
- A `cs` entry may also reference a file: `cs "MyPass.hlsl" : "CSMain";`.

### 5.2 Defines

```text
defines
{
    FEATURE_A;
    FEATURE_B;
}
```

Each identifier is passed to DXC as `-D NAME` (a bare `#define NAME`). There is no `=` value
syntax in the DSL.

---

## 6. GPU work graphs (`.ggraph`)

A `.ggraph` uses the **compute** grammar and the **compute** baker path up to the point of
compilation. It is then compiled as a DXC **library** (`-T lib_6_8`) with an empty entry point.
Work-graph node functions are ordinary HLSL annotated with `[Shader("node")]`, and the DSL entry
string is never used as an export.

Therefore every `.ggraph` needs one dummy `cs` entry purely to satisfy the compute grammar:

```text
compute "Internal/MeshletCullGraph"
{
    properties { ... }

    includes
    {
        "EngineResources/Shaders/Properties.hlsl";
        "EngineResources/Shaders/Mesh.hlsl";
    }

    hlsl
    {
        struct WorkGraphDispatchGridRecord
        {
            uint3 dispatchGrid : SV_DispatchGrid;
        };

        [Shader("node")]
        [NodeLaunch("broadcasting")]
        [NodeIsProgramEntry]
        [NodeMaxDispatchGrid(65535, 1, 1)]
        [NumThreads(64, 1, 1)]
        void InstanceCullNode(
            DispatchNodeInputRecord<WorkGraphDispatchGridRecord> inputRecord,
            uint3 dispatchThreadId : SV_DispatchThreadID,
            [NodeId("HierarchyTraverseNode")] [MaxRecords(64)] NodeOutput<HierarchyNodeRecord> hierarchyQueue)
        {
            // ...
        }
    }

    // Required by the DSL, ignored by the work-graph compiler.
    cs "hlsl_block" : "NotARealEntryPoint";
}
```

The entry name is irrelevant. `cs "hlsl_block" : "RandomString";` is valid and idiomatic here.
Do not waste time making it match anything.

If you use a file instead of an `hlsl` block, the dummy entry still needs a valid path:

```text
cs "MeshletCullGraph.hlsl" : "ignored";
```

---

## 7. Properties

### 7.1 DSL syntax

```text
properties
{
    float roughness = 0.4;
    float metallic = 0.0;
    float4 tint = float4(1.0, 1.0, 1.0, 1.0);
    bool alphaClip = false;
    uint baseColor;
    uint sampler;
    Texture2D mainTex;
    SamplerState sampler_mainTex;
}
```

- Format: `Type name [= default];`
- `Type` is a single identifier. No generics or spaces (`RWTexture2D<float>` is not valid).
- Supported defaults: a decimal number (`0.4`), an identifier (`true`, `false`, another
  identifier), or an identifier call (`float4(1.0, 1.0, 1.0, 1.0)`).
- **Negative numbers and exponent notation are not supported by the grammar.** `-1.0` and `1e-3`
  are parse errors. Use `0.0` and negate in HLSL, or omit the default.
- The default only initializes the generated C# struct field. It is not written into the HLSL
  cbuffer; HLSL receives whatever the material data contains.

### 7.2 Type mapping

The compiler emits a flat struct for the property buffer and a `cbuffer` for it. Texture,
sampler, and buffer handle types are **stored as `uint` bindless indices** in HLSL and C#.

| DSL type | HLSL field | C# field | Size |
| --- | --- | --- | --- |
| `float`, `int`, `uint`, `bool` | same (`bool` stays `bool`) | `float`, `int`, `uint`, `uint` | 4 |
| `float2/3/4`, `int2/3/4`, `uint2/3/4`, `bool2/3/4` | same | same (`boolN` -> `uintN`) | 8 / 12 / 16 |
| `float2x2` | `float2x2` | `float2x2` | 32 |
| `float3x3`, `float3x4` | same | same | 48 |
| `float4x4`, `matrix4x4`, `float4x3` | same | same | 64 |
| `int2x4` | `int2x4` | `int2x4` | 32 |
| `Texture2D`, `Texture3D`, `TextureCube`, `Texture2DArray`, `TextureCubeArray` | `uint` | `uint` | 4 |
| `SamplerState`, `Sampler` | `uint` | `uint` | 4 |
| `ByteAddressBuffer`, `StructuredBuffer` | `uint` | `uint` | 4 |

Anything not in this table is emitted verbatim in HLSL while the C# generator assumes 4 bytes.
That mismatch corrupts the buffer layout (or fails to compile), so do not use custom struct or
handle types as properties. In particular, `quaternion` is a C#-side type and has no HLSL
definition; use `float4` if you need four floats.

Because textures and samplers are indices, access them through the engine helpers, not as
`Texture2D` objects:

```hlsl
float4 c = SampleTexture2D<float4>(g_Properties.mainTex, g_Properties.sampler_mainTex, uv);
RWTexture2D<uint2> vis = GET_TEXTURE2D(g_Properties.visBufferIndex);
ByteAddressBuffer scene = GET_BUFFER(g_Properties.gpuSceneBuffer);

if (IS_VALID_BUFFER(g_Properties.gpuSceneBuffer)) { ... }
if (IS_VALID_TEXTURE(g_Properties.depthTexture)) { ... }
```

`GET_*` / `IS_VALID_*` are defined in `EngineResources/Shaders/Common.hlsl`, which is included
by `Properties.hlsl`.

### 7.3 Generated HLSL

The compiler writes (for a shader named `Hidden/Blit`):

```hlsl
struct HiddenBlitShaderProperties
{
    uint mainTex;
    uint sampler_mainTex;
    bool linearToSRGB;
};

cbuffer PassProperties : register(b3, space0)
{
    HiddenBlitShaderProperties g_Properties;
};

typedef HiddenBlitShaderProperties MaterialProperties;
typedef HiddenBlitShaderProperties ComputeProperties;
```

So in HLSL you always read `g_Properties.<field>`, never a hand-written cbuffer.

Template shaders get the same struct, but prepended with the template's base properties, and the
`MaterialProperties` alias. See §8.4.

### 7.4 Generated C#

`ShaderPropertiesGenerator` emits a `LayoutKind.Explicit` struct in namespace
`Ghost.Engine.ShaderProperties`:

```csharp
namespace Ghost.Engine.ShaderProperties
{
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public partial struct GhostStandardLitShaderProperties()
    {
        [FieldOffset(0)]  public float4 doubleSidedConstants = new float4(1.0f, 1.0f, 1.0f, 0.0f);
        [FieldOffset(16)] public uint alphaClip = 0u;
        [FieldOffset(20)] public float alphaClipThreshold = 0.5f;
        [FieldOffset(24)] public uint materialFeatureMask = 0u;

        [FieldOffset(28)] public uint baseColor;
        [FieldOffset(32)] public uint sampler;
        [FieldOffset(36)] public float roughness = 0.4f;
        [FieldOffset(40)] public float metallic = 0.0f;
        [FieldOffset(44)] public float occlusion = 1.0f;

        public const string SHADER_NAME = "Ghost/StandardLit";
        public const string TEMPLATE_NAME = "Lit";
    }
}
```

#### Struct name rule

Take the shader name, split on `/ \ . space -`, upper-case the first character of each part, and
concatenate. Append `ShaderProperties` unless the result already ends in `Properties` or
`ShaderProperties`.

| Shader name | Struct name |
| --- | --- |
| `"Ghost/StandardLit"` | `GhostStandardLitShaderProperties` |
| `"Hidden/Blit"` | `HiddenBlitShaderProperties` |
| `"Internal/UpdateGPUScene"` | `InternalUpdateGPUSceneShaderProperties` |

Usage:

```csharp
engine.RenderEngine.ResourceManager.SetMaterialProperty(mat, new GhostStandardLitShaderProperties
{
    roughness = 0.75f,
});
```

`SetMaterialProperty<T>` copies the raw bytes of the struct, so the explicit field offsets are
what bind the C# struct to the HLSL cbuffer. Do not reorder properties casually: the layout hash
is part of shader compatibility.

### 7.5 Template base properties

When a shader inherits a template, the generated struct always starts with the template's base
properties, then your properties:

| Template | Base properties |
| --- | --- |
| `Lit` | `float4 doubleSidedConstants`, `bool alphaClip`, `float alphaClipThreshold`, `uint materialFeatureMask` |
| `Unlit` | `float4 doubleSidedConstants`, `bool alphaClip`, `float alphaClipThreshold` |

Your properties are appended in declaration order. **If you redeclare a base property name, your
declaration is silently dropped** (base wins). Do not define `alphaClip` or
`alphaClipThreshold` again.

---

## 8. Templates and strategies

### 8.1 Inheritance

```text
shader "MyShader/SimpleLit" : "Lit"
{
    properties { ... }
    strategy "DEFERREDLIGHTING_STRATEGY" "SimpleLitDeferredLighting" (".../SimpleLit.hlsl");
}
```

- Only `Lit` and `Unlit` exist. The name is matched case-sensitively (`"lit"` fails).
- A template supplies the passes, stages, pipeline defaults, keywords, and per-pass engine
  properties. Do not declare `pass` blocks in a template shader.
- Do not declare `ms` / `ps` / `as` entries in a template shader. The template's stages are used.
- Shader-level `includes` are injected into every generated stage of every pass.
- Shader-level `pipeline` overrides the template's pass pipeline defaults.
- Shader-level `hlsl` is not injected. Use strategies instead.

| Template | Passes generated |
| --- | --- |
| `Lit` | `Visibility`, `Shadow`, `DeferredTexturing`, `DeferredLighting` |
| `Unlit` | `Visibility`, `Shadow`, `DeferredTexturing` |

`Visibility` and `Shadow` are mesh + pixel passes. `DeferredTexturing` is a compute pass.
`Lit` additionally generates the `DeferredLighting` compute pass.

### 8.2 Strategy syntax

A strategy fills a named slot in a template pass. Two equivalent forms:

```text
// External file
strategy "SLOT_NAME" "StrategyStruct" ("relative/or/asset/path.hlsl");

// Inline HLSL
strategy "SLOT_NAME" "StrategyStruct"
{
    struct StrategyStruct { ... };
}
```

- The first string is the slot name. It must be one accepted by the template or resolution fails
  with a list of valid slots.
- The second string is the struct type name. If omitted, the slot name is used as the type name.
- When the type name differs from the slot name, the stitcher emits
  `#define SLOT_NAME StrategyStruct` before the strategy code. This is how
  `ALPHA_STRATEGY::Create()` in the template binds to `MyAlphaStruct`.
- If you omit the type name, the struct you define must be named exactly the slot name, because
  no alias is emitted.
- A strategy file path is treated as an HLSL include: the stitcher emits `#include "path"` (or
  inlines the file if the path resolves relative to the process working directory). It is
  resolved relative to the shader directory first, then the asset roots, the same way an
  `#include` written in the shader is resolved.
- Strategies are only valid on template shaders. They are ignored on non-template shaders.

### 8.3 Strategy slots and required methods

The strategy is instantiated once per shader invocation with a static `Create()` and then used by
the template. All methods must be implemented if you override a slot; there is no partial
override and no inheritance.

#### `ALPHA_STRATEGY` (Lit + Unlit; Visibility and Shadow passes)

```hlsl
struct MyAlphaStrategy
{
    static MyAlphaStrategy Create()
    {
        return (MyAlphaStrategy)0;
    }

    float GetAlphaCoverage(uint materialBufferIndex, float2 uv)
    {
        return 1.0f;
    }
};
```

#### `DEFERREDTEXTURING_STRATEGY` (Lit + Unlit; DeferredTexturing pass)

```hlsl
struct MyDeferredTexturing
{
    static MyDeferredTexturing Create()
    {
        return (MyDeferredTexturing)0;
    }

    SurfaceData GetSurfaceData(MaterialContext ctx, MaterialProperties props)
    {
        SurfaceData surface = (SurfaceData)0;
        surface.shadingModel = SHADING_MODEL_ID;
        surface.albedo = float3(0.73f, 0.73f, 0.73f);
        surface.normalWS = ctx.normalWS;
        surface.metallic = 0.0f;
        surface.roughness = 0.5f;
        surface.occlusion = 1.0f;
        surface.emissive = float3(0.0f, 0.0f, 0.0f);
        return surface;
    }
};
```

#### `DEFERREDLIGHTING_STRATEGY` (Lit only; DeferredLighting pass)

```hlsl
struct MyDeferredLighting
{
    static const uint ShadingModelID = 2u;

    static MyDeferredLighting Create()
    {
        return (MyDeferredLighting)0;
    }

    BSDFData GetBSDFData(float3 positionRWS, float3 T, SurfaceData surface) { ... }
    PreLightData GetPreLightData(ShadingContext ctx, float3 V, inout BSDFData bsdf) { ... }
    bool IsNonZeroBSDF(float3 V, float3 L, BSDFData bsdf) { ... }
    void ClampRoughness(inout BSDFData bsdf, float minRoughness) { ... }
    DirectLighting EvaluateDirectLighting(BSDFData bsdf, PreLightData preLightData, float3 V, float3 L, float3 lightColor) { ... }
    IndirectLighting EvaluateIndirectLighting(ShadingContext ctx, BSDFData bsdf, PreLightData preLightData, float3 V) { ... }
    LightLoopOutput PostEvaluateBSDF(AggregateLighting lighting, BSDFData bsdf, PreLightData preLightData, float3 V) { ... }
};
```

The fallback implementations live in
`src/Editor/Ghost.DSL/Templates/Lit/Lit_Common.template.hlsl` and
`src/Editor/Ghost.DSL/Templates/Unlit/Unlit_Common.template.hlsl`. Use them as the exact
signature reference.

#### Shading model ID

The stitcher scans the `DEFERREDLIGHTING_STRATEGY` code (file or inline) — or the shader-level
`hlsl` block — for a literal `ShadingModelID = <number>` and uses that value for
`#define SHADING_MODEL_ID <n>`. If it does not find one, it defaults to `1u`.

```hlsl
static const uint ShadingModelID = 2u;   // -> #define SHADING_MODEL_ID 2u
```

Every custom Lit material defaults to shading model `1` unless it declares its own.

### 8.4 Property aliases

Inside template HLSL, use the aliases rather than a concrete generated name:

- `MaterialProperties` — the shader's material property struct, emitted for template
  `DeferredTexturing` passes and for non-template shaders.
- `ComputeProperties` — emitted by the non-template path (`.gcomp`, `.ggraph`, and non-template
  `.gshdr`); use it in compute code so the code is independent of the shader name.

This lets a strategy be written once and reused by any shader that inherits the template, even
though each shader produces a differently named C#/HLSL struct. In a strategy, always use
`MaterialProperties` rather than the concrete generated name.

```hlsl
SurfaceData GetSurfaceData(MaterialContext ctx, MaterialProperties props)
{
    float3 albedo = SampleTexture2D<float4>(props.baseColor, props.sampler, ctx.uv).rgb;
    // ...
}
```

`MaterialProperties` is generated for the `DeferredTexturing` pass, for passes with a strategy,
and for non-template shaders. It is **not** generated for the `DeferredLighting` pass, which uses
the engine-owned `DeferredLightingShaderProperties` instead. A `DEFERREDLIGHTING_STRATEGY` therefore
must not reference `MaterialProperties`; it receives the decoded `SurfaceData` and uses the
`ShadingContext` instead.

Likewise, `ComputeProperties` is never emitted on the template path. Use it only in `.gcomp`,
`.ggraph`, and non-template shaders.

### 8.5 Stateful strategies

`Create()` is the constructor. The instance is created once and the same instance flows through
the template's calls, so field mutations persist across methods. This is intentional and is how
the light loop carries per-pixel state.

```hlsl
struct MyStrategy
{
    float v;

    static MyStrategy Create()
    {
        MyStrategy s;
        s.v = 1.0f;
        return s;
    }

    void Func()
    {
        float value = v;   // 1.0
        v = 2.0f;
    }

    void FuncAfter()
    {
        float value = v;   // 2.0
    }
};
```

Notes:

- Methods are instance methods, not static. `Create` is the only static member (plus the
  optional `static const uint ShadingModelID`).
- Do not rely on a default-initialized instance; always go through `Create()`.
- `return (MyStrategy)0;` is the idiomatic zero-initialization when there is no state.
- `Create()` is called from the template's call site, so it runs once per shader invocation
  (thread), not once per module. Keep it cheap and deterministic.

---

## 9. Keywords

A `keywords` block declares mutually exclusive compile-time variants. Each statement is one
group: the compiler emits one variant per keyword in the group and adds that keyword as a define
(`-D`).

```text
keywords
{
    SHADOW_MODE: _SHADOW_PCF, _SHADOW_VSM, _SHADOW_RIS;
    _DEBUG_OVERLAY;
}
```

- `GROUP: A, B, C;` — group name explicit. The name is metadata; the defines are the keywords.
- `A, B, C;` or `A;` — group name defaults to the first keyword.
- Keywords must be identifiers (`_NAME`, `NAME`), not strings.
- In a **non-template** `.gshdr`, a shader-level block applies to every pass; a pass-level block
  applies to that pass only and is appended after the shader-level groups.
- In a **template** `.gshdr`, user `keywords` blocks are ignored. The template defines the
  keyword groups for each generated pass (`Lit`'s `DeferredLighting` pass, for example, generates
  the `SHADOW_MODE` group).
- In a `.gcomp` / `.ggraph`, the block is top-level and applies to all entries.
- Variants are the **cartesian product** of all groups: 2 groups with 3 and 2 keywords produce 6
  variants, each containing exactly one keyword from each group.
- There is no "no keyword selected" state. If a pass must also compile without a keyword, add a
  sentinel such as `_SHADOW_NONE` and test for it explicitly.
- Groups are combined as a flat permutation list; a keyword does not imply another keyword.

---

## 10. Pitfall checklist

Use this when a shader "compiles" but does nothing, or when properties read as zero.

1. `ms` entry missing from a `.gshdr` pass -> bake error.
2. `cs` entry missing from a `.gcomp` / `.ggraph` -> bake error. `.ggraph` needs a dummy one.
3. `cs` used inside a `.gshdr` pass -> hard error. Use a separate `.gcomp`.
4. Pass name not exact PascalCase -> silently becomes `Custom`.
5. Non-template shader uses top-level `pipeline` / `includes` / `hlsl` -> silently ignored;
   move them into each `pass`.
6. Template shader uses `pass` or `hlsl` for user code -> ignored; use `strategy`.
7. `strategy` on a non-template shader -> ignored.
8. Strategy struct type name differs from the slot but no type string is given -> `#define`
   alias is not emitted and the slot macro is undefined.
9. Strategy does not implement every required method -> compile error at the template call site.
10. Texture/sampler property used as a `Texture2D` -> it is a `uint` bindless index; use
    `GET_TEXTURE2D` / `SampleTexture2D` / `GET_BUFFER`.
11. Hand-written `cbuffer` for material properties -> duplicate/competing register; delete it and
    use `g_Properties`.
12. Negative or exponent default values (`-1.0`, `1e-3`) -> DSL parse error. Use `0.0` or no
    default.
13. Property type not in the supported table -> C# layout assumes 4 bytes and corrupts offsets.
14. Redeclaring a template base property (`alphaClip`, `alphaClipThreshold`, ...) -> silently
    dropped; the base declaration wins.
15. Two `includes` / `defines` / `keywords` blocks of the same kind -> later block replaces the
    earlier one instead of merging.
16. Typo in a `pipeline` value -> silently falls back (`ztest` -> disabled, `cull` -> off,
    `blend` -> opaque).
17. Custom Lit shading model without `static const uint ShadingModelID = n;` -> defaults to `1u`
    and may collide with `SimpleLit`.
18. Expecting a keyword group to have an "off" variant -> there is none; add a sentinel keyword.
19. Entry path is a "virtual" engine path but the file is read as a path relative to the
    `.gshdr`. Entry files are read from disk; only `#include` and strategy paths use asset roots.
20. Declaring `keywords` in a template shader -> ignored; the template owns the keyword groups.
21. Template name not `Lit` or `Unlit`, or wrong case -> "Unknown shader template".

---

## 11. Reference examples

| Example | Shows |
| --- | --- |
| [`Blit.gshdr`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Blit.gshdr) | Minimal mesh + pixel pass, texture properties |
| [`DebugClassification.gshdr`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/DebugClassification.gshdr) | Pass-level includes, pipeline override, `uint` properties |
| [`HDRISky.gshdr`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Lighting/HDRISky.gshdr) | Multiple passes with separate pixel entries |
| [`UpdateGPUScene.gcomp`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/UpdateGPUScene.gcomp) | Compute shader with buffer properties |
| [`RisLightSampling.gcomp`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Lighting/RisLightSampling.gcomp) | Multi-include compute shader |
| [`MeshletCullGraph.ggraph`](../../src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/MeshletCullGraph.ggraph) | Work graph with node records and dummy `cs` entry |
| [`SimpleLit.gshdr`](../../src/Test/TestGame/Assets/Shaders/SimpleLit.gshdr) | `Lit` template with file + inline strategies |
| [`StandardLit.gshdr`](../../src/Test/TestGame/Assets/Shaders/StandardLit.gshdr) | Template shader used by `TestGame` |
| [`Lit_Common.template.hlsl`](../../src/Editor/Ghost.DSL/Templates/Lit/Lit_Common.template.hlsl) | Lit strategy fallbacks and exact method signatures |
| [`Unlit_Common.template.hlsl`](../../src/Editor/Ghost.DSL/Templates/Unlit/Unlit_Common.template.hlsl) | Unlit strategy fallbacks |
| [`Visibility.template.hlsl`](../../src/Editor/Ghost.DSL/Templates/Common/Visibility.template.hlsl) | How `ALPHA_STRATEGY` is consumed |

Grammar and compiler sources:

- [`GhostShaderLexer.g4`](../../src/Editor/Ghost.DSL/Grammar/GhostShaderLexer.g4)
- [`GhostShaderParser.g4`](../../src/Editor/Ghost.DSL/Grammar/GhostShaderParser.g4)
- [`GhostComputeShaderParser.g4`](../../src/Editor/Ghost.DSL/Grammar/GhostComputeShaderParser.g4)
- [`AntlrShaderCompiler.cs`](../../src/Editor/Ghost.DSL/ShaderParser/AntlrShaderCompiler.cs)
- [`DSLShaderCompiler.cs`](../../src/Editor/Ghost.DSL/ShaderCompiler/DSLShaderCompiler.cs)
- [`TemplateStitcher.cs`](../../src/Editor/Ghost.DSL/ShaderCompiler/Templates/TemplateStitcher.cs)
- [`ShaderPropertiesGenerator.cs`](../../src/Runtime/Ghost.Generator/ShaderPropertiesGenerator.cs)
- [`ShaderBaker.cs`](../../src/Editor/Ghost.AssetForge.Core/Bakers/ShaderBaker.cs)
