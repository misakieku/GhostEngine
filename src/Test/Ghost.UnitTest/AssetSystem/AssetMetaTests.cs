using Ghost.Editor.Core.Assets;

namespace Ghost.UnitTest.AssetSystem;

#if false
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
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            AssetTypeId = Guid.NewGuid(),
            HandlerVersion = 1,
            Settings = new GenericAssetSettings()
        };

        var metaPath = Path.Combine(_testDir, "test.meta");

        await AssetMetaIO.WriteAsync(metaPath, meta, CancellationToken.None);

        Assert.IsTrue(File.Exists(metaPath));

        var readMeta = await AssetMetaIO.ReadAsync(metaPath, CancellationToken.None);

        Assert.IsNotNull(readMeta);
        Assert.AreEqual(meta.Guid, readMeta.Guid);
        Assert.AreEqual(meta.AssetTypeId, readMeta.AssetTypeId);
        Assert.AreEqual(meta.HandlerVersion, readMeta.HandlerVersion);
    }

    [TestMethod]
    public void TestAssetMetaIO_GetPaths()
    {
        var sourcePath = "Assets/Textures/logo.png";
        var metaPath = "Assets/Textures/logo.png" + AssetMetaIO.META_EXTENSION;

        Assert.AreEqual(metaPath, AssetMetaIO.GetMetaPath(sourcePath));
        Assert.AreEqual(sourcePath, AssetMetaIO.GetSourcePath(metaPath));
    }
}
#endif
