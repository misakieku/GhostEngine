using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public sealed class MaterialEncodingAndShaderTests
{
    [TestMethod]
    public void EncodeAndDecodePreservesOffsetSurfaceTypeChromaticAndVariant()
    {
        const uint maxAlignedOffset = (32u * 1024u * 1024u) - 16u;
        const uint variantIndex = 237u;

        var packedOpaque = MaterialEncoding.Encode(0u, 0u, MaterialSurfaceType.Opaque, false);
        Assert.AreEqual(0u, MaterialEncoding.GetMaterialByteOffset(packedOpaque));
        Assert.AreEqual(MaterialSurfaceType.Opaque, MaterialEncoding.GetSurfaceType(packedOpaque));
        Assert.IsFalse(MaterialEncoding.GetHasAlphaClip(packedOpaque));
        Assert.IsFalse(MaterialEncoding.GetIsTransparent(packedOpaque));
        Assert.IsFalse(MaterialEncoding.GetChromaticTransmittance(packedOpaque));
        Assert.AreEqual(0u, MaterialEncoding.GetVariantIndex(packedOpaque));

        var packedTransparent = MaterialEncoding.Encode(maxAlignedOffset, variantIndex, MaterialSurfaceType.Transparent, true);
        Assert.AreEqual(maxAlignedOffset, MaterialEncoding.GetMaterialByteOffset(packedTransparent));
        Assert.AreEqual(maxAlignedOffset, MaterialEncoding.GetMaterialBufferIndex(packedTransparent));
        Assert.AreEqual(MaterialSurfaceType.Transparent, MaterialEncoding.GetSurfaceType(packedTransparent));
        Assert.IsFalse(MaterialEncoding.GetHasAlphaClip(packedTransparent));
        Assert.IsTrue(MaterialEncoding.GetIsTransparent(packedTransparent));
        Assert.IsTrue(MaterialEncoding.GetChromaticTransmittance(packedTransparent));
        Assert.AreEqual(variantIndex, MaterialEncoding.GetVariantIndex(packedTransparent));

        var packedLegacyAlphaClip = MaterialEncoding.Encode(4096u, 15u, hasAlphaClip: true);
        Assert.AreEqual(4096u, MaterialEncoding.GetMaterialByteOffset(packedLegacyAlphaClip));
        Assert.AreEqual(MaterialSurfaceType.AlphaClip, MaterialEncoding.GetSurfaceType(packedLegacyAlphaClip));
        Assert.IsTrue(MaterialEncoding.GetHasAlphaClip(packedLegacyAlphaClip));
        Assert.IsFalse(MaterialEncoding.GetIsTransparent(packedLegacyAlphaClip));
        Assert.IsFalse(MaterialEncoding.GetChromaticTransmittance(packedLegacyAlphaClip));
        Assert.AreEqual(15u, MaterialEncoding.GetVariantIndex(packedLegacyAlphaClip));
    }

    [TestMethod]
    public void PassSemanticFromNameAndShaderSemanticMapSupportTransparentPasses()
    {
        Assert.AreEqual(PassSemantic.TransparentPrepass, PassSemanticExtensions.FromName("TransparentPrepass"));
        Assert.AreEqual(PassSemantic.TransparentForward, PassSemanticExtensions.FromName("TransparentForward"));
        Assert.AreEqual(PassSemantic.Custom, PassSemanticExtensions.FromName("CustomPass"));

        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);

        var descriptor = new GraphicsShaderDescriptor
        {
            Name = "Test/TransparentLit",
            PropertyBufferSize = 64,
            VariantIndex = 3,
            Passes = new PassDescriptor[]
            {
                new PassDescriptor
                {
                    name = "Visibility",
                    semantic = PassSemantic.Visibility,
                    stageMask = ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                    localPipeline = PipelineState.Default,
                },
                new PassDescriptor
                {
                    name = "TransparentPrepass",
                    semantic = PassSemantic.TransparentPrepass,
                    stageMask = ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                    localPipeline = PipelineState.Default,
                },
                new PassDescriptor
                {
                    name = "TransparentForward",
                    semantic = PassSemantic.TransparentForward,
                    stageMask = ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                    localPipeline = new PipelineState
                    {
                        ZTest = ZTest.GreaterEqual,
                        ZWrite = ZWrite.Off,
                        Cull = Cull.Back,
                        Blend = Blend.PureAdditive,
                        ColorMask = ColorWriteMask.All,
                    },
                },
            },
        };

        var shaderHandle = resourceManager.CreateShader(descriptor);
        Assert.IsTrue(shaderHandle.IsValid);

        ref var shader = ref resourceManager.GetShaderReference(shaderHandle).Value;
        Assert.AreEqual(0, shader.GetPassIndex(PassSemantic.Visibility));
        Assert.AreEqual(1, shader.GetPassIndex(PassSemantic.TransparentPrepass));
        Assert.AreEqual(2, shader.GetPassIndex(PassSemantic.TransparentForward));
        Assert.AreEqual(Blend.PureAdditive, shader.GetPassReference(2).DefaultState.Blend);
    }

    [TestMethod]
    public void ResourceManagerTracksSurfaceTypeAndChromaticFromPropertyBufferAndOverrides()
    {
        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);

        var shaderHandle = resourceManager.CreateShader(new GraphicsShaderDescriptor
        {
            Name = "Test/TemplateLayout",
            PropertyBufferSize = 48,
            VariantIndex = 2,
            Passes = Array.Empty<PassDescriptor>(),
        });

        var matHandle = resourceManager.CreateMaterial(shaderHandle, "GlassMat");
        Assert.IsTrue(matHandle.IsValid);

        var props = new byte[48];
        // surfaceType = 2 (Transparent) at offset 28, chromaticTransmittance = 1 at offset 32.
        MemoryMarshal.Write(props.AsSpan(28, 4), (uint)MaterialSurfaceType.Transparent);
        MemoryMarshal.Write(props.AsSpan(32, 4), 1u);

        Assert.AreEqual(Error.None, resourceManager.SetRawMaterialProperty(matHandle, props));

        ref var mat = ref resourceManager.GetMaterialReference(matHandle).Value;
        Assert.AreEqual(MaterialSurfaceType.Transparent, mat.SurfaceType);
        Assert.IsTrue(mat.IsTransparent);
        Assert.IsFalse(mat.HasAlphaClip);
        Assert.IsTrue(mat.ChromaticTransmittance);

        Assert.AreEqual(Error.None, resourceManager.SetMaterialSurfaceType(matHandle, MaterialSurfaceType.AlphaClip));
        Assert.AreEqual(Error.None, resourceManager.SetMaterialChromaticTransmittance(matHandle, false));
        Assert.AreEqual(MaterialSurfaceType.AlphaClip, mat.SurfaceType);
        Assert.IsTrue(mat.HasAlphaClip);
        Assert.IsFalse(mat.ChromaticTransmittance);
    }
}
