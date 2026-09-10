using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// Holds the transient RenderGraph textures and identifiers for a single frame of modern GPU-driven rendering.
/// </summary>
internal struct RenderFrameResources
{
    /// <summary>
    /// Visibility Buffer (64 bpp: R32G32_UInt, packing instanceID/meshletID + primitiveID).
    /// </summary>
    public Identifier<RGTexture> VisibilityBuffer;

    /// <summary>
    /// Scene Depth Buffer (D32_Float, reversed-Z where near=1.0, far=0.0).
    /// </summary>
    public Identifier<RGTexture> DepthBuffer;

    /// <summary>
    /// Maximum number of HZB mip levels supported.
    /// </summary>
    public const int MAX_HZB_MIPS = 16;

    /// <summary>
    /// Hierarchical Z-Buffer mip textures for two-pass occlusion culling.
    /// </summary>
    public Identifier<RGTexture>[] HZBMips;

    /// <summary>
    /// Number of valid mips in the current HZB pyramid.
    /// </summary>
    public uint HZBMipCount;

    /// <summary>
    /// Width of HZB Mip 0 (half render resolution).
    /// </summary>
    public uint HZBWidth;

    /// <summary>
    /// Height of HZB Mip 0 (half render resolution).
    /// </summary>
    public uint HZBHeight;

    /// <summary>
    /// GBuffer0: R8G8B8A8_UNorm (Albedo.rgb, Perceptual Roughness).
    /// </summary>
    public Identifier<RGTexture> GBuffer0;

    /// <summary>
    /// GBuffer1: R10G10B10A2_UNorm (World Normal octahedral packed + flags).
    /// </summary>
    public Identifier<RGTexture> GBuffer1;

    /// <summary>
    /// GBuffer2: R8G8_UNorm (Metallic, Ambient Occlusion).
    /// </summary>
    public Identifier<RGTexture> GBuffer2;

    /// <summary>
    /// GBuffer3: R16G16_Float (Screen-space motion vectors).
    /// </summary>
    public Identifier<RGTexture> GBuffer3;

    /// <summary>
    /// HDR scene color buffer (R16G16B16A16_Float).
    /// </summary>
    public Identifier<RGTexture> HDRColorBuffer;

    /// <summary>
    /// Render resolution width.
    /// </summary>
    public uint Width;

    /// <summary>
    /// Render resolution height.
    /// </summary>
    public uint Height;

    /// <summary>
    /// Allocates standard frame resources within the RenderGraph builder.
    /// </summary>
    public static RenderFrameResources Create(IRenderGraphBuilder builder, uint width, uint height, bool useReversedZ = true, Identifier<RGTexture> externalDepth = default)
    {
        var depthClear = useReversedZ ? 0.0f : 1.0f;

        var visBufferDesc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R32G32_UInt,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var depthDesc = RGTextureDesc.RelativeDepth(
            1.0f,
            clearDepth: depthClear,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            format: TextureFormat.D32_Float,
            usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource);

        var gbuffer0Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R8G8B8A8_UNorm,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var gbuffer1Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R10G10B10A2_UNorm,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var gbuffer2Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R8G8_UNorm,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var gbuffer3Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16_Float,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var hdrColorDesc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16B16A16_Float,
            clearColor: default,
            clearAtFirstUse: true,
            discardAtLastUse: true,
            usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

        var hzbWidth = Math.Max(1u, width / 2);
        var hzbHeight = Math.Max(1u, height / 2);
        var maxDim = Math.Max(hzbWidth, hzbHeight);
        var mipCount = (uint)Math.Floor(Math.Log2(maxDim)) + 1;
        mipCount = Math.Min(mipCount, (uint)MAX_HZB_MIPS);

        var hzbMips = new Identifier<RGTexture>[mipCount];
        var curW = hzbWidth;
        var curH = hzbHeight;
        for (uint i = 0; i < mipCount; ++i)
        {
            var mipDesc = RGTextureDesc.Absolute(
                curW,
                curH,
                TextureFormat.R32_Float,
                clearColor: default,
                clearAtFirstUse: false,
                discardAtLastUse: false,
                usage: TextureUsage.ShaderResource | TextureUsage.UnorderedAccess);

            hzbMips[i] = builder.CreateTexture(in mipDesc, $"HZBMip_{i}");
            curW = Math.Max(1u, curW / 2);
            curH = Math.Max(1u, curH / 2);
        }

        return new RenderFrameResources
        {
            VisibilityBuffer = builder.CreateTexture(in visBufferDesc, "VisibilityBuffer"),
            DepthBuffer = externalDepth.IsValid ? externalDepth : builder.CreateTexture(in depthDesc, "SceneDepthBuffer"),
            HZBMips = hzbMips,
            HZBMipCount = mipCount,
            HZBWidth = hzbWidth,
            HZBHeight = hzbHeight,
            GBuffer0 = builder.CreateTexture(in gbuffer0Desc, "GBuffer0"),
            GBuffer1 = builder.CreateTexture(in gbuffer1Desc, "GBuffer1"),
            GBuffer2 = builder.CreateTexture(in gbuffer2Desc, "GBuffer2"),
            GBuffer3 = builder.CreateTexture(in gbuffer3Desc, "GBuffer3"),
            HDRColorBuffer = builder.CreateTexture(in hdrColorDesc, "HDRColorBuffer"),
            Width = width,
            Height = height,
        };
    }
}
