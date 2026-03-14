using Ghost.Nvtt;
using Ghost.Nvtt.Wrapper;
using Misaki.HighPerformance.Image;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Drives the NVTT compression + mipmap pipeline for a single texture asset.
///
/// Responsibilities:
///   1. Accept raw decoded pixel bytes + settings.
///   2. Determine the cache file path (<c>CachesFolderPath/TextureCache/&lt;guid&gt;_&lt;hash&gt;.dds</c>).
///   3. If the cache is already valid (hash matches), skip compression.
///   4. Otherwise run the full NVTT pipeline and write the DDS to the cache file.
///
/// The caller owns opening/closing all streams; this class only takes spans and paths.
/// </summary>
internal static unsafe class TextureProcessor
{
    private const string _TEXTURE_CACHE_SUBFOLDER = "TextureCache";

    /// <summary>
    /// Compresses <paramref name="pixelData"/> according to <paramref name="settings"/>
    /// and writes the result to the texture cache.
    ///
    /// Returns the absolute path of the cache file on success.
    /// The cache file is skipped if it already exists with a matching content hash.
    /// </summary>
    public static string CompressToCache(
        string cachesFolderPath,
        Guid assetId,
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        bool isFloat,
        ColorComponents colorComponents,
        TextureAssetSettings settings)
    {
        // --- derive cache path --------------------------------------------------
        var cacheDir = Path.Combine(cachesFolderPath, _TEXTURE_CACHE_SUBFOLDER);
        Directory.CreateDirectory(cacheDir);

        var settingsHash = ComputeSettingsHash(settings);
        var cacheFileName = $"{assetId:N}_{settingsHash:X16}.dds";
        var cachePath = Path.Combine(cacheDir, cacheFileName);

        // --- check validity: same file name = same settings hash = already done -
        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        // --- delete any stale cache entries for this asset ----------------------
        foreach (var stale in Directory.EnumerateFiles(cacheDir, $"{assetId:N}_*.dds"))
        {
            File.Delete(stale);
        }

        // --- run NVTT pipeline --------------------------------------------------
        RunNvttPipeline(cachePath, pixelData, width, height, isFloat, colorComponents, settings);

        return cachePath;
    }

    private static void RunNvttPipeline(
        string outputPath,
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        bool isFloat,
        ColorComponents colorComponents,
        TextureAssetSettings settings)
    {
        using var surface = new NvttSurfaceHandle();
        using var compOpts = new NvttCompressionOptionsHandle();
        using var outOpts = new NvttOutputOptionsHandle();
        using var ctx = new NvttContextHandle();

        // ---- 1. load pixels into NVTT -----------------------------------------
        // Misaki.HighPerformance.Image always decodes to RGBA channel order.
        // Float images → RGBA_32F, byte images → BGRA_8UB.
        // NOTE: NVTT BGRA_8UB expects Blue in byte[0]; stb decodes RGBA so we need
        // to pass RGBA. There is no RGBA_8UB enum — we swizzle after load instead.
        var inputFormat = isFloat
            ? NvttInputFormat.NVTT_InputFormat_RGBA_32F
            : NvttInputFormat.NVTT_InputFormat_BGRA_8UB; // we'll swizzle RB below

        surface.SetImageData(inputFormat, width, height, 1, pixelData);

        // stb gives us RGBA byte order; NVTT BGRA_8UB reads it as BGRA,
        // so channels R and B are swapped — fix with swizzle(2,1,0,3).
        if (!isFloat)
        {
            surface.Swizzle(2, 1, 0, 3);
        }

        // ---- 2. resize ---------------------------------------------------------
        var maxExtent = (int)settings.Sampler.MaxSize;
        if (settings.Advanced.StretchToPowerOfTwo)
        {
            surface.ResizeMakeSquare(maxExtent,
                NvttRoundMode.NVTT_RoundMode_ToNearestPowerOfTwo,
                NvttResizeFilter.NVTT_ResizeFilter_Box);
        }
        else if (surface.Width > maxExtent || surface.Height > maxExtent)
        {
            surface.ResizeMax(maxExtent,
                NvttRoundMode.NVTT_RoundMode_None,
                NvttResizeFilter.NVTT_ResizeFilter_Box);
        }

        // ---- 2b. border color --------------------------------------------------
        if (settings.Advanced.UseBorderColor)
        {
            var c = settings.Advanced.BorderColor;
            surface.SetBorder(c.r, c.g, c.b, c.a);
        }
        else if (settings.Advanced.ZeroAlphaBorder)
        {
            surface.SetBorder(0f, 0f, 0f, 0f);
        }

        // ---- 3. colour-space: convert to linear before mip filtering -----------
        if (settings.Basic.IsSRGB && settings.Advanced.GammaCorrection)
        {
            surface.ToLinearFromSrgb();
        }

        // ---- 4. premultiply alpha (before mip chain) ---------------------------
        if (settings.Advanced.PremultiplyAlpha)
        {
            surface.PremultiplyAlpha();
        }

        // ---- 5. configure compression options ----------------------------------
        compOpts.Format = SelectFormat(settings);
        compOpts.Quality = SelectQuality(settings.Advanced.CompressionLevel);

        if (settings.Advanced.CutoutAlpha)
        {
            compOpts.SetQuantization(false, false, true,
                settings.Advanced.CutoutAlphaThreshold);
        }

        // ---- 6. configure output options ---------------------------------------
        outOpts.OutputHeader = true;
        outOpts.Srgb = settings.Basic.IsSRGB;
        outOpts.Container = NvttContainer.NVTT_Container_DDS10;
        outOpts.FileName = outputPath;

        // ---- 7. mipmap count ---------------------------------------------------
        var nvttFilter = SelectMipmapFilter(settings.Advanced.MipmapFilter);

        int mipmapCount;
        if (!settings.Advanced.GenerateMipmaps)
        {
            mipmapCount = 1;
        }
        else if (settings.Advanced.MipmapLevelCount == 0)
        {
            mipmapCount = surface.CountMipmaps();
        }
        else
        {
            mipmapCount = (int)settings.Advanced.MipmapLevelCount;
        }

        // ---- 8. enable CUDA if available ---------------------------------------
        ctx.SetCudaAcceleration(NvttGlobal.IsCudaSupported);

        // ---- 9. write DDS header -----------------------------------------------
        ctx.OutputHeader(surface, mipmapCount, compOpts, outOpts);

        // ---- 10. compress mip chain using a working clone ----------------------
        using var mip = surface.Clone();

        for (var level = 0; level < mipmapCount; level++)
        {
            // Scale alpha for coverage on each mip (if requested)
            if (settings.Advanced.ScaleAlphaForMipCoverage && level > 0)
            {
                var refCoverage = mip.AlphaTestCoverage(
                    settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f);
                mip.ScaleAlphaToCoverage(refCoverage,
                    settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f);
            }

            ctx.Compress(mip, 0, level, compOpts, outOpts);

            if (level + 1 < mipmapCount)
            {
                mip.BuildNextMipmap(nvttFilter);
            }
        }
    }

    private static NvttFormat SelectFormat(TextureAssetSettings settings)
        => settings.Basic.TextureType switch
        {
            TextureType.Normal => NvttFormat.NVTT_Format_BC5,  // RG normal map
            TextureType.SingleChannel => NvttFormat.NVTT_Format_BC4,  // single channel
            TextureType.Lightmap => NvttFormat.NVTT_Format_BC6U, // HDR lightmap (unsigned)
            _ => NvttFormat.NVTT_Format_BC7,  // default colour
        };

    private static NvttQuality SelectQuality(TextureCompressionLevel level)
        => level switch
        {
            TextureCompressionLevel.Low => NvttQuality.NVTT_Quality_Fastest,
            TextureCompressionLevel.High => NvttQuality.NVTT_Quality_Production,
            _ => NvttQuality.NVTT_Quality_Normal,
        };

    private static NvttMipmapFilter SelectMipmapFilter(MipmapFilter filter)
        => filter switch
        {
            MipmapFilter.Box => NvttMipmapFilter.NVTT_MipmapFilter_Box,
            MipmapFilter.Triangle => NvttMipmapFilter.NVTT_MipmapFilter_Triangle,
            MipmapFilter.MitchellNetravali => NvttMipmapFilter.NVTT_MipmapFilter_Mitchell,
            _ => NvttMipmapFilter.NVTT_MipmapFilter_Kaiser,
        };

    private static ulong ComputeSettingsHash(TextureAssetSettings s)
    {
        var basicSize = Unsafe.SizeOf<TextureAssetSettings.BasicSettings>();
        var advancedSize = Unsafe.SizeOf<TextureAssetSettings.AdvancedSettings>();
        var samplerSize = Unsafe.SizeOf<TextureAssetSettings.SamplerSettings>();
        var total = basicSize + advancedSize + samplerSize;

        Span<byte> buf = stackalloc byte[total];
        var basic = s.Basic;
        var advanced = s.Advanced;
        var sampler = s.Sampler;
        MemoryMarshal.Write(buf, in basic);
        MemoryMarshal.Write(buf.Slice(basicSize), in advanced);
        MemoryMarshal.Write(buf.Slice(basicSize + advancedSize), in sampler);

        return XxHash64.HashToUInt64(buf);
    }
}
