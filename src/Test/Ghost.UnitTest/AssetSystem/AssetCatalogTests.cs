using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Services;
using Microsoft.Data.Sqlite;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class AssetCatalogTests
{
    private string _dbPath = null!;

    [TestInitialize]
    public void Setup()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        _dbPath = Path.Combine(testDir, "AssetDB.sqlite");
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        var dir = Path.GetDirectoryName(_dbPath);
        if (dir != null && Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException)
            {
                // Sometimes SQLite holds a lock for a bit longer
                Thread.Sleep(100);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }

    [TestMethod]
    public void TestAssetCatalog_UpsertLookup()
    {
        using var catalog = new AssetCatalog(_dbPath);
        var guid = Guid.NewGuid();
        var meta = new AssetMeta { Guid = guid, HandlerVersion = 1 };
        var path = "Textures/hero.png";

        catalog.Upsert(meta, path);

        Assert.AreEqual(guid, catalog.GetGuid(path));
        Assert.AreEqual(path, catalog.GetSourcePath(guid));
    }

    [TestMethod]
    public void TestAssetCatalog_Dependencies()
    {
        using var catalog = new AssetCatalog(_dbPath);
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();

        catalog.Upsert(new AssetMeta { Guid = asset1 }, "test1.png");
        catalog.Upsert(new AssetMeta { Guid = asset2 }, "test2.png");

        catalog.SetDependencies(asset1, stackalloc[] { asset2 });

        var referencers = catalog.GetReferencers(asset2);
        Assert.AreEqual(1, referencers.Count);
        Assert.AreEqual(asset1, referencers[0]);
    }
}
