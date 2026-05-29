using Ghost.Editor.Core.Assets;
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
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            ForeignKeys = true,
            Pooling = true
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        SqliteConnection.ClearPool(connection);
    }

    [TestMethod]
    public void TestAssetCatalog_UpsertLookup()
    {
        var catalog = new AssetCatalog(_dbPath);
        var guid = Guid.NewGuid();
        var meta = new AssetMeta { Guid = guid, HandlerVersion = 1 };
        var path = "Textures/hero.png";

        catalog.Upsert(meta, path);

        Assert.AreEqual(guid, catalog.GetGuid(path));
        Assert.AreEqual(Path.GetFullPath(path).Replace('\\', '/'), catalog.GetSourcePath(guid));
    }

    [TestMethod]
    public void TestAssetCatalog_Dependencies()
    {
        var catalog = new AssetCatalog(_dbPath);
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();

        catalog.Upsert(new AssetMeta { Guid = asset1 }, "test1.png");
        catalog.Upsert(new AssetMeta { Guid = asset2 }, "test2.png");

        catalog.SetDependencies(asset1, stackalloc[] { asset2 });

        var referencers = catalog.GetReferencers(asset2);
        Assert.AreEqual(1, referencers.Count);
        Assert.AreEqual(asset1, referencers[0]);
    }

    [TestMethod]
    public void TestAssetCatalog_VirtualSubAssets()
    {
        var catalog = new AssetCatalog(_dbPath);
        var parent = Guid.NewGuid();
        var subMesh = Guid.NewGuid();
        var handlerTypeId = Guid.NewGuid();

        catalog.Upsert(new AssetMeta { Guid = parent, AssetTypeId = handlerTypeId, HandlerVersion = 1 }, "Props/kit.fbx");
        catalog.UpsertSubAsset(parent,
            new AssetMeta { Guid = subMesh, AssetTypeId = handlerTypeId, HandlerVersion = 1 },
            "Props/kit.fbx#Mesh/Root/Crate",
            "Mesh",
            "Crate",
            "Root/Crate");
        catalog.SetDependencies(parent, stackalloc[] { subMesh });

        Assert.AreEqual(subMesh, catalog.GetGuid("Props/kit.fbx#Mesh/Root/Crate"));
        var subAssets = catalog.GetSubAssets(parent);
        Assert.AreEqual(1, subAssets.Count);
        Assert.AreEqual(subMesh, subAssets[0].Guid);
        Assert.AreEqual(parent, subAssets[0].ParentGuid);
        Assert.AreEqual("Mesh", subAssets[0].Kind);
        Assert.AreEqual("Crate", subAssets[0].DisplayName);
        Assert.AreEqual("Root/Crate", subAssets[0].StablePath);

        var dependencies = catalog.GetDependencies(parent);
        Assert.AreEqual(1, dependencies.Count);
        Assert.AreEqual(subMesh, dependencies[0]);
    }
}
