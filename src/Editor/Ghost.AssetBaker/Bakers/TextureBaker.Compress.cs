using Ghost.Nvtt;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Text;

namespace Ghost.AssetBaker.Bakers;

internal partial class TextureBaker
{
    private static async Task<(string tempFilePath, int mipmapCount)> GenerateMipAndCompressAsync(
        TextureInfo textureInfo,
        TextureBakeSettings settings)
    {
        var tempFilePath = Path.GetTempFileName();

        UnsafeArray<MipLevel> mipLevels = default;
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
                        throw new Exception("Failed to set image data for cube map.");
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

                mipLevels = await Task.Run(() => GenerateMipHDRI(textureInfo, baseCubeData, edge, maxCubeMips)).ConfigureAwait(false);
                baseCubeData.Dispose();
            }

            var mipmapCount = await Task.Run(() =>
            {
                if (settings.Basic.TextureShape == TextureShape.TextureCube)
                {
                    return RunCubeMapCompressionPipeline(tempFilePath, textureInfo, settings, mipLevels);
                }
                else
                {
                    return RunMipGenCompressionPipeline(tempFilePath, textureInfo, settings);
                }
            }).ConfigureAwait(false);

            return (tempFilePath, mipmapCount);
        }
        finally
        {
            if (mipLevels.IsCreated)
            {
                for (var i = 0; i < mipLevels.Length; i++)
                {
                    mipLevels[i].data.Dispose();
                }
                mipLevels.Dispose();
            }
        }
    }

    private static unsafe int RunMipGenCompressionPipeline(string outputPath, TextureInfo textureInfo, TextureBakeSettings settings)
    {
        using var pSurface = new DisposablePtr<NvttSurface>(NvttSurface.Create());
        using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
        using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
        using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

        var inputFormat = textureInfo.colorComponents == 1
            ? NvttInputFormat.NVTT_InputFormat_R_32F
            : textureInfo.bitsPerChannel > 8
                ? NvttInputFormat.NVTT_InputFormat_RGBA_32F
                : NvttInputFormat.NVTT_InputFormat_BGRA_8UB; // we'll swizzle RB below

        var isNormal = settings.Basic.TextureType == TextureType.Normal;
        if (!pSurface.Get()->SetImageData(inputFormat, textureInfo.width, textureInfo.height, textureInfo.depth, (void*)textureInfo.pixelData, isNormal, null))
        {
            throw new Exception("Failed to set image data for NVTT compression.");
        }

        if (isNormal)
        {
            pSurface.Get()->SetNormalMap(true);
        }

        // stb gives us RGBA byte order; NVTT BGRA_8UB reads it as BGRA,
        // so channels R and B are swapped — fix with swizzle(2,1,0,3).
        if (textureInfo.colorComponents > 1 && textureInfo.bitsPerChannel <= 8)
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
            pSurface.Get()->SetBorder(c.X, c.Y, c.Z, c.W, null);
        }
        else if (settings.Advanced.ZeroAlphaBorder)
        {
            pSurface.Get()->SetBorder(0f, 0f, 0f, 0f, null);
        }

        if (settings.Basic.IsSRGB)
        {
            pSurface.Get()->ToLinearFromSrgb(null);
        }

        if (settings.Advanced.PremultiplyAlpha)
        {
            pSurface.Get()->PremultiplyAlpha(null);
        }

        pCompOpts.Get()->SetFormat(SelectFormat(settings, textureInfo.isHDR));
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
        if (pMip.Get() == null)
        {
            throw new Exception("Failed to clone surface for mipmap generation.");
        }

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

            using var compressMip = new DisposablePtr<NvttSurface>(pMip.Get()->Clone());
            if (settings.Basic.IsSRGB)
            {
                compressMip.Get()->ToSrgb(null);
            }

            if (!pCtx.Get()->Compress(compressMip.Get(), 0, level, pCompOpts.Get(), pOutOpts.Get()))
            {
                throw new Exception("Failed to compress mipmap.");
            }

            if (level + 1 < mipmapCount)
            {
                if (!pMip.Get()->BuildNextMipmapDefaults(nvttFilter, 1, null))
                {
                    throw new Exception("Failed to build next mipmap.");
                }
            }
        }

        return mipmapCount;
    }

    private static unsafe int RunCubeMapCompressionPipeline(string outputPath, TextureInfo textureInfo, TextureBakeSettings settings, UnsafeArray<MipLevel> mipLevels)
    {
        using var pCompOpts = new DisposablePtr<NvttCompressionOptions>(NvttCompressionOptions.Create());
        using var pOutOpts = new DisposablePtr<NvttOutputOptions>(NvttOutputOptions.Create());
        using var pCtx = new DisposablePtr<NvttContext>(NvttContext.Create());

        pCompOpts.Get()->SetFormat(SelectFormat(settings, textureInfo.isHDR));
        pCompOpts.Get()->SetQuality(SelectQuality(settings.Advanced.CompressionLevel));

        pOutOpts.Get()->SetOutputHeader(true);
        pOutOpts.Get()->SetSrgbFlag(settings.Basic.IsSRGB);
        pOutOpts.Get()->SetContainer(NvttContainer.NVTT_Container_DDS10);
        pOutOpts.Get()->SetFileName(Encoding.UTF8.GetBytes(outputPath));

        pCtx.Get()->SetCudaAcceleration(NvttApi.IsCudaSupported());

        var maxCubeMips = mipLevels.Length;
        var w0 = mipLevels[0].width;

        if (!pCtx.Get()->OutputHeaderData(NvttTextureType.NVTT_TextureType_Cube, w0, w0, 1, maxCubeMips, false, pCompOpts.Get(), pOutOpts.Get()))
        {
            throw new Exception("Failed to output header for cube map.");
        }

        for (var face = 0; face < 6; face++)
        {
            for (var level = 0; level < maxCubeMips; level++)
            {
                using var faceSurf = new DisposablePtr<NvttSurface>(NvttSurface.Create());
                var w = mipLevels[level].width;
                var faceSize = w * w * textureInfo.colorComponents;
                var pSrcData = (float*)mipLevels[level].data.GetUnsafePtr() + face * faceSize;

                if (!faceSurf.Get()->SetImageData(NvttInputFormat.NVTT_InputFormat_RGBA_32F, w, w, 1, pSrcData, false, null))
                {
                    throw new Exception("Failed to set image data for NVTT compression.");
                }

                if (settings.Basic.IsSRGB)
                {
                    faceSurf.Get()->ToSrgb(null);
                }

                if (!pCtx.Get()->Compress(faceSurf.Get(), face, level, pCompOpts.Get(), pOutOpts.Get()))
                {
                    throw new Exception("Failed to compress cube map face.");
                }
            }
        }

        return maxCubeMips;
    }

    private static NvttFormat SelectFormat(TextureBakeSettings settings, bool isHDR)
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
}
