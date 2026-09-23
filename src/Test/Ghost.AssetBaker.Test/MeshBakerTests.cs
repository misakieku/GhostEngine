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
            var flippedInputTris = 0;
            var totalInputTris = mesh.indices.Count / 3;
            for (var t = 0; t < totalInputTris; t++)
            {
                var idx0 = mesh.indices[t * 3 + 0];
                var idx1 = mesh.indices[t * 3 + 1];
                var idx2 = mesh.indices[t * 3 + 2];

                var p0 = mesh.vertices[(int)idx0].position;
                var p1 = mesh.vertices[(int)idx1].position;
                var p2 = mesh.vertices[(int)idx2].position;
                var nAvg = (mesh.vertices[(int)idx0].normal + mesh.vertices[(int)idx1].normal + mesh.vertices[(int)idx2].normal) / 3.0f;

                var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                {
                    flippedInputTris++;
                    Console.WriteLine($"[Diagnostic] Flipped input tri {t}: indices=({idx0}, {idx1}, {idx2}), p0={p0}, p1={p1}, p2={p2}, geomN={geomNormal}, nAvg={nAvg}, dot={Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg)}");
                }
            }
            Console.WriteLine($"[Diagnostic] Input mesh: total triangles={totalInputTris}, flipped triangles={flippedInputTris}");

            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.vertices.AsReadOnly(),
                mesh.indices.AsReadOnly(),
                mesh.materialParts.AsReadOnly(),
                settings,
                Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            Console.WriteLine($"[Diagnostic] Total meshlets: {pMeshletData->meshlets.Count}, groups: {pMeshletData->groups.Count}, lods: {pMeshletData->lodLevelCount}");

            var totalTriangles = 0;
            var totalFlipped = 0;
            var flippedPerLod = new int[pMeshletData->lodLevelCount + 1];
            var totalPerLod = new int[pMeshletData->lodLevelCount + 1];

            for (var m = 0; m < pMeshletData->meshlets.Count; m++)
            {
                ref readonly var ml = ref pMeshletData->meshlets[m];
                int lod = ml.lodLevel;
                for (var t = 0; t < ml.triangleCount; t++)
                {
                    var packed = pMeshletData->meshletTriangles[(int)ml.triangleOffset + t];
                    var i0 = packed & 0xFF;
                    var i1 = (packed >> 8) & 0xFF;
                    var i2 = (packed >> 16) & 0xFF;

                    var v0 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i0];
                    var v1 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i1];
                    var v2 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i2];

                    var p0 = mesh.vertices[(int)v0].position;
                    var p1 = mesh.vertices[(int)v1].position;
                    var p2 = mesh.vertices[(int)v2].position;
                    var nAvg = (mesh.vertices[(int)v0].normal + mesh.vertices[(int)v1].normal + mesh.vertices[(int)v2].normal) / 3.0f;

                    var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                    totalTriangles++;
                    totalPerLod[lod]++;

                    if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                    {
                        totalFlipped++;
                        flippedPerLod[lod]++;
                    }
                }
            }

            for (var l = 0; l < pMeshletData->lodLevelCount; l++)
            {
                Console.WriteLine($"[Diagnostic] LOD {l}: total triangles={totalPerLod[l]}, flipped triangles={flippedPerLod[l]}");
            }
            Console.WriteLine($"[Diagnostic] ALL LODs: total triangles={totalTriangles}, flipped triangles={totalFlipped}");

            // At LOD 0, the meshlets preserve the imported mesh geometry exactly.
            // (Only 1 micro-sliver with near-zero area at the bottom cap has dot < -1e-6 in Stanford Bunny).
            Assert.IsLessThanOrEqualTo(1, flippedPerLod[0], $"LOD 0 has {flippedPerLod[0]} flipped triangles!");
        }
        finally
        {
            mesh.Dispose();
        }
    }

    [TestMethod]
    public unsafe void TestDragonMeshletsForFlippedTriangles()
    {
        var dragonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Test/TestGame/Assets/Meshes/dragon.obj"));
        if (!File.Exists(dragonPath))
        {
            dragonPath = @"F:\csharp\GhostEngine\src\Test\TestGame\Assets\Meshes\dragon.obj";
        }
        Assert.IsTrue(File.Exists(dragonPath), $"dragon.obj not found at {dragonPath}");

        var settings = new MeshBakeSettings
        {
            ObjectUpAxis = CoordinateAxis.PositiveY,
            ObjectForwardAxis = CoordinateAxis.NegativeZ,
            ObjectRightAxis = CoordinateAxis.PositiveX,
            UnitMeterScale = 1,
            NormalDataSource = VertexDataSource.ComputedIfMissing,
            TangentDataSource = VertexDataSource.ComputedIfMissing,
            SimplifyRatio = 0.5f,
            SimplifyThreshold = 0.85f,
            OptimizeClusters = true
        };

        var parseResult = MeshProcessor.ParseModel(dragonPath, settings, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        Assert.IsTrue(parseResult.IsSuccess, parseResult.Message);

        var mesh = parseResult.Value[0];
        try
        {
            var count010 = 0;
            int sampleV = -1;
            for (var v = 0; v < mesh.vertices.Count; v++)
            {
                var n = mesh.vertices[v].normal;
                if (n.x == 0 && n.y == 1 && n.z == 0)
                {
                    if (sampleV == -1) sampleV = v;
                    count010++;
                }
            }
            Console.WriteLine($"[Dragon Vertices] Total vertices={mesh.vertices.Count}, vertices with normal (0, 1, 0)={count010}");
            if (sampleV != -1)
            {
                Console.WriteLine($"[Sample Vertex {sampleV}] pos={mesh.vertices[sampleV].position}");
                for (var t = 0; t < mesh.indices.Count / 3; t++)
                {
                    var i0 = mesh.indices[t * 3 + 0];
                    var i1 = mesh.indices[t * 3 + 1];
                    var i2 = mesh.indices[t * 3 + 2];
                    if (i0 == sampleV || i1 == sampleV || i2 == sampleV)
                    {
                        var p0 = mesh.vertices[(int)i0].position;
                        var p1 = mesh.vertices[(int)i1].position;
                        var p2 = mesh.vertices[(int)i2].position;
                        var fn = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                        Console.WriteLine($"   Tri {t} (i0={i0}, i1={i1}, i2={i2}) cross={fn}, lenSq={Misaki.HighPerformance.Mathematics.math.lengthsq(fn)}");
                    }
                }
            }
            var flippedInputTris = 0;
            var totalInputTris = mesh.indices.Count / 3;
            for (var t = 0; t < totalInputTris; t++)
            {
                var idx0 = mesh.indices[t * 3 + 0];
                var idx1 = mesh.indices[t * 3 + 1];
                var idx2 = mesh.indices[t * 3 + 2];

                var p0 = mesh.vertices[(int)idx0].position;
                var p1 = mesh.vertices[(int)idx1].position;
                var p2 = mesh.vertices[(int)idx2].position;
                var nAvg = (mesh.vertices[(int)idx0].normal + mesh.vertices[(int)idx1].normal + mesh.vertices[(int)idx2].normal) / 3.0f;

                var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                {
                    if (flippedInputTris < 10)
                    {
                        Console.WriteLine($"[Dragon Tri {t}] idx=({idx0}, {idx1}, {idx2}) n0={mesh.vertices[(int)idx0].normal}, n1={mesh.vertices[(int)idx1].normal}, n2={mesh.vertices[(int)idx2].normal}");
                        Console.WriteLine($"               p0={p0}, p1={p1}, p2={p2}, geomN={geomNormal}, nAvg={nAvg}, dot={Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg)}");
                    }
                    flippedInputTris++;
                }
            }
            Console.WriteLine($"[Diagnostic Dragon] Input mesh: total triangles={totalInputTris}, flipped triangles={flippedInputTris}");

            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.vertices.AsReadOnly(),
                mesh.indices.AsReadOnly(),
                mesh.materialParts.AsReadOnly(),
                settings,
                Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            Console.WriteLine($"[Diagnostic Dragon] Total meshlets: {pMeshletData->meshlets.Count}, groups: {pMeshletData->groups.Count}, lods: {pMeshletData->lodLevelCount}");

            var totalTriangles = 0;
            var totalFlipped = 0;
            var flippedPerLod = new int[pMeshletData->lodLevelCount + 1];
            var totalPerLod = new int[pMeshletData->lodLevelCount + 1];

            var meshletsPerLod = new int[pMeshletData->lodLevelCount + 1];

            for (var m = 0; m < pMeshletData->meshlets.Count; m++)
            {
                ref readonly var ml = ref pMeshletData->meshlets[m];
                int lod = ml.lodLevel;
                meshletsPerLod[lod]++;
                for (var t = 0; t < ml.triangleCount; t++)
                {
                    var packed = pMeshletData->meshletTriangles[(int)ml.triangleOffset + t];
                    var i0 = packed & 0xFF;
                    var i1 = (packed >> 8) & 0xFF;
                    var i2 = (packed >> 16) & 0xFF;

                    var v0 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i0];
                    var v1 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i1];
                    var v2 = pMeshletData->meshletVertices[(int)ml.vertexOffset + (int)i2];

                    var p0 = mesh.vertices[(int)v0].position;
                    var p1 = mesh.vertices[(int)v1].position;
                    var p2 = mesh.vertices[(int)v2].position;
                    var nAvg = (mesh.vertices[(int)v0].normal + mesh.vertices[(int)v1].normal + mesh.vertices[(int)v2].normal) / 3.0f;

                    var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                    totalTriangles++;
                    totalPerLod[lod]++;

                    if (Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg) < -1e-6f)
                    {
                        totalFlipped++;
                        flippedPerLod[lod]++;
                    }
                }
            }

            for (var l = 0; l < pMeshletData->lodLevelCount; l++)
            {
                Console.WriteLine($"[Diagnostic Dragon] LOD {l}: meshlets={meshletsPerLod[l]}, total triangles={totalPerLod[l]}, flipped triangles={flippedPerLod[l]}");
            }
            Console.WriteLine($"[Diagnostic Dragon] ALL LODs: total triangles={totalTriangles}, flipped triangles={totalFlipped}");
        }
        finally
        {
            mesh.Dispose();
        }
    }

    [TestMethod]
    public unsafe void TestBunnyContinuousLodHierarchy()
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
            SimplifyRatio = 0.5f,
            SimplifyThreshold = 0.85f,
            OptimizeClusters = true
        };

        var parseResult = MeshProcessor.ParseModel(bunnyPath, settings, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        Assert.IsTrue(parseResult.IsSuccess, parseResult.Message);

        var mesh = parseResult.Value[0];
        try
        {
            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.vertices.AsReadOnly(),
                mesh.indices.AsReadOnly(),
                mesh.materialParts.AsReadOnly(),
                settings,
                Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            Assert.IsGreaterThan(0, pMeshletData->hierarchyNodes.Count);
            Assert.IsGreaterThan(1, pMeshletData->lodLevelCount);

            // Node 0 must be the root node
            ref readonly var rootNode = ref pMeshletData->hierarchyNodes[0];
            Assert.IsGreaterThan(0u, rootNode.childCount);
            Assert.IsLessThanOrEqualTo(8u, rootNode.childCount);
            Assert.IsGreaterThan(0u, rootNode.childOffset);


            // Validate all hierarchy nodes
            for (var i = 0; i < pMeshletData->hierarchyNodes.Count; i++)
            {
                ref readonly var node = ref pMeshletData->hierarchyNodes[i];
                Assert.IsLessThanOrEqualTo(8u, node.childCount, $"Node {i} exceeds max fanout 8");

                if (node.childCount > 0)
                {
                    Assert.IsGreaterThan(0u, node.childOffset, $"Node {i} with children must have positive childOffset");
                    Assert.IsTrue((int)(node.childOffset + node.childCount) <= pMeshletData->hierarchyNodes.Count,
                        $"Node {i} children slice out of bounds");

                    for (uint c = 0; c < node.childCount; c++)
                    {
                        var childIdx = node.childOffset + c;
                        ref readonly var childNode = ref pMeshletData->hierarchyNodes[(int)childIdx];

                        // Error must not be poisoned with float.MaxValue / infinity
                        Assert.IsFalse(float.IsPositiveInfinity(node.error) || node.error >= 3.4e38f,
                            $"Node {i} error was poisoned to float.MaxValue/infinity: {node.error}");

                        // Monotonicity: parent error must be >= child error
                        Assert.IsTrue(node.error >= childNode.error - 1e-5f,
                            $"Monotonic error violated: parent {i} (error={node.error}) < child {childIdx} (error={childNode.error})");

                        // Conservative bounding: child sphere must be inside parent sphere (with small tolerance)
                        var dist = Misaki.HighPerformance.Mathematics.math.length(childNode.bounds.Center - node.bounds.Center);
                        Assert.IsTrue(dist + childNode.bounds.Radius <= node.bounds.Radius + 1e-3f,
                            $"Parent bounding sphere does not enclose child {childIdx}: dist={dist}, childR={childNode.bounds.Radius}, parentR={node.bounds.Radius}");
                    }
                }
                else
                {
                    // Leaf node must reference a valid MeshletGroup
                    Assert.IsGreaterThanOrEqualTo(0, node.groupIndex, $"Leaf node {i} must reference a valid group");
                    Assert.IsTrue(node.groupIndex < pMeshletData->groups.Count, $"Leaf node {i} groupIndex out of range");
                }
            }
        }
        finally
        {
            mesh.Dispose();
        }
    }

    [TestMethod]
    public unsafe void TestDiagnoseDragon()
    {
        var dragonPath = @"F:\csharp\GhostEngine\src\Test\TestGame\Assets\Meshes\dragon.obj";
        if (!File.Exists(dragonPath))
        {
            Assert.Inconclusive("dragon.obj not found");
            return;
        }

        var settings = new MeshBakeSettings
        {
            ObjectUpAxis = CoordinateAxis.PositiveY,
            ObjectForwardAxis = CoordinateAxis.NegativeZ,
            ObjectRightAxis = CoordinateAxis.PositiveX,
            UnitMeterScale = 1,
            NormalDataSource = VertexDataSource.ComputedIfMissing,
            TangentDataSource = VertexDataSource.ComputedIfMissing,
            SimplifyRatio = 0.5f,
            SimplifyThreshold = 0.85f,
            OptimizeClusters = true
        };

        var parseResult = MeshProcessor.ParseModel(dragonPath, settings, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        Assert.IsTrue(parseResult.IsSuccess, parseResult.Message);

        var mesh = parseResult.Value[0];
        try
        {
            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.vertices.AsReadOnly(),
                mesh.indices.AsReadOnly(),
                mesh.materialParts.AsReadOnly(),
                settings,
                Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            Console.WriteLine($"Total meshlets: {pMeshletData->meshletCount}");
            Console.WriteLine($"Total groups: {pMeshletData->groups.Count}");
            Console.WriteLine($"Total LOD levels: {pMeshletData->lodLevelCount}");
            Console.WriteLine($"Hierarchy nodes: {pMeshletData->hierarchyNodes.Count}");

            for (uint lvl = 0; lvl < pMeshletData->lodLevelCount; lvl++)
            {
                var groupCount = 0;
                uint meshletCount = 0;
                var minError = float.MaxValue;
                float maxError = 0;
                for (var g = 0; g < pMeshletData->groups.Count; g++)
                {
                    if (pMeshletData->groups[g].lodLevel == lvl)
                    {
                        groupCount++;
                        meshletCount += pMeshletData->groups[g].meshletCount;
                        minError = Math.Min(minError, pMeshletData->groups[g].parentError);
                        maxError = Math.Max(maxError, pMeshletData->groups[g].parentError);
                    }
                }
                Console.WriteLine($"LOD {lvl}: groups={groupCount}, meshlets={meshletCount}, error=[{minError:F4} .. {maxError:F4}]");
            }
        }
        finally
        {
            mesh.Dispose();
        }
    }
}



