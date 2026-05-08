using Ghost.Editor.Core.Assets;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class AssetMetaTests
{
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public async Task TestAssetMeta_ReadWrite()
    {
        var metaPath = Path.Combine(_testDir, "test.png.gmeta");
        var originalMeta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            AssetTypeId = Guid.NewGuid(),
            HandlerVersion = 1,
            Labels = ["test", "hero"]
        };

        await AssetMetaIO.WriteAsync(metaPath, originalMeta);
        Assert.IsTrue(File.Exists(metaPath));

        var loadedMeta = await AssetMetaIO.ReadAsync(metaPath);
        Assert.IsNotNull(loadedMeta);
        Assert.AreEqual(originalMeta.Guid, loadedMeta.Guid);
        Assert.AreEqual(originalMeta.AssetTypeId, loadedMeta.AssetTypeId);
        Assert.AreEqual(originalMeta.HandlerVersion, loadedMeta.HandlerVersion);
        CollectionAssert.AreEqual(originalMeta.Labels, loadedMeta.Labels);
    }

    [TestMethod]
    public void TestAssetMetaIO_Paths()
    {
        var sourcePath = "f:/assets/hero.png";
        var expectedMetaPath = "f:/assets/hero.png.gmeta";

        Assert.AreEqual(expectedMetaPath, AssetMetaIO.GetMetaPath(sourcePath));
        Assert.AreEqual(sourcePath, AssetMetaIO.GetSourcePath(expectedMetaPath));
    }
}
