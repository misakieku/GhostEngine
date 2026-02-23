# Ghost.Nvtt – Usage Guide

`Ghost.Nvtt` is a managed C# wrapper over the NVIDIA Texture Tools 3 (nvtt) native library.
All wrapper classes are in the `Ghost.Nvtt` namespace.  Add a single `using Ghost.Nvtt;` and you have access to every wrapper class and every enum.

---

## Quick-start: compress a PNG to BC7 DDS

```csharp
using Ghost.Nvtt;

// 1. Load source image
using var surface = new NvttSurface();
surface.Load("albedo.png", out bool hasAlpha);

// 2. Convert to linear before compression
surface.ToLinearFromSrgb();

// 3. Build compression options
using var compOpts = new NvttCompressionOptions();
compOpts.SetFormat(NvttFormat.NVTT_Format_BC7);
compOpts.SetQuality(NvttQuality.NVTT_Quality_Production);

// 4. Build output options (write to file)
using var outOpts = new NvttOutputOptions();
outOpts.SetFileName("albedo.dds");
outOpts.SetOutputHeader(true);

// 5. Create context and compress the full mip chain
using var ctx = new NvttContext();
ctx.SetCudaAcceleration(NvttGlobal.IsCudaSupported);

int mipmaps = surface.CountMipmaps();
ctx.OutputHeader(surface, mipmaps, compOpts, outOpts);

using var mip = surface.Clone();
for (int m = 0; m < mipmaps; m++)
{
    ctx.Compress(mip, 0, m, compOpts, outOpts);
    if (!mip.BuildNextMipmap(NvttMipmapFilter.NVTT_MipmapFilter_Box))
        break;
}
```

---

## Capturing compressed data in memory

Instead of writing to a file, provide output handlers to accumulate bytes:

```csharp
using var outOpts = new NvttOutputOptions();
var buffer = new List<byte>();

outOpts.SetOutputHandler(
    beginImage: (size, w, h, d, face, mip) => { /* optional: per-mip notification */ },
    outputData: (data, hasAlpha) => { buffer.AddRange(data); return true; },
    error:      err => Console.Error.WriteLine(NvttGlobal.ErrorString(err)));
outOpts.SetOutputHeader(true);
```

---

## Cube maps

```csharp
using var cube = new NvttCubeSurface();
cube.Load("skybox.dds");

// Generate specular mip chain (cosine-power filter)
using var filtered = cube.CosinePowerFilter(size: 128, cosinePower: 64f);

using var compOpts = new NvttCompressionOptions();
compOpts.SetFormat(NvttFormat.NVTT_Format_BC6H_UF16);

using var outOpts = new NvttOutputOptions();
outOpts.SetFileName("skybox_spec.dds");
outOpts.SetOutputHeader(true);

using var ctx = new NvttContext();
int mipmaps = filtered.MipmapCount;
ctx.OutputHeaderCube(filtered, mipmaps, compOpts, outOpts);
for (int m = 0; m < mipmaps; m++)
    ctx.CompressCube(filtered, m, compOpts, outOpts);
```

---

## Loading an existing DDS (SurfaceSet)

`NvttSurfaceSet` reads DDS files that may contain multiple faces and mip levels
without decoding them one-by-one:

```csharp
using var set = new NvttSurfaceSet();
set.LoadDDS("texture_array.dds");

Console.WriteLine($"{set.FaceCount} faces, {set.MipmapCount} mips, " +
                  $"{set.Width}x{set.Height}");

// Access the raw pointer for face 0, mip 0 (borrowed – do not dispose)
var surfacePtr = set.GetSurfacePtr(faceId: 0, mipId: 0);
```

---

## Batch compression

Use `NvttBatchList` to compress many surfaces in a single driver call (better
GPU utilisation):

```csharp
using var batch   = new NvttBatchList();
using var compOpts = new NvttCompressionOptions();
compOpts.SetFormat(NvttFormat.NVTT_Format_BC1);

// Build one NvttOutputOptions per destination
var surfaces = LoadAllSurfaces(); // user-supplied IEnumerable<NvttSurface>
var outOptsList = new List<NvttOutputOptions>();

foreach (var (surf, path) in surfaces.Zip(paths))
{
    var oo = new NvttOutputOptions();
    oo.SetFileName(path);
    outOptsList.Add(oo);
    batch.Append(surf, face: 0, mipmap: 0, oo);
}

using var ctx = new NvttContext();
ctx.CompressBatch(batch, compOpts);

foreach (var oo in outOptsList) oo.Dispose();
```

---

## Timing

```csharp
using var ctx = new NvttContext();
ctx.EnableTiming(true, detailLevel: 1);

// ... compress ...

using var tc = new NvttTimingContext(detailLevel: 1);
// OR use ctx.GetTimingContextPtr() to borrow the context's own timing data.
```

---

## Global message callback

```csharp
using var token = NvttGlobal.SetMessageCallback((severity, error, description) =>
{
    if (severity == NvttSeverity.NVTT_Severity_Error)
        throw new Exception($"nvtt error {error}: {description}");
    Console.WriteLine($"[nvtt] {severity}: {description}");
});

// ... do work ...

token.Dispose(); // unregisters the callback
```

---

## Image comparison helpers

```csharp
using var reference = new NvttSurface();
reference.Load("original.png", out _);

using var compressed = new NvttSurface();
compressed.Load("compressed.png", out _);

float rms     = NvttGlobal.RmsError(reference, compressed);
float cielab  = NvttGlobal.RmsCIELabError(reference, compressed);

using var diff = NvttGlobal.Diff(reference, compressed, scale: 4f);
diff.Save("diff.png");
```

---

## Common enums (all available without qualification after `using Ghost.Nvtt`)

| Enum | Key values |
|------|-----------|
| `NvttFormat` | `NVTT_Format_BC1` … `NVTT_Format_BC7`, `NVTT_Format_BC6H_UF16`, `NVTT_Format_RGBA` |
| `NvttQuality` | `NVTT_Quality_Fastest`, `NVTT_Quality_Normal`, `NVTT_Quality_Production`, `NVTT_Quality_Highest` |
| `NvttMipmapFilter` | `NVTT_MipmapFilter_Box`, `NVTT_MipmapFilter_Triangle`, `NVTT_MipmapFilter_Kaiser` |
| `NvttResizeFilter` | `NVTT_ResizeFilter_Box`, `NVTT_ResizeFilter_Triangle`, `NVTT_ResizeFilter_Kaiser` |
| `NvttRoundMode` | `NVTT_RoundMode_None`, `NVTT_RoundMode_ToPreviousPowerOfTwo`, `NVTT_RoundMode_ToNextPowerOfTwo` |
| `NvttTextureType` | `NVTT_TextureType_2D`, `NVTT_TextureType_3D`, `NVTT_TextureType_Cube` |
| `NvttCubeLayout` | `NVTT_CubeLayout_VerticalCross`, `NVTT_CubeLayout_HorizontalCross`, `NVTT_CubeLayout_Column` |
| `EdgeFixup` | `NVTT_EdgeFixup_None`, `NVTT_EdgeFixup_Stretch`, `NVTT_EdgeFixup_Warp` |

---

## Ownership rules

| Returns | Ownership |
|---------|-----------|
| `new NvttSurface(...)` constructor overload accepting a raw pointer | **Takes** ownership – dispose when done |
| `NvttSurface.Clone()` | Caller owns result |
| `NvttSurface.CreateSubImage()`, `CreateToksvigMap()` | Caller owns result |
| `NvttCubeSurface.Unfold()`, `IrradianceFilter()`, `CosinePowerFilter()`, `FastResample()` | Caller owns result |
| `NvttGlobal.Diff()`, `Histogram()`, `HistogramRange()` | Caller owns result |
| `NvttCubeSurface.FacePtr()`, `NvttSurfaceSet.GetSurfacePtr()` | **Borrowed** – do NOT dispose |
| `NvttContext.GetTimingContextPtr()` | **Borrowed** – do NOT dispose |
