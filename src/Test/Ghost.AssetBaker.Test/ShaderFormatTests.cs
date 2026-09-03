using Ghost.Core;
using Ghost.Core.Graphics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Test;

[TestClass]
public class ShaderFormatTests
{
    [TestMethod]
    public void ShaderContentHeader_RoundTripsMetadata()
    {
        var header = new ShaderContentHeader
        {
            shaderType = ShaderType.Graphics,
            passCount = 4,
            propertyBufferSize = 128,
            shaderModel = ShaderModel.SM_6_7,
            shaderId = 0x123456789ABCDEF0ul,
            familyId = 0x1122334455667788ul,
            layoutHash = 0x0FEDCBA987654321ul,
            nameOffset = 64,
            nameSize = 18,
        };

        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ShaderContentHeader>(in header)).ToArray();
        var decoded = MemoryMarshal.Read<ShaderContentHeader>(bytes);

        Assert.AreEqual(ShaderContentHeader.MAGIC, decoded.magic);
        Assert.AreEqual(ShaderContentHeader.VERSION, decoded.version);
        Assert.AreEqual(header.shaderType, decoded.shaderType);
        Assert.AreEqual(header.passCount, decoded.passCount);
        Assert.AreEqual(header.propertyBufferSize, decoded.propertyBufferSize);
        Assert.AreEqual(header.shaderModel, decoded.shaderModel);
        Assert.AreEqual(header.shaderId, decoded.shaderId);
        Assert.AreEqual(header.familyId, decoded.familyId);
        Assert.AreEqual(header.layoutHash, decoded.layoutHash);
        Assert.AreEqual(header.nameOffset, decoded.nameOffset);
        Assert.AreEqual(header.nameSize, decoded.nameSize);
    }

    [TestMethod]
    public void ShaderPassHeader_RoundTripsSemanticStagesAndPipeline()
    {
        var pass = new ShaderContentHeader.PassHeader
        {
            entryPointCount = 3,
            semantic = PassSemantic.Visibility,
            stageMask = ShaderStageMask.Amplification | ShaderStageMask.Mesh | ShaderStageMask.Pixel,
            passId = ShaderIdentity.GetPassId(0x123456789ABCDEF0ul, 1),
            localPipeline = new PipelineState
            {
                ZTest = ZTest.LessEqual,
                ZWrite = ZWrite.On,
                Cull = Cull.Back,
                Blend = Blend.Opaque,
                ColorMask = ColorWriteMask.None,
            },
            nameOffset = 100,
            nameSize = 10,
            dataOffset = 120,
            dataSize = 300,
        };

        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ShaderContentHeader.PassHeader>(in pass)).ToArray();
        var decoded = MemoryMarshal.Read<ShaderContentHeader.PassHeader>(bytes);

        Assert.AreEqual(pass.entryPointCount, decoded.entryPointCount);
        Assert.AreEqual(pass.semantic, decoded.semantic);
        Assert.AreEqual(pass.stageMask, decoded.stageMask);
        Assert.AreEqual(pass.passId, decoded.passId);
        Assert.AreEqual(pass.localPipeline.GetHashCode64(), decoded.localPipeline.GetHashCode64());
        Assert.AreEqual(pass.nameOffset, decoded.nameOffset);
        Assert.AreEqual(pass.nameSize, decoded.nameSize);
        Assert.AreEqual(pass.dataOffset, decoded.dataOffset);
        Assert.AreEqual(pass.dataSize, decoded.dataSize);
    }

    [TestMethod]
    public void ShaderIdentity_IsDeterministicAndPreservesPassIndex()
    {
        var shaderId = ShaderIdentity.GetShaderId("Custom/CarPaint");

        Assert.AreEqual(shaderId, ShaderIdentity.GetShaderId("Custom/CarPaint"));
        Assert.AreEqual(shaderId, ShaderIdentity.GetPassId(shaderId, 0) & ShaderIdentity.ShaderIdMask);
        Assert.AreEqual(7ul, ShaderIdentity.GetPassId(shaderId, 7) & 0xFul);
        Assert.AreEqual(64, Unsafe.SizeOf<ShaderContentHeader>());
    }

    [TestMethod]
    public async Task ShaderCatalog_RoundTripsThroughManifest()
    {
        var assetId = Guid.NewGuid();
        var manifest = new Manifest();
        manifest.Shaders.Add(new ShaderCatalogEntry
        {
            AssetId = assetId,
            ShaderType = ShaderType.Graphics,
            Name = "StandardLit",
            ShaderId = ShaderIdentity.GetShaderId("StandardLit"),
            FamilyId = ShaderIdentity.GetShaderId("Lit"),
            LayoutHash = 0x1020304050607080ul,
            PropertyBufferSize = 96,
            ShaderModel = ShaderModel.SM_6_8,
            Passes = new ShaderCatalogPass[]
            {
                new ShaderCatalogPass
                {
                    Name = "DeferredTexturing",
                    Semantic = PassSemantic.DeferredTexturing,
                    StageMask = ShaderStageMask.Compute,
                    PassId = ShaderIdentity.GetPassId(ShaderIdentity.GetShaderId("StandardLit"), 0),
                    LocalPipeline = PipelineState.Default,
                },
            },
        });

        var path = Path.Combine(Path.GetTempPath(), $"ghost-shader-catalog-{Guid.NewGuid():N}.json");
        try
        {
            await manifest.SaveToDiskAsync(path);
            var decoded = await Manifest.LoadFromDiskAsync(path);

            Assert.HasCount(1, decoded.Shaders);
            var shader = decoded.Shaders[0];
            Assert.AreEqual(assetId, shader.AssetId);
            Assert.AreEqual("StandardLit", shader.Name);
            Assert.AreEqual(ShaderIdentity.GetShaderId("Lit"), shader.FamilyId);
            Assert.AreEqual(96u, shader.PropertyBufferSize);
            Assert.HasCount(1, shader.Passes);
            Assert.AreEqual(PassSemantic.DeferredTexturing, shader.Passes[0].Semantic);
            Assert.AreEqual(ShaderStageMask.Compute, shader.Passes[0].StageMask);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
