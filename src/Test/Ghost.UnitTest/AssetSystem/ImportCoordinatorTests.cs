using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Services;
using Microsoft.Data.Sqlite;

namespace Ghost.UnitTest.AssetSystem;

#if false
[TestClass]
public class ImportCoordinatorTests
{
    private string _testDir = null!;
    private AssetCatalog _catalog = null!;
    private ImportCoordinator _coordinator = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        EditorApplication.Initialize(null!, _testDir, "Test");

        var dbPath = Path.Combine(_testDir, "AssetDB.sqlite");
        _catalog = new AssetCatalog(dbPath);
        _coordinator = new ImportCoordinator(_catalog);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _coordinator.Dispose();
        _catalog.Dispose();

        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public async Task TestImportCoordinator_BasicImport()
    {
        var sourcePath = "Assets/test.text";
        var fullSourcePath = Path.Combine(_testDir, sourcePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullSourcePath)!);
        await File.WriteAllBytesAsync(fullSourcePath, [1, 2, 3], CancellationToken.None);

        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            AssetTypeId = Guid.NewGuid(),
            HandlerVersion = 1,
            Settings = new GenericAssetSettings()
        };

        var metaPath = AssetMetaIO.GetMetaPath(fullSourcePath);
        await AssetMetaIO.WriteAsync(metaPath, meta, CancellationToken.None);

        var job = new ImportJob(meta.Guid, sourcePath, AssetMetaIO.GetMetaPath(sourcePath), ImportReason.NewAsset);
        await _coordinator.EnqueueAsync(job);

        // Wait for the import to complete. The importer for .text will just copy the file to the library.
        var cachePath = EditorApplication.GetAssetCachePath(meta.Guid);
        using var cts = new CancellationTokenSource(5000);
        while (!File.Exists(cachePath) && !cts.IsCancellationRequested)
        {
            await Task.Delay(50, cts.Token);
        }

        Assert.IsTrue(File.Exists(cachePath));
    }
}
#endif
