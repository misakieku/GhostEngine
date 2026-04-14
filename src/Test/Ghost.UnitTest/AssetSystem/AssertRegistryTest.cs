using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class AssertRegistryTest
{
    private string _assetsRoot = null!;
    private IAssetRegistry _registry = null!;

    [TestInitialize]
    public void Setup()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        _assetsRoot = Path.Combine(testDir, "Assets");
        _registry = new AssetRegistry(_assetsRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _registry.Dispose();
    }

    [TestMethod]
    public async Task TestAssetRegistry_AutoImport()
    {
        var sourcePath = "test.text";
        var fullSourcePath = Path.Combine(_assetsRoot, sourcePath);
        await File.WriteAllBytesAsync(fullSourcePath, [1, 2, 3]);

        await Task.Delay(1000); // Wait for FSW to trigger
        
        var metaPath = AssetMetaIO.GetMetaPath(fullSourcePath);
        Assert.IsTrue(File.Exists(metaPath));
        
        var meta = await AssetMetaIO.ReadAsync(metaPath);
        Assert.IsNotNull(meta);

        var guid = _registry.GetAssetGuid(sourcePath);
        Assert.AreEqual(meta.Guid, guid);
    }
}
