using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Test;

[TestClass]
public class MeshBakerTests
{
    private string _tempDir = "";

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostMeshBakerTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
            }
        }
    }

    [TestMethod]
    public void TestMeshBakerRegisteredInBakerRegistry()
    {
        var registry = new BakerRegistry();

        var objBaker = registry.GetBaker(".obj");
        Assert.IsNotNull(objBaker);
        Assert.IsInstanceOfType(objBaker, typeof(MeshBaker));

        var fbxBaker = registry.GetBaker(".fbx");
        Assert.IsNotNull(fbxBaker);
        Assert.IsInstanceOfType(fbxBaker, typeof(MeshBaker));

        Assert.AreEqual(AssetType.Mesh, registry.DetectAssetType(".obj"));
        Assert.AreEqual(AssetType.Mesh, registry.DetectAssetType(".fbx"));
        Assert.AreEqual(typeof(MeshBakeSettings), registry.GetSettingsType(".obj"));
    }

    [TestMethod]
    public async Task TestBakeSimpleObjMesh()
    {
        var objContent = """
            v 0.0 0.0 0.0
            v 1.0 0.0 0.0
            v 1.0 1.0 0.0
            v 0.0 1.0 0.0
            vn 0.0 0.0 1.0
            vt 0.0 0.0
            vt 1.0 0.0
            vt 1.0 1.0
            vt 0.0 1.0
            f 1/1/1 2/2/1 3/3/1
            f 1/1/1 3/3/1 4/4/1
            """;

        var objPath = Path.Combine(_tempDir, "quad.obj");
        await File.WriteAllTextAsync(objPath, objContent);

        var baker = new MeshBaker();
        var settings = new MeshBakeSettings();
        var ctx = new AssetBakerContext
        {
            ShaderMetadata = new ShaderMetadata(),
            AssetDirectories = [_tempDir],
        };

        using var outputStream = new MemoryStream();
        await baker.BakeAssetAsync(objPath, outputStream, settings, ctx, CancellationToken.None);

        Assert.IsGreaterThan(0L, outputStream.Length);

        outputStream.Position = 0;
        var headerBytes = new byte[Marshal.SizeOf<MeshContentHeader>()];
        outputStream.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<MeshContentHeader>(headerBytes);

        Assert.AreEqual(MeshContentHeader.MAGIC, header.magic);
        Assert.AreEqual(MeshContentHeader.VERSION, header.version);
        Assert.AreEqual(4, header.vertexCount);
        Assert.AreEqual(6, header.indexCount);
        Assert.IsGreaterThan(0, header.meshletCount);
        Assert.IsGreaterThan(0, header.meshletGroupCount);
        Assert.IsGreaterThan(0, header.meshletHierarchyNodeCount);

        Assert.AreEqual(32, Marshal.SizeOf<MeshletHierarchyNode>());
    }
}
