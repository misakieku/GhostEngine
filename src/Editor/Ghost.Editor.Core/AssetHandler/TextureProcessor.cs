using Ghost.Nvtt;
using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.LowLevel;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
        var cacheDir = Path.Combine(cachesFolderPath, _TEXTURE_CACHE_SUBFOLDER);
        Directory.CreateDirectory(cacheDir);

        var settingsHash = ComputeSettingsHash(settings);
        var cacheFileName = $"{assetId:N}_{settingsHash:X16}.dds";
        var cachePath = Path.Combine(cacheDir, cacheFileName);

        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        foreach (var stale in Directory.EnumerateFiles(cacheDir, $"{assetId:N}_*.dds"))
        {
            File.Delete(stale);
        }

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
        using var pSurface = new DisposablePtr<NvttSurface>(NvttSurface.Create());
        using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
        using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
        using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

        var inputFormat = isFloat
            ? NvttInputFormat.NVTT_InputFormat_RGBA_32F
            : NvttInputFormat.NVTT_InputFormat_BGRA_8UB; // we'll swizzle RB below

        fixed (void* pData = pixelData)
        {
            pSurface.Get()->SetImageData(inputFormat, width, height, 1, pData, NvttBoolean.NVTT_True, null);
        }

        // stb gives us RGBA byte order; NVTT BGRA_8UB reads it as BGRA,
        // so channels R and B are swapped — fix with swizzle(2,1,0,3).
        if (!isFloat)
        {
            pSurface.Get()->Swizzle(2, 1, 0, 3, null);
        }

        var maxExtent = (int)settings.Sampler.MaxSize;
        if (settings.Advanced.StretchToPowerOfTwo)
        {
            pSurface.Get()->ResizeMakeSquare(maxExtent,
                NvttRoundMode.NVTT_RoundMode_ToNearestPowerOfTwo,
                NvttResizeFilter.NVTT_ResizeFilter_Box, null);
        }
        else if (pSurface.Get()->Width() > maxExtent || pSurface.Get()->Height() > maxExtent)
        {
            pSurface.Get()->ResizeMax(maxExtent,
                NvttRoundMode.NVTT_RoundMode_None,
                NvttResizeFilter.NVTT_ResizeFilter_Box, null);
        }

        if (settings.Advanced.UseBorderColor)
        {
            var c = settings.Advanced.BorderColor;
            pSurface.Get()->SetBorder(c.r, c.g, c.b, c.a, null);
        }
        else if (settings.Advanced.ZeroAlphaBorder)
        {
            pSurface.Get()->SetBorder(0f, 0f, 0f, 0f, null);
        }
        
        if (settings.Basic.IsSRGB && settings.Advanced.GammaCorrection)
        {
            pSurface.Get()->ToLinearFromSrgb(null);
        }

        if (settings.Advanced.PremultiplyAlpha)
        {
            pSurface.Get()->PremultiplyAlpha(null);
        }

        pCompOpts.Get()->SetFormat(SelectFormat(settings));
        pCompOpts.Get()->SetQuality(SelectQuality(settings.Advanced.CompressionLevel));

        if (settings.Advanced.CutoutAlpha)
        {
            pCompOpts.Get()->SetQuantization(false, false, true,
                settings.Advanced.CutoutAlphaThreshold);
        }

        pOutOpts.Get()->SetOutputHeader(true);
        pOutOpts.Get()->SetSrgbFlag(settings.Basic.IsSRGB);
        pOutOpts.Get()->SetContainer(NvttContainer.NVTT_Container_DDS10);
        pOutOpts.Get()->SetFileName(Encoding.UTF8.GetBytes(outputPath));

        var nvttFilter = SelectMipmapFilter(settings.Advanced.MipmapFilter);

        int mipmapCount;
        if (!settings.Advanced.GenerateMipmaps)
        {
            mipmapCount = 1;
        }
        else if (settings.Advanced.MipmapLevelCount == 0)
        {
            mipmapCount = pSurface.Get()->CountMipmaps(1);
        }
        else
        {
            mipmapCount = (int)settings.Advanced.MipmapLevelCount;
        }

        pCtx.Get()->SetCudaAcceleration(NvttApi.IsCudaSupported());

        pCtx.Get()->OutputHeader(pSurface.Get(), mipmapCount, pCompOpts.Get(), pOutOpts.Get());

        using var pMip = new DisposablePtr<NvttSurface>(pSurface.Get()->Clone());

        for (var level = 0; level < mipmapCount; level++)
        {
            // Scale alpha for coverage on each pMip (if requested)
            if (settings.Advanced.ScaleAlphaForMipCoverage && level > 0)
            {
                var refCoverage = pMip.Get()->AlphaTestCoverage(
                    settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f, 3);
                pMip.Get()->ScaleAlphaToCoverage(refCoverage,
                    settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f, 3, null);
            }

            pCtx.Get()->Compress(pMip.Get(), 0, level, pCompOpts.Get(), pOutOpts.Get());

            if (level + 1 < mipmapCount)
            {
                pMip.Get()->BuildNextMipmapDefaults(nvttFilter, 1, null);
            }
        }
    }

    private static NvttFormat SelectFormat(TextureAssetSettings settings)
        => settings.Basic.TextureType switch
        {
            TextureType.Normal => NvttFormat.NVTT_Format_BC5,  // RG normal map
            TextureType.SingleChannel => NvttFormat.NVTT_Format_BC4,  // single channel
            TextureType.Lightmap => NvttFormat.NVTT_Format_BC6U, // HDR lightmap (unsigned)
            _ => NvttFormat.NVTT_Format_BC7,  // default color
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
