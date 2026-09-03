using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Streaming;

[TestClass]
[DoNotParallelize]
public sealed class ShaderVariantRegistryTest
{
    [TestMethod]
    public void CatalogRegistrationCreatesDenseSemanticRostersAndStableHandles()
    {
        var firstAsset = Guid.NewGuid();
        var secondAsset = Guid.NewGuid();
        var familyId = ShaderIdentity.GetShaderId("Lit");
        var catalog = new ShaderCatalogEntry[]
        {
            CreateEntry(firstAsset, "StandardLit", familyId, PassSemantic.Forward, PassSemantic.DeferredTexturing),
            CreateEntry(secondAsset, "MyLit", familyId, PassSemantic.Forward, PassSemantic.Shadow),
        };

        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var registry = new ShaderVariantRegistry(resourceManager, catalog);

        Assert.AreEqual(2, registry.Count);
        Assert.IsTrue(registry.TryGetVariantIndex(firstAsset, out var firstIndex));
        Assert.IsTrue(registry.TryGetVariantIndex(catalog[1].ShaderId, out var secondIndex));
        Assert.AreEqual(0, firstIndex.Value);
        Assert.AreEqual(1, secondIndex.Value);

        var forwardVariants = registry.GetVariants(PassSemantic.Forward);
        Assert.AreEqual(2, forwardVariants.Length);
        Assert.AreEqual(firstIndex, forwardVariants[0]);
        Assert.AreEqual(secondIndex, forwardVariants[1]);

        var deferredVariants = registry.GetVariants(PassSemantic.DeferredTexturing);
        Assert.AreEqual(1, deferredVariants.Length);
        Assert.AreEqual(firstIndex, deferredVariants[0]);

        ref readonly var firstVariant = ref registry.GetVariant(firstIndex);
        ref readonly var secondVariant = ref registry.GetVariant(secondIndex);
        Assert.AreEqual(familyId, firstVariant.FamilyId);
        Assert.AreEqual(familyId, secondVariant.FamilyId);
        Assert.AreEqual(ShaderVariantState.MetadataReady, firstVariant.State);
        Assert.IsTrue(firstVariant.Shader.IsValid);

        ref readonly var shader = ref resourceManager.GetShaderReference(firstVariant.Shader).Value;
        Assert.AreEqual(0, shader.GetPassIndex(PassSemantic.Forward));
        Assert.AreEqual(1, shader.GetPassIndex(PassSemantic.DeferredTexturing));
        Assert.AreEqual(-1, shader.GetPassIndex(PassSemantic.Shadow));
        Assert.AreEqual(ShaderStageMask.Compute, shader.GetPassReference(1).StageMask);
        Assert.IsTrue(shader.TryGetPass(Ghost.Graphics.Core.Shader.GetPassID("Forward"), out var passIndex).IsSuccess);
        Assert.AreEqual(0, passIndex);
    }

    private static ShaderCatalogEntry CreateEntry(Guid assetId, string name, ulong familyId, params PassSemantic[] semantics)
    {
        var shaderId = ShaderIdentity.GetShaderId(name);
        var passes = new ShaderCatalogPass[semantics.Length];
        for (var i = 0; i < semantics.Length; i++)
        {
            passes[i] = new ShaderCatalogPass
            {
                Name = semantics[i].ToString(),
                Semantic = semantics[i],
                StageMask = semantics[i] == PassSemantic.DeferredTexturing ? ShaderStageMask.Compute : ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                PassId = ShaderIdentity.GetPassId(shaderId, i),
                LocalPipeline = PipelineState.Default,
            };
        }

        return new ShaderCatalogEntry
        {
            AssetId = assetId,
            ShaderType = ShaderType.Graphics,
            Name = name,
            ShaderId = shaderId,
            FamilyId = familyId,
            LayoutHash = 42,
            PropertyBufferSize = 64,
            ShaderModel = ShaderModel.SM_6_8,
            Passes = passes,
        };
    }
}
