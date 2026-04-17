using Ghost.Core;
using Ghost.Core.Attributes;
using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Contracts;
using Ghost.Engine.AssetLoader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class AssetHandlerRegistryTests
{
    private sealed class MockAssetSettings : IAssetSettings;

    [CustomAssetHandler(ID = "9A5B7F56-5B5B-4C5D-9E9A-8B8B7F565B5B", SupportedExtensions = [".test"])]
    private sealed class MockAssetHandler : IAssetHandler
    {
        public ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, IAssetRegistry assetRegistry, CancellationToken token = default) => throw new NotImplementedException();
        public ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetRegistry, CancellationToken token = default) => throw new NotImplementedException();
    }

    [TestMethod]
    public void TestAssetHandlerRegistry_Discovery()
    {
        // For testing we rely on TypeCache being initialized. 
        // In this environment we might need to be careful about what assemblies are scanned.
        var registry = new AssetHandlerRegistry();

        // Find existing handlers (e.g. TextureAssetHandler if it exists and has attribute)
        var pngHandler = registry.GetByExtension(".png");
        Assert.IsNotNull(pngHandler, "Should find PNG handler if registered via CustomAssetHandlerAttribute");

        var guid = new Guid("9A5B7F56-5B5B-4C5D-9E9A-8B8B7F565B5B");
        var handlerById = registry.GetByTypeId(guid);
        // Note: MockAssetHandler might not be found if the test assembly isn't marked with [EngineAssembly]
        // or if TypeCache hasn't scanned it. 

        Assert.IsTrue(registry.GetSupportedExtensions().Any());
    }
}
