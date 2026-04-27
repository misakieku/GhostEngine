using Ghost.Core;
using Ghost.Engine;
using Ghost.Nvtt;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.Editor.Core.Assets;

internal static partial class TextureProcessor
{
    private struct NvttPipelineJob : IJob
    {
        private readonly Wrapper<Result<int>> _result;

        private readonly string _outputPath;
        private readonly TextureAssetHandler.TextureInfo _textureInfo;
        private readonly TextureAssetSettings _settings;
        private UnsafeArray<MipLevel> _mipLevels;

        public NvttPipelineJob(Wrapper<Result<int>> result, string outputPath, TextureAssetHandler.TextureInfo textureInfo, TextureAssetSettings settings, UnsafeArray<MipLevel> mipLevels)
        {
            _result = result;

            _outputPath = outputPath;
            _textureInfo = textureInfo;
            _settings = settings;
            _mipLevels = mipLevels;
        }

        private unsafe Result<int> RunMipGenCompressionPipeline()
        {
            using var pSurface = new DisposablePtr<NvttSurface>(NvttSurface.Create());
            using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
            using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
            using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

            var inputFormat = _textureInfo.colorComponents == 1
                ? NvttInputFormat.NVTT_InputFormat_R_32F
                : _textureInfo.bitsPerChannel > 8
                    ? NvttInputFormat.NVTT_InputFormat_RGBA_32F
                    : NvttInputFormat.NVTT_InputFormat_BGRA_8UB; // we'll swizzle RB below

            var isNormal = _settings.Basic.TextureType == TextureType.Normal;
            if (!pSurface.Get()->SetImageData(inputFormat, _textureInfo.width, _textureInfo.height, _textureInfo.depth, (void*)_textureInfo.pixelData, isNormal, null))
            {
                return Result.Failure<int>("Failed to set image data for NVTT compression.");
            }

            if (isNormal)
            {
                pSurface.Get()->SetNormalMap(true);
            }

            // stb gives us RGBA byte order; NVTT BGRA_8UB reads it as BGRA,
            // so channels R and B are swapped — fix with swizzle(2,1,0,3).
            if (_textureInfo.colorComponents > 1 && _textureInfo.bitsPerChannel <= 8)
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

            if (_settings.Basic.IsSRGB)
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
            if (pMip.Get() == null)
            {
                return Result.Failure("Failed to clone surface for mipmap generation.");
            }

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

                using var compressMip = new DisposablePtr<NvttSurface>(pMip.Get()->Clone());
                if (_settings.Basic.IsSRGB)
                {
                    compressMip.Get()->ToSrgb(null);
                }

                if (!pCtx.Get()->Compress(compressMip.Get(), 0, level, pCompOpts.Get(), pOutOpts.Get()))
                {
                    return Result.Failure("Failed to compress mipmap.");
                }

                if (level + 1 < mipmapCount)
                {
                    if (!pMip.Get()->BuildNextMipmapDefaults(nvttFilter, 1, null))
                    {
                        return Result.Failure("Failed to build next mipmap.");
                    }
                }
            }

            return Result.Success(mipmapCount);
        }

        private unsafe Result<int> RunCubeMapCompressionPipeline()
        {
            using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
            using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
            using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

            pCompOpts.Get()->SetFormat(SelectFormat(_settings, _textureInfo.isHDR));
            pCompOpts.Get()->SetQuality(SelectQuality(_settings.Advanced.CompressionLevel));

            pOutOpts.Get()->SetOutputHeader(true);
            pOutOpts.Get()->SetSrgbFlag(_settings.Basic.IsSRGB);
            pOutOpts.Get()->SetContainer(NvttContainer.NVTT_Container_DDS10);
            pOutOpts.Get()->SetFileName(Encoding.UTF8.GetBytes(_outputPath));

            pCtx.Get()->SetCudaAcceleration(NvttApi.IsCudaSupported());

            var maxCubeMips = _mipLevels.Length;
            var w0 = _mipLevels[0].width;

            if (!pCtx.Get()->OutputHeaderData(NvttTextureType.NVTT_TextureType_Cube, w0, w0, 1, maxCubeMips, false, pCompOpts.Get(), pOutOpts.Get()))
            {
                return Result.Failure("Failed to output header for cube map.");
            }

            for (var face = 0; face < 6; face++)
            {
                for (var level = 0; level < maxCubeMips; level++)
                {
                    using var faceSurf = new DisposablePtr<NvttSurface>(NvttSurface.Create());
                    var w = _mipLevels[level].width;
                    var faceSize = w * w * _textureInfo.colorComponents;
                    var pSrcData = (float*)_mipLevels[level].data.GetUnsafePtr() + face * faceSize;

                    if (!faceSurf.Get()->SetImageData(NvttInputFormat.NVTT_InputFormat_RGBA_32F, w, w, 1, pSrcData, false, null))
                    {
                        return Result.Failure("Failed to set image data for NVTT compression.");
                    }

                    if (_settings.Basic.IsSRGB)
                    {
                        faceSurf.Get()->ToSrgb(null);
                    }

                    if (!pCtx.Get()->Compress(faceSurf.Get(), face, level, pCompOpts.Get(), pOutOpts.Get()))
                    {
                        return Result.Failure("Failed to compress cube map face.");
                    }
                }
            }

            return Result.Success(maxCubeMips);
        }

        public void Execute(ref readonly JobExecutionContext context)
        {
            try
            {
                Result<int> finalResult;

                if (_settings.Basic.TextureShape == TextureShape.TextureCube)
                {
                    finalResult = RunCubeMapCompressionPipeline();
                }
                else
                {
                    finalResult = RunMipGenCompressionPipeline();
                }

                _result.Value = finalResult;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception during NVTT compression: {ex}");
            }
        }
    }

    public static async ValueTask<Result<(string cachePath, int mipmapCount)>> GenerateMipAndCompressAsync(string cachesFolderPath, Guid assetId,
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
            var isValid = false;
            var mipMapCount = 1u;
            var hasMipMapFlag = false;

            try
            {
                using var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fs);
                if (reader.ReadUInt32() == 0x20534444)
                {
                    reader.BaseStream.Seek(4, SeekOrigin.Current);
                    var flags = reader.ReadUInt32();
                    hasMipMapFlag = (flags & 0x00020000) != 0;

                    reader.BaseStream.Seek(28, SeekOrigin.Begin);
                    mipMapCount = reader.ReadUInt32();
                    isValid = true;
                }
            }
            catch
            {
                // Ignore read errors and regenerate
            }

            if (isValid)
            {
                return (cachePath, (!hasMipMapFlag || mipMapCount == 0) ? 1 : (int)mipMapCount);
            }

            try
            {
                File.Delete(cachePath);
            }
            catch
            {
                // Ignore deletion errors, maybe file is still locked or we have no permission.
                // The pipeline will overwrite it.
            }
        }

        UnsafeArray<MipLevel> mipLevels = default;
        var scheduler = EditorApplication.GetService<EngineCore>().JobScheduler;

        try
        {
            if (settings.Basic.TextureShape == TextureShape.TextureCube)
            {
                int maxCubeMips;
                int edge;
                UnsafeArray<float> baseCubeData;
                unsafe
                {
                    using var cubeSurface0 = new DisposablePtr<NvttCubeSurface>(NvttCubeSurface.Create());
                    using var mip0Surf = new DisposablePtr<NvttSurface>(NvttSurface.Create());
                    if (!mip0Surf.Get()->SetImageData(NvttInputFormat.NVTT_InputFormat_RGBA_32F, textureInfo.width, textureInfo.height, 1, (void*)textureInfo.pixelData, false, null))
                    {
                        return Result.Failure("Failed to set image data for cube map.");
                    }

                    cubeSurface0.Get()->Fold(mip0Surf.Get(), NvttCubeLayout.NVTT_CubeLayout_LatitudeLongitude);
                    edge = cubeSurface0.Get()->EdgeLength();
                    maxCubeMips = (int)Math.Floor(Math.Log2(edge)) + 1;

                    var pixelsPerFace = edge * edge;
                    var faceSize = pixelsPerFace * textureInfo.colorComponents;
                    baseCubeData = new UnsafeArray<float>(faceSize * 6, AllocationHandle.FreeList);

                    var channels = textureInfo.colorComponents;
                    var channelPtrs = stackalloc float*[channels];
                    for (var face = 0; face < 6; face++)
                    {
                        using var faceSurf = new DisposablePtr<NvttSurface>(cubeSurface0.Get()->Face(face));

                        // NVTT stores data in planar format: [RRRR...][GGGG...][BBBB...][AAAA...]
                        // We need to interleave into RGBARGBA... for our sampling code.
                        var pDst = (float*)baseCubeData.GetUnsafePtr() + face * faceSize;

                        for (var ch = 0; ch < channels; ch++)
                        {
                            channelPtrs[ch] = faceSurf.Get()->Channel(ch);
                        }

                        for (var p = 0; p < pixelsPerFace; p++)
                        {
                            for (var ch = 0; ch < channels; ch++)
                            {
                                pDst[p * channels + ch] = channelPtrs[ch][p];
                            }
                        }
                    }
                }

                var handle = GenerateMipHDRI(scheduler, textureInfo, baseCubeData, edge, maxCubeMips, out mipLevels);
                await scheduler.WaitAsync(handle, cancellationToken);
                baseCubeData.Dispose();
            }

            var result = new Wrapper<Result<int>>();
            var nvttJob = new NvttPipelineJob(result, cachePath, textureInfo, settings, mipLevels);
            var nvttJobHandle = scheduler.Schedule(in nvttJob);
            await scheduler.WaitAsync(nvttJobHandle, cancellationToken);

            if (result.Value.IsFailure)
            {
                return Result.Failure(result.Value.Message);
            }

            return (cachePath, result.Value.Value);
        }
        finally
        {
            if (mipLevels.IsCreated)
            {
                var mipDisposeJob = new MipLevelDisposeJob
                {
                    mipLevels = mipLevels,
                };

                scheduler.Schedule(in mipDisposeJob);
            }
        }
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
