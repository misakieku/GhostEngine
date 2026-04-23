using Ghost.Core;
using Ghost.Nvtt;
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
internal static class TextureProcessor
{
    private class NvttPipelineTask : IThreadPoolWorkItem
    {
        private readonly string _outputPath;

        private readonly TextureAssetHandler.TextureInfo _textureInfo;

        private readonly TextureAssetSettings _settings;
        private readonly TaskCompletionSource<Result<int>> _completionSource;

        public Task<Result<int>> Task => _completionSource.Task;

        public NvttPipelineTask(string outputPath, TextureAssetHandler.TextureInfo textureInfo, TextureAssetSettings settings)
        {
            _outputPath = outputPath;
            _textureInfo = textureInfo;
            _settings = settings;
            _completionSource = new TaskCompletionSource<Result<int>>();
        }

        public unsafe void Execute()
        {
            using var pSurface = new DisposablePtr<NvttSurface>(NvttSurface.Create());
            using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
            using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
            using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

            var inputFormat = _textureInfo.colorComponents == 1
                ? NvttInputFormat.NVTT_InputFormat_R_32F
                : _textureInfo.depth > 8
                    ? NvttInputFormat.NVTT_InputFormat_RGBA_32F
                    : NvttInputFormat.NVTT_InputFormat_BGRA_8UB; // we'll swizzle RB below

            var needUnsigned = _settings.Basic.TextureType == TextureType.Normal ? NvttBoolean.NVTT_True : NvttBoolean.NVTT_False;
            if (pSurface.Get()->SetImageData(inputFormat, _textureInfo.width, _textureInfo.height, _textureInfo.depth, (void*)_textureInfo.pixelData, needUnsigned, null))
            {
                _completionSource.SetResult(Result.Failure("Failed to set image data for NVTT compression."));
                return;
            }

            // stb gives us RGBA byte order; NVTT BGRA_8UB reads it as BGRA,
            // so channels R and B are swapped — fix with swizzle(2,1,0,3).
            if (_textureInfo.colorComponents > 1 && _textureInfo.depth <= 8)
            {
                pSurface.Get()->Swizzle(2, 1, 0, 3, null);
            }

            var maxExtent = (int)_settings.Sampler.MaxSize;
            if (_settings.Advanced.StretchToPowerOfTwo)
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

            if (_settings.Advanced.UseBorderColor)
            {
                var c = _settings.Advanced.BorderColor;
                pSurface.Get()->SetBorder(c.r, c.g, c.b, c.a, null);
            }
            else if (_settings.Advanced.ZeroAlphaBorder)
            {
                pSurface.Get()->SetBorder(0f, 0f, 0f, 0f, null);
            }

            if (_settings.Basic.IsSRGB && _settings.Advanced.GammaCorrection)
            {
                pSurface.Get()->ToLinearFromSrgb(null);
            }

            if (_settings.Advanced.PremultiplyAlpha)
            {
                pSurface.Get()->PremultiplyAlpha(null);
            }

            pCompOpts.Get()->SetFormat(SelectFormat(_settings, _textureInfo.isHDR));
            pCompOpts.Get()->SetQuality(SelectQuality(_settings.Advanced.CompressionLevel));

            if (_settings.Advanced.CutoutAlpha)
            {
                pCompOpts.Get()->SetQuantization(false, false, true,
                    _settings.Advanced.CutoutAlphaThreshold);
            }

            pOutOpts.Get()->SetOutputHeader(true);
            pOutOpts.Get()->SetSrgbFlag(_settings.Basic.IsSRGB);
            pOutOpts.Get()->SetContainer(NvttContainer.NVTT_Container_DDS10);
            pOutOpts.Get()->SetFileName(Encoding.UTF8.GetBytes(_outputPath));

            var nvttFilter = SelectMipmapFilter(_settings.Advanced.MipmapFilter);

            int mipmapCount;
            if (!_settings.Advanced.GenerateMipmaps)
            {
                mipmapCount = 1;
            }
            else if (_settings.Advanced.MipmapLevelCount == 0)
            {
                mipmapCount = pSurface.Get()->CountMipmaps(1);
            }
            else
            {
                mipmapCount = (int)_settings.Advanced.MipmapLevelCount;
            }

            pCtx.Get()->SetCudaAcceleration(NvttApi.IsCudaSupported());

            pCtx.Get()->OutputHeader(pSurface.Get(), mipmapCount, pCompOpts.Get(), pOutOpts.Get());

            using var pMip = new DisposablePtr<NvttSurface>(pSurface.Get()->Clone());

            for (var level = 0; level < mipmapCount; level++)
            {
                // Scale alpha for coverage on each pMip (if requested)
                if (_settings.Advanced.ScaleAlphaForMipCoverage && level > 0)
                {
                    var refCoverage = pMip.Get()->AlphaTestCoverage(
                        _settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f, 3);
                    pMip.Get()->ScaleAlphaToCoverage(refCoverage,
                        _settings.Advanced.ScaleAlphaForMipCoverageThreshold / 255f, 3, null);
                }

                pCtx.Get()->Compress(pMip.Get(), 0, level, pCompOpts.Get(), pOutOpts.Get());

                if (level + 1 < mipmapCount)
                {
                    pMip.Get()->BuildNextMipmapDefaults(nvttFilter, 1, null);
                }
            }

            _completionSource.SetResult(Result.Success(mipmapCount));
        }
    }

    public static async ValueTask<Result<(string cachePath, int mipmapCount)>> CompressToCacheAsync(string cachesFolderPath, Guid assetId,
        TextureAssetHandler.TextureInfo textureInfo,
        TextureAssetSettings settings, CancellationToken cancellationToken)
    {
        var settingsHash = ComputeSettingsHash(settings);
        var cacheFileName = $"texturecache_{assetId:N}_{settingsHash:X16}.dds";

        var textureCachePath = Path.Combine(cachesFolderPath, "TextureCache");
        var cachePath = Path.Combine(textureCachePath, cacheFileName);

        Directory.CreateDirectory(textureCachePath);

        if (File.Exists(cachePath))
        {
            using var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            if (reader.ReadUInt32() != 0x20534444)
            {
                File.Delete(cachePath);
                goto ScheduleWork;
            }

            // Read dwFlags (Offset 8)
            // Skip dwSize (4 bytes), then read dwFlags (4 bytes)
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            var flags = reader.ReadUInt32();

            // The DDSD_MIPMAPCOUNT flag is 0x00020000
            var hasMipMapFlag = (flags & 0x00020000) != 0;

            // Read dwMipMapCount (Offset 28)
            reader.BaseStream.Seek(28, SeekOrigin.Begin);
            var mipMapCount = reader.ReadUInt32();

            // Return the correct count
            // If the flag is missing, or the count says 0, there is still 1 main image.
            if (!hasMipMapFlag || mipMapCount == 0)
            {
                return (cachePath, 1);
            }

            return (cachePath, (int)mipMapCount);
        }

    ScheduleWork:
        var workItem = new NvttPipelineTask(cachePath, textureInfo, settings);
        ThreadPool.UnsafeQueueUserWorkItem(workItem, true);
        var result = await workItem.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Message);
        }

        return (cachePath, result.Value);
    }

    private static NvttFormat SelectFormat(TextureAssetSettings settings, bool isHDR)
        => isHDR
            ? NvttFormat.NVTT_Format_BC6U
            : settings.Basic.TextureType switch
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

    private static ulong ComputeSettingsHash(TextureAssetSettings settings)
    {
        var basicSize = Unsafe.SizeOf<TextureAssetSettings.BasicSettings>();
        var advancedSize = Unsafe.SizeOf<TextureAssetSettings.AdvancedSettings>();
        var samplerSize = Unsafe.SizeOf<TextureAssetSettings.SamplerSettings>();
        var total = basicSize + advancedSize + samplerSize;

        Span<byte> buf = stackalloc byte[total];
        var basic = settings.Basic;
        var advanced = settings.Advanced;
        var sampler = settings.Sampler;
        MemoryMarshal.Write(buf, in basic);
        MemoryMarshal.Write(buf.Slice(basicSize), in advanced);
        MemoryMarshal.Write(buf.Slice(basicSize + advancedSize), in sampler);

        return XxHash64.HashToUInt64(buf);
    }
}
