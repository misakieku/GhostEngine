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

    [TestMethod]
    public async Task BakeAssetAsync_WithInitialStreamOffset_PreservesPrefixAndComputesRelativeOffsets()
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

        var objPath = Path.Combine(_tempDir, "quad_offset.obj");
        await File.WriteAllTextAsync(objPath, objContent);

        var baker = new MeshBaker();
        var settings = new MeshBakeSettings();
        var ctx = new AssetBakerContext
        {
            ShaderMetadata = new ShaderMetadata(),
            AssetDirectories = [_tempDir],
        };

        using var outputStream = new MemoryStream();
        // Simulate a 16-byte CacheFileHeader prefix
        var prefix = new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        outputStream.Write(prefix);

        await baker.BakeAssetAsync(objPath, outputStream, settings, ctx, CancellationToken.None);

        // Verify prefix was NOT overwritten
        outputStream.Position = 0;
        var readPrefix = new byte[16];
        outputStream.ReadExactly(readPrefix);
        CollectionAssert.AreEqual(prefix, readPrefix);

        // Verify header starts at offset 16 and has valid magic and relative offsets
        var headerBytes = new byte[Marshal.SizeOf<MeshContentHeader>()];
        outputStream.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<MeshContentHeader>(headerBytes);

        Assert.AreEqual(MeshContentHeader.MAGIC, header.magic);
        Assert.AreEqual(MeshContentHeader.VERSION, header.version);
        Assert.AreEqual(Marshal.SizeOf<MeshContentHeader>(), (int)header.vertexOffset);
        Assert.IsGreaterThan(0, (int)header.vertexOffset);
        Assert.IsGreaterThan((int)header.vertexOffset, (int)header.indexOffset);
    }

    [TestMethod]
    public unsafe void TestBunnyMeshletsForFlippedTriangles()
    {
        var bunnyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Test/TestGame/Assets/Meshes/bunny.obj"));
        if (!File.Exists(bunnyPath))
        {
            bunnyPath = @"F:\csharp\GhostEngine\src\Test\TestGame\Assets\Meshes\bunny.obj";
        }
        Assert.IsTrue(File.Exists(bunnyPath), $"bunny.obj not found at {bunnyPath}");

        var settings = new MeshBakeSettings
        {
            ObjectUpAxis = CoordinateAxis.PositiveY,
            ObjectForwardAxis = CoordinateAxis.NegativeZ,
            ObjectRightAxis = CoordinateAxis.PositiveX,
            UnitMeterScale = 1,
            NormalDataSource = VertexDataSource.ComputedIfMissing,
            TangentDataSource = VertexDataSource.ComputedIfMissing,
            MaxVerticesPerMeshlet = 64,
            MinTrianglesPerMeshlet = 32,
            MaxTrianglesPerMeshlet = 124,
            SimplifyRatio = 0.5f,
            SimplifyThreshold = 0.85f,
            OptimizeClusters = true
        };

        var parseResult = MeshProcessor.ParseModel(bunnyPath, settings, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        Assert.IsTrue(parseResult.IsSuccess, parseResult.Message);

        var mesh = parseResult.Value[0];
        try
        {
            // First check the parsed mesh itself (LOD 0 source triangles before clustering)
            int flippedInputTris = 0;
            int totalInputTris = mesh.Indices.Count / 3;
            for (int t = 0; t < totalInputTris; t++)
            {
                uint idx0 = mesh.Indices[t * 3 + 0];
                uint idx1 = mesh.Indices[t * 3 + 1];
                uint idx2 = mesh.Indices[t * 3 + 2];

                var p0 = mesh.Vertices[(int)idx0].position;
                var p1 = mesh.Vertices[(int)idx1].position;
                var p2 = mesh.Vertices[(int)idx2].position;
                var nAvg = (mesh.Vertices[(int)idx0].normal + mesh.Vertices[(int)idx1].normal + mesh.Vertices[(int)idx2].normal) / 3.0f;

                var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                {
                    flippedInputTris++;
                }
            }
            Console.WriteLine($"[Diagnostic] Input mesh: total triangles={totalInputTris}, flipped triangles={flippedInputTris}");

            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.Vertices.AsReadOnly(),
                mesh.Indices.AsReadOnly(),
                mesh.MaterialParts.AsReadOnly(),
                settings,
                Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            Console.WriteLine($"[Diagnostic] Total meshlets: {pMeshletData->meshlets.Count}, groups: {pMeshletData->groups.Count}, lods: {pMeshletData->lodLevelCount}");

            int totalTriangles = 0;
            int totalFlipped = 0;
            var flippedPerLod = new int[pMeshletData->lodLevelCount + 1];
            var totalPerLod = new int[pMeshletData->lodLevelCount + 1];

            for (int m = 0; m < pMeshletData->meshlets.Count; m++)
            {
                ref readonly var ml = ref pMeshletData->meshlets[m];
                int lod = ml.lodLevel;
                for (int t = 0; t < ml.triangleCount; t++)
                {
                    uint packed = pMeshletData->meshletTriangles[(int)ml.triangleOffset + t];
                    uint i0 = packed & 0xFF;
                    uint i1 = (packed >> 8) & 0xFF;
                    uint i2 = (packed >> 16) & 0xFF;

                    uint v0 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i0];
                    uint v1 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i1];
                    uint v2 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i2];

                    var p0 = mesh.Vertices[(int)v0].position;
                    var p1 = mesh.Vertices[(int)v1].position;
                    var p2 = mesh.Vertices[(int)v2].position;
                    var nAvg = (mesh.Vertices[(int)v0].normal + mesh.Vertices[(int)v1].normal + mesh.Vertices[(int)v2].normal) / 3.0f;

                    var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                    totalTriangles++;
                    totalPerLod[lod]++;

                    if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                    {
                        // If we swap i1 and i2:
                        var correctedNormal = Misaki.HighPerformance.Mathematics.math.cross(p2 - p0, p1 - p0);
                        Assert.IsTrue(Misaki.HighPerformance.Mathematics.math.dot(correctedNormal, nAvg) > -1e-6f,
                            $"Flipped triangle at LOD {lod} cannot be corrected by swapping i1 and i2!");
                        totalFlipped++;
                        flippedPerLod[lod]++;
                    }
                }
            }

            for (int l = 0; l < pMeshletData->lodLevelCount; l++)
            {
                Console.WriteLine($"[Diagnostic] LOD {l}: total triangles={totalPerLod[l]}, flipped triangles={flippedPerLod[l]}");
            }
            Console.WriteLine($"[Diagnostic] ALL LODs: total triangles={totalTriangles}, flipped triangles={totalFlipped}");

            // Test Cone Culling:
            // Put camera at (0, 0.1, -0.3) looking towards (0, 0.1, 0)
            var camPos = new Misaki.HighPerformance.Mathematics.float3(0, 0.1f, -0.3f);
            int totalTested = 0;
            int backfaceCulled = 0;
            int coneValid = 0;

            for (int m = 0; m < pMeshletData->meshlets.Count; m++)
            {
                ref readonly var ml = ref pMeshletData->meshlets[m];
                if (ml.lodLevel != 0) continue; // Test on LOD 0
                totalTested++;

                if (ml.cone.w < 1.0f)
                {
                    coneValid++;
                    var center = ml.boundingSphere.Center;
                    var radius = ml.boundingSphere.Radius;
                    var axis = ml.cone.xyz;
                    var cutoff = ml.cone.w;

                    var toCluster = center - camPos;
                    var dist = Misaki.HighPerformance.Mathematics.math.length(toCluster);
                    if (dist > radius)
                    {
                        if (Misaki.HighPerformance.Mathematics.math.dot(toCluster, axis) >= cutoff * dist + radius)
                        {
                            backfaceCulled++;
                        }
                    }
                }
            }
            Console.WriteLine($"[Diagnostic] LOD 0 Cone culling: total={totalTested}, valid cones={coneValid}, backface culled={backfaceCulled} ({backfaceCulled * 100.0 / totalTested:F1}%)");

            Assert.AreEqual(0, totalFlipped, $"Found {totalFlipped} flipped triangles out of {totalTriangles} total triangles!");
            Assert.IsGreaterThan(0, backfaceCulled, "Cone culling should cull backfacing meshlets");
        }
        finally
        {
            mesh.Dispose();
        }
    }
}

