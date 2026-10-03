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

        var dispatchVariants = registry.GetDispatchVariants(PassSemantic.DeferredTexturing);
        Assert.AreEqual(1, dispatchVariants.Length);
        Assert.AreEqual(firstIndex.Value, dispatchVariants[0].DenseIndex);
        Assert.AreEqual(registry.GetVariant(firstIndex).Shader, dispatchVariants[0].Shader);
        Assert.IsFalse(registry.IsBytecodeReady(firstIndex.Value));
        registry.PublishBytecodeReady(firstAsset);
        Assert.IsTrue(registry.IsBytecodeReady(firstIndex.Value));
        Assert.IsFalse(registry.IsBytecodeReady(secondIndex.Value));

        ref readonly var firstVariant = ref registry.GetVariant(firstIndex);
        ref readonly var secondVariant = ref registry.GetVariant(secondIndex);
        Assert.AreEqual(familyId, firstVariant.FamilyId);
        Assert.AreEqual(familyId, secondVariant.FamilyId);
        Assert.AreEqual(ShaderVariantState.BytecodeReady, registry.GetState(firstIndex));
        Assert.IsTrue(firstVariant.Shader.IsValid);

        ref readonly var shader = ref resourceManager.GetShaderReference(firstVariant.Shader).Value;
        Assert.AreEqual(0, shader.GetPassIndex(PassSemantic.Forward));
        Assert.AreEqual(1, shader.GetPassIndex(PassSemantic.DeferredTexturing));
        Assert.AreEqual(-1, shader.GetPassIndex(PassSemantic.Shadow));
        Assert.AreEqual(ShaderStageMask.Compute, shader.GetPassReference(1).StageMask);
        Assert.IsTrue(shader.TryGetPass(Ghost.Graphics.Core.Shader.GetPassID("Forward"), out var passIndex).IsSuccess);
        Assert.AreEqual(0, passIndex);
    }

    [TestMethod]
    public void BytecodePublicationAdvancesGenerationWithoutChangingRuntimeIdentity()
    {
        var assetId = Guid.NewGuid();
        var catalog = new ShaderCatalogEntry[]
        {
            CreateEntry(assetId, "ReloadableLit", ShaderIdentity.GetShaderId("Lit"), PassSemantic.Forward),
        };

        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var registry = new ShaderVariantRegistry(resourceManager, catalog);

        Assert.IsTrue(registry.TryGetVariantIndex(assetId, out var index));
        var handle = registry.GetVariant(index).Shader;
        registry.PublishBytecodeReady(assetId);
        Assert.AreEqual(1u, registry.GetGeneration(index));
        registry.PublishBytecodeReady(assetId);

        Assert.AreEqual(index, registry.GetVariant(index).Index);
        Assert.AreEqual(handle, registry.GetVariant(index).Shader);
        Assert.AreEqual(2u, registry.GetGeneration(index));
        Assert.AreEqual(ShaderVariantState.BytecodeReady, registry.GetState(index));
    }

    [TestMethod]
    public void DeferredLightingDeduplicatesAcrossMatchingShadingModelIds()
    {
        var firstAsset = Guid.NewGuid();
        var secondAsset = Guid.NewGuid();
        var thirdAsset = Guid.NewGuid();
        var familyId = ShaderIdentity.GetShaderId("Lit");

        var catalog = new ShaderCatalogEntry[]
        {
            CreateEntryWithShadingModel(firstAsset, "SimpleLit", familyId, 1u, PassSemantic.DeferredLighting),
            CreateEntryWithShadingModel(secondAsset, "MobileLit", familyId, 1u, PassSemantic.DeferredLighting),
            CreateEntryWithShadingModel(thirdAsset, "CustomLit", familyId, 2u, PassSemantic.DeferredLighting),
        };

        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var registry = new ShaderVariantRegistry(resourceManager, catalog);

        var dispatchVariants = registry.GetDispatchVariants(PassSemantic.DeferredLighting);
        Assert.AreEqual(2, dispatchVariants.Length);
        Assert.AreEqual(0, dispatchVariants[0].DenseIndex);
        Assert.AreEqual(2, dispatchVariants[1].DenseIndex);

        ref readonly var firstVariant = ref registry.GetVariant(new ShaderVariantIndex(0));
        ref readonly var secondVariant = ref registry.GetVariant(new ShaderVariantIndex(1));
        ref readonly var thirdVariant = ref registry.GetVariant(new ShaderVariantIndex(2));

        Assert.AreEqual(1u, firstVariant.ShadingModelId);
        Assert.AreEqual(1u, secondVariant.ShadingModelId);
        Assert.AreEqual(2u, thirdVariant.ShadingModelId);

        // When second asset (MobileLit, model 1u) becomes ready while first asset is not, representative switches to it
        registry.PublishBytecodeReady(secondAsset);
        dispatchVariants = registry.GetDispatchVariants(PassSemantic.DeferredLighting);
        Assert.AreEqual(2, dispatchVariants.Length);
        Assert.AreEqual(1, dispatchVariants[0].DenseIndex);
        Assert.AreEqual(2, dispatchVariants[1].DenseIndex);
    }

    [TestMethod]
    public void VisibilityDeduplicatesMatchingBytecodeAndAssignsBin0()
    {
        var firstAsset = Guid.NewGuid();
        var secondAsset = Guid.NewGuid();
        var thirdAsset = Guid.NewGuid();
        var familyId = ShaderIdentity.GetShaderId("Lit");
        var sharedBytecode = new ulong[] { 0x1111222233334444UL, 0x5555666677778888UL };
        var customBytecode = new ulong[] { 0x9999AAAABBBBCCCCUL };

        var catalog = new ShaderCatalogEntry[]
        {
            CreateEntryWithBytecode(firstAsset, "SimpleLit", familyId, sharedBytecode, PassSemantic.Visibility),
            CreateEntryWithBytecode(secondAsset, "MobileLit", familyId, sharedBytecode, PassSemantic.Visibility),
            CreateEntryWithBytecode(thirdAsset, "CustomLit", familyId, customBytecode, PassSemantic.Visibility),
        };

        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var registry = new ShaderVariantRegistry(resourceManager, catalog);

        var dispatchVariants = registry.GetDispatchVariants(PassSemantic.Visibility);
        Assert.AreEqual(2, dispatchVariants.Length);
        Assert.AreEqual(0, dispatchVariants[0].DenseIndex);
        Assert.AreEqual(1, dispatchVariants[1].DenseIndex);
        Assert.AreEqual(registry.GetVariant(new ShaderVariantIndex(0)).Shader, dispatchVariants[0].Shader);

        // When second asset becomes bytecode ready while first is not, representative switches to it
        registry.PublishBytecodeReady(secondAsset);
        dispatchVariants = registry.GetDispatchVariants(PassSemantic.Visibility);
        Assert.AreEqual(2, dispatchVariants.Length);
        Assert.AreEqual(0, dispatchVariants[0].DenseIndex);
        Assert.AreEqual(registry.GetVariant(new ShaderVariantIndex(1)).Shader, dispatchVariants[0].Shader);
    }

    private static ShaderCatalogEntry CreateEntryWithBytecode(Guid assetId, string name, ulong familyId, ulong[] bytecodeHashes, params PassSemantic[] semantics)
    {
        var shaderId = ShaderIdentity.GetShaderId(name);
        var passes = new ShaderCatalogPass[semantics.Length];
        for (var i = 0; i < semantics.Length; i++)
        {
            passes[i] = new ShaderCatalogPass
            {
                Name = semantics[i].ToString(),
                Semantic = semantics[i],
                StageMask = semantics[i] == PassSemantic.DeferredTexturing || semantics[i] == PassSemantic.DeferredLighting ? ShaderStageMask.Compute : ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                PassId = ShaderIdentity.GetPassId(shaderId, i),
                LocalPipeline = PipelineState.Default,
                ShadingModelId = 0u,
                BytecodeHashes = bytecodeHashes,
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
            Passes = passes,
        };
    }

    private static ShaderCatalogEntry CreateEntry(Guid assetId, string name, ulong familyId, params PassSemantic[] semantics)
    {
        return CreateEntryWithShadingModel(assetId, name, familyId, 0u, semantics);
    }

    private static ShaderCatalogEntry CreateEntryWithShadingModel(Guid assetId, string name, ulong familyId, uint shadingModelId, params PassSemantic[] semantics)
    {
        var shaderId = ShaderIdentity.GetShaderId(name);
        var passes = new ShaderCatalogPass[semantics.Length];
        for (var i = 0; i < semantics.Length; i++)
        {
            passes[i] = new ShaderCatalogPass
            {
                Name = semantics[i].ToString(),
                Semantic = semantics[i],
                StageMask = semantics[i] == PassSemantic.DeferredTexturing || semantics[i] == PassSemantic.DeferredLighting ? ShaderStageMask.Compute : ShaderStageMask.Mesh | ShaderStageMask.Pixel,
                PassId = ShaderIdentity.GetPassId(shaderId, i),
                LocalPipeline = PipelineState.Default,
                ShadingModelId = shadingModelId,
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
            Passes = passes,
        };
    }
}
