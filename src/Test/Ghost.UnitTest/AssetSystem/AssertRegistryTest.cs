using Ghost.Editor.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;

namespace Ghost.UnitTest.AssetSystem;

#if false
[TestClass]
public class AssertRegistryTest
{
    private IAssetRegistry _registry = null!;

    public TestContext TestContext
    {
        get; set;
    }

    [TestInitialize]
    public void Setup()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        EditorApplication.Initialize(null!, testDir, "Test");

        _registry = new AssetRegistry();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _registry.Dispose();
    }

    [TestMethod]
    public async Task TestAssetRegistry_AutoImport()
    {
        var sourcePath = "Assets/test.text";
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], TestContext.CancellationToken);

        var metaPath = AssetMetaIO.GetMetaPath(sourcePath);

        using var cts = new CancellationTokenSource(5000);
        while (!File.Exists(metaPath) && !cts.IsCancellationRequested)
        {
            await Task.Delay(50, cts.Token);
        }

        Assert.IsTrue(File.Exists(metaPath));

        var meta = await AssetMetaIO.ReadAsync(metaPath, TestContext.CancellationToken);
        Assert.IsNotNull(meta);

        var guid = _registry.GetAssetGuid(sourcePath);
        Assert.AreEqual(meta.Guid, guid);
    }
}
#endif
