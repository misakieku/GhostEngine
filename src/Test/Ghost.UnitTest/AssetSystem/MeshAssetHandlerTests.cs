using Ghost.Editor.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Services;
using Ghost.Engine.Streaming;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class MeshAssetHandlerTests
{
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private string _projectRoot = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectRoot = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_projectRoot);
        EditorApplication.Initialize(new EmptyServiceProvider(), _projectRoot, "MeshImportTest");
    }

    [TestMethod]
    public async Task FBXAssetHandler_ImportsObjAsManifestAndMeshSubAssets()
    {
        var sourcePath = Path.Combine(EditorApplication.AssetsFolderPath, "kit.obj");
        await File.WriteAllTextAsync(sourcePath, CreateTwoObjectObj(), TestContext.CancellationToken);

        var parentGuid = Guid.NewGuid();
        var targetPath = ImportCoordinator.GetImportedAssetPath(parentGuid);
        var handler = new ModelAssetHandler();

        var result = await handler.ImportAsync(sourcePath, targetPath, parentGuid, new ObjAssetSettings(), TestContext.CancellationToken);

        if (result.IsFailure && result.Message?.Contains("Unable to load DLL", StringComparison.OrdinalIgnoreCase) == true)
        {
            Assert.Inconclusive(result.Message);
        }

        Assert.IsTrue(result.IsSuccess, result.Message);
        Assert.IsTrue(File.Exists(targetPath));
        Assert.IsGreaterThanOrEqualTo(result.Value.Length, 2);

        foreach (var subAsset in result.Value)
        {
            Assert.AreEqual("Mesh", subAsset.Kind);
            Assert.IsTrue(subAsset.VirtualSourcePath.Contains("#Mesh/", StringComparison.Ordinal));

            var meshPath = ImportCoordinator.GetImportedAssetPath(subAsset.Guid);
            Assert.IsTrue(File.Exists(meshPath));

            await using var stream = new FileStream(meshPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var headerBytes = new byte[Marshal.SizeOf<MeshContentHeader>()];
            await stream.ReadExactlyAsync(headerBytes, TestContext.CancellationToken);

            var header = MemoryMarshal.Read<MeshContentHeader>(headerBytes);
            Assert.AreEqual(MeshContentHeader.MAGIC, header.magic);
            Assert.AreEqual(MeshContentHeader.VERSION, header.version);
            Assert.IsGreaterThan(0, header.vertexCount);
            Assert.IsGreaterThan(0, header.indexCount);
            Assert.IsGreaterThan(0, header.meshletCount);
            Assert.IsGreaterThan(0, header.meshletGroupCount);
            Assert.IsGreaterThan(0, header.meshletHierarchyNodeCount);
        }
    }

    public TestContext TestContext
    {
        get; set;
    } = null!;

    private static string CreateTwoObjectObj()
    {
        var sb = new StringBuilder();
        AppendGrid(sb, "PropA", 0, 0);
        AppendGrid(sb, "PropB", 49, 1);
        return sb.ToString();
    }

    private static void AppendGrid(StringBuilder sb, string name, int vertexOffset, int z)
    {
        const int size = 6;
        sb.AppendLine($"o {name}");

        for (var y = 0; y <= size; y++)
        {
            for (var x = 0; x <= size; x++)
            {
                sb.AppendLine($"v {x} {y} {z}");
                sb.AppendLine("vn 0 0 1");
                sb.AppendLine($"vt {x / (float)size} {y / (float)size}");
            }
        }

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i0 = vertexOffset + y * (size + 1) + x + 1;
                var i1 = i0 + 1;
                var i2 = i0 + size + 1;
                var i3 = i2 + 1;
                sb.AppendLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
                sb.AppendLine($"f {i1}/{i1}/{i1} {i3}/{i3}/{i3} {i2}/{i2}/{i2}");
            }
        }
    }
}
