using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Services;
using Microsoft.Data.Sqlite;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class ImportCoordinatorTests
{
    private string _assetsRoot = null!;
    private string _libraryRoot = null!;
    private string _dbPath = null!;

    [TestInitialize]
    public void Setup()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        _assetsRoot = Path.Combine(testDir, "Assets");
        _libraryRoot = Path.Combine(testDir, "Library");
        _dbPath = Path.Combine(_libraryRoot, "AssetDB.sqlite");

        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(_libraryRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        var dir = Path.GetDirectoryName(_libraryRoot);
        if (dir != null && Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException)
            {
                Thread.Sleep(100);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }

    [TestMethod]
    public async Task TestImportCoordinator_BasicImport()
    {
        var catalog = new AssetCatalog(_dbPath);
        using var coordinator = new ImportCoordinator(catalog);

        var assetGuid = Guid.NewGuid();
        var sourcePath = "test.png";
        var fullSourcePath = Path.Combine(_assetsRoot, sourcePath);
        await File.WriteAllBytesAsync(fullSourcePath, [1, 2, 3]);

        var meta = new AssetMeta { Guid = assetGuid };
        var metaPath = AssetMetaIO.GetMetaPath(fullSourcePath);
        await AssetMetaIO.WriteAsync(metaPath, meta);

        catalog.Upsert(meta, sourcePath);

        await coordinator.EnqueueAsync(new ImportJob(assetGuid, sourcePath, metaPath, ImportReason.NewAsset));
    }
}
