using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Ghost.Editor.Test;

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

        Assert.AreEqual(AssetType.EntityPrefab, registry.DetectAssetType(".obj"));
        Assert.AreEqual(AssetType.EntityPrefab, registry.DetectAssetType(".fbx"));
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
        var subStreams = new Dictionary<string, MemoryStream>();
        var ctx = new AssetBakerContext
        {
            AssetDirectories = [_tempDir],
            BakeConfig = new BakeConfig(),
            SubAssetStreamFactory = subPath =>
            {
                var ms = new MemoryStream();
                subStreams[subPath] = ms;
                return ms;
            }
        };

        using var outputStream = new MemoryStream();
        await baker.BakeAssetAsync(objPath, outputStream, settings, ctx, CancellationToken.None);

        Assert.IsGreaterThan(0L, outputStream.Length);

        outputStream.Position = 0;
        var headerBytes = new byte[Marshal.SizeOf<PrefabContentHeader>()];
        outputStream.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<PrefabContentHeader>(headerBytes);

        Assert.AreEqual(PrefabContentHeader.MAGIC, header.magic);
        Assert.AreEqual(PrefabContentHeader.VERSION, header.version);
        Assert.IsGreaterThan(0, header.entityCount);
        Assert.IsGreaterThan(0, ctx.AssetDependencies.Count);
        Assert.IsGreaterThan(0, header.componentTypeCount);

        // Verify sub-asset mesh was baked
        Assert.IsGreaterThan(0, subStreams.Count);
        var subMeshBytes = subStreams.Values.First().ToArray();
        var meshHeader = MemoryMarshal.Read<MeshContentHeader>(subMeshBytes);

        Assert.AreEqual(MeshContentHeader.MAGIC, meshHeader.magic);
        Assert.AreEqual(MeshContentHeader.VERSION, meshHeader.version);
        Assert.IsGreaterThan(0, meshHeader.pageCount);
        Assert.AreEqual(1, meshHeader.rootPageCount);
        Assert.IsGreaterThan(0, meshHeader.meshletCount);
        Assert.IsGreaterThan(0, meshHeader.meshletGroupCount);
        Assert.IsGreaterThan(0, meshHeader.meshletHierarchyNodeCount);
        Assert.IsGreaterThan(0, meshHeader.rayTracingProxyVertexCount);
        Assert.IsGreaterThan(0, meshHeader.rayTracingProxyIndexCount);
        Assert.IsGreaterThan(0L, meshHeader.rayTracingProxySize);
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
        var subStreams = new Dictionary<string, MemoryStream>();
        var ctx = new AssetBakerContext
        {
            AssetDirectories = [_tempDir],
            BakeConfig = new BakeConfig(),
            SubAssetStreamFactory = subPath =>
            {
                var ms = new MemoryStream();
                subStreams[subPath] = ms;
                return ms;
            }
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
        var headerBytes = new byte[Marshal.SizeOf<PrefabContentHeader>()];
        outputStream.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<PrefabContentHeader>(headerBytes);

        Assert.AreEqual(PrefabContentHeader.MAGIC, header.magic);
        Assert.AreEqual(PrefabContentHeader.VERSION, header.version);
        Assert.AreEqual(Marshal.SizeOf<PrefabContentHeader>(), (int)header.componentTypesOffset);
        Assert.IsGreaterThan(0, ctx.AssetDependencies.Count);
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

                    var p0 = v0.position;
                    var p1 = v1.position;
                    var p2 = v2.position;
                    var nAvg = (PackedVertex.UnpackNormalOctahedral(v0.packedNormal) + PackedVertex.UnpackNormalOctahedral(v1.packedNormal) + PackedVertex.UnpackNormalOctahedral(v2.packedNormal)) / 3.0f;

                    var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                    totalTriangles++;
                    totalPerLod[lod]++;

                    var dot = Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg);
                    if (dot < -1e-6f)
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
            var sampleV = -1;
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

                    var p0 = v0.position;
                    var p1 = v1.position;
                    var p2 = v2.position;
                    var nAvg = (PackedVertex.UnpackNormalOctahedral(v0.packedNormal) + PackedVertex.UnpackNormalOctahedral(v1.packedNormal) + PackedVertex.UnpackNormalOctahedral(v2.packedNormal)) / 3.0f;

                    var geomNormal = Misaki.HighPerformance.Mathematics.math.cross(p1 - p0, p2 - p0);
                    totalTriangles++;
                    totalPerLod[lod]++;

                    var dot = Misaki.HighPerformance.Mathematics.math.dot(geomNormal, nAvg);
                    if (dot < -1e-6f)
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
                    Assert.IsLessThanOrEqualTo(pMeshletData->hierarchyNodes.Count,
(int)(node.childOffset + node.childCount), $"Node {i} children slice out of bounds");

                    for (uint c = 0; c < node.childCount; c++)
                    {
                        var childIdx = node.childOffset + c;
                        ref readonly var childNode = ref pMeshletData->hierarchyNodes[(int)childIdx];

                        // Error must not be poisoned with float.MaxValue / infinity
                        Assert.IsFalse(float.IsPositiveInfinity(node.error) || node.error >= 3.4e38f,
                            $"Node {i} error was poisoned to float.MaxValue/infinity: {node.error}");

                        // Monotonicity: parent error must be >= child error
                        Assert.IsGreaterThanOrEqualTo(childNode.error - 1e-5f,
node.error, $"Monotonic error violated: parent {i} (error={node.error}) < child {childIdx} (error={childNode.error})");

                        // Conservative bounding: child sphere must be inside parent sphere (with small tolerance)
                        var dist = Misaki.HighPerformance.Mathematics.math.length(childNode.bounds.Center - node.bounds.Center);
                        Assert.IsLessThanOrEqualTo(node.bounds.Radius + 1e-3f,
dist + childNode.bounds.Radius, $"Parent bounding sphere does not enclose child {childIdx}: dist={dist}, childR={childNode.bounds.Radius}, parentR={node.bounds.Radius}");
                    }
                }
                else
                {
                    // Leaf node must reference a valid MeshletGroup
                    Assert.IsGreaterThanOrEqualTo(0, node.groupIndex, $"Leaf node {i} must reference a valid group");
                    Assert.IsLessThan(pMeshletData->groups.Count, node.groupIndex, $"Leaf node {i} groupIndex out of range");
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

    [TestMethod]
    public void TestAssetBakerContext_ImportAsset_CreatesMetadataRespectsOverwriteAndGuardsTraversal()
    {
        var registry = new BakerRegistry();
        var ctx = new AssetBakerContext
        {
            AssetDirectories = [_tempDir],
            BakeConfig = new BakeConfig(),
            BakerRegistry = registry
        };

        var initialBytes = Encoding.UTF8.GetBytes("""{ "Version": 1 }""");
        var firstGuid = ctx.ImportAsset("Props/Crate/Materials/Wood.gmat", initialBytes, overwrite: false);
        Assert.AreNotEqual(Guid.Empty, firstGuid);

        var destPath = Path.Combine(_tempDir, "Props", "Crate", "Materials", "Wood.gmat");
        var metaPath = destPath + ".meta";
        Assert.IsTrue(File.Exists(destPath));
        Assert.IsTrue(File.Exists(metaPath));
        Assert.HasCount(1, ctx.EmittedAssets);
        Assert.AreEqual("Props/Crate/Materials/Wood.gmat", ctx.EmittedAssets[0].RelativePath);

        // overwrite: false must preserve existing content and keep the same GUID
        var editedBytes = Encoding.UTF8.GetBytes("""{ "Version": 1, "Edited": true }""");
        var secondGuid = ctx.ImportAsset("Props/Crate/Materials/Wood.gmat", editedBytes, overwrite: false);
        Assert.AreEqual(firstGuid, secondGuid);
        Assert.AreEqual("""{ "Version": 1 }""", File.ReadAllText(destPath));

        // overwrite: true must replace file content while preserving .meta GUID
        var thirdGuid = ctx.ImportAsset("Props/Crate/Materials/Wood.gmat", editedBytes, overwrite: true);
        Assert.AreEqual(firstGuid, thirdGuid);
        Assert.AreEqual("""{ "Version": 1, "Edited": true }""", File.ReadAllText(destPath));

        // Traversal outside the project's primary asset directory must be rejected
        var threw = false;
        try
        {
            ctx.ImportAsset("../Outside.gmat", initialBytes);
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        Assert.IsTrue(threw, "Expected ArgumentException when escaping outside AssetDirectories[^1].");
    }

    [TestMethod]
    public async Task TestBakeService_ExtractMaterials_BakesEmittedGmatAndResolvesExternalTexture()
    {
        var assetsDir = Path.Combine(_tempDir, "Assets");
        var cacheDir = Path.Combine(_tempDir, "Cache");
        var buildDir = Path.Combine(_tempDir, "Build");
        var modelsDir = Path.Combine(assetsDir, "Models");
        var texDir = Path.Combine(modelsDir, "textures");
        Directory.CreateDirectory(texDir);
        Directory.CreateDirectory(cacheDir);
        Directory.CreateDirectory(buildDir);

        // Minimal valid 1x1 32-bit uncompressed TGA texture + pre-existing .meta
        byte[] tga1x1 = [0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 32, 0, 255, 0, 0, 255];
        var texPath = Path.Combine(texDir, "albedo.tga");
        await File.WriteAllBytesAsync(texPath, tga1x1);
        var expectedTexGuid = Guid.NewGuid();
        var texMeta = new AssetMetadata { Id = expectedTexGuid, Type = AssetType.Texture, Settings = new TextureBakeSettings() };
        await File.WriteAllTextAsync(texPath + ".meta", JsonSerializer.Serialize(texMeta, ProjectContext.JsonOptions));

        var mtlPath = Path.Combine(modelsDir, "crate.mtl");
        await File.WriteAllTextAsync(mtlPath, """
            newmtl CrateMat
            Kd 0.8 0.4 0.2
            map_Kd textures/albedo.tga
            """);

        var objPath = Path.Combine(modelsDir, "Crate.obj");
        await File.WriteAllTextAsync(objPath, """
            mtllib crate.mtl
            usemtl CrateMat
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
            """);

        var meshMeta = new AssetMetadata
        {
            Id = Guid.NewGuid(),
            Type = AssetType.EntityPrefab,
            Settings = new MeshBakeSettings { ExtractMaterials = true }
        };
        await File.WriteAllTextAsync(objPath + ".meta", JsonSerializer.Serialize(meshMeta, ProjectContext.JsonOptions));

        var project = new Project { Name = "WaveTest", RootPath = _tempDir };
        var projectCtx = new ProjectContext(project, [assetsDir], cacheDir, buildDir);
        var registry = new BakerRegistry();
        var bakeService = new BakeService(projectCtx, registry);

        var result = await bakeService.BakeAssetsAsync(new[] { ("Models/Crate.obj", objPath) }, new BakeConfig { ForceRebake = true });
        Assert.AreEqual(3, result.Total);
        Assert.AreEqual(3, result.Succeeded);
        Assert.AreEqual(0, result.Failed);
        Assert.IsTrue(File.Exists(Path.Combine(cacheDir, "Models", "textures", "albedo")), "Expected resolved external texture to be baked in follow-up wave.");

        var extractedGmatPath = Path.Combine(modelsDir, "Crate_Extracted", "Materials", "CrateMat.gmat");
        Assert.IsTrue(File.Exists(extractedGmatPath), "Expected extracted .gmat file under per-model Materials folder.");
        Assert.IsTrue(File.Exists(extractedGmatPath + ".meta"));

        var gmatDoc = JsonSerializer.Deserialize<MaterialAssetDocument>(await File.ReadAllTextAsync(extractedGmatPath));
        Assert.IsNotNull(gmatDoc);
        Assert.IsTrue(gmatDoc.Textures.TryGetValue("baseColorMap", out var mappedTexGuid));
        Assert.AreEqual(expectedTexGuid, mappedTexGuid);

        var gmatCachePath = Path.Combine(cacheDir, "Models", "Crate_Extracted", "Materials", "CrateMat");
        Assert.IsTrue(File.Exists(gmatCachePath), "Expected extracted .gmat to be baked by MaterialBaker in follow-up wave.");

        // Edit the extracted .gmat and verify rebake of Crate.obj preserves user changes (overwrite: false)
        gmatDoc.Properties["roughness"] = JsonSerializer.SerializeToElement(0.15f);
        await File.WriteAllTextAsync(extractedGmatPath, JsonSerializer.Serialize(gmatDoc));

        var rebakeResult = await bakeService.BakeAssetsAsync(new[] { ("Models/Crate.obj", objPath) }, new BakeConfig { ForceRebake = true });
        Assert.AreEqual(0, rebakeResult.Failed);

        var afterRebakeDoc = JsonSerializer.Deserialize<MaterialAssetDocument>(await File.ReadAllTextAsync(extractedGmatPath));
        Assert.IsNotNull(afterRebakeDoc);
        Assert.AreEqual(0.15f, afterRebakeDoc.Properties["roughness"].GetSingle(), 1e-4f);
    }

    [TestMethod]
    public async Task TestMeshBaker_EmbeddedSubAssetMaterialResolvesExternalTexture()
    {
        var assetsDir = Path.Combine(_tempDir, "Assets");
        var modelsDir = Path.Combine(assetsDir, "Props");
        var texDir = Path.Combine(modelsDir, "textures");
        Directory.CreateDirectory(texDir);

        byte[] tga1x1 = [0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 32, 0, 255, 0, 0, 255];
        var texPath = Path.Combine(texDir, "albedo.tga");
        await File.WriteAllBytesAsync(texPath, tga1x1);
        var expectedTexGuid = Guid.NewGuid();
        var texMeta = new AssetMetadata { Id = expectedTexGuid, Type = AssetType.Texture, Settings = new TextureBakeSettings() };
        await File.WriteAllTextAsync(texPath + ".meta", JsonSerializer.Serialize(texMeta, ProjectContext.JsonOptions));

        var mtlPath = Path.Combine(modelsDir, "crate.mtl");
        await File.WriteAllTextAsync(mtlPath, """
            newmtl CrateMat
            map_Kd textures/albedo.tga
            """);

        var objPath = Path.Combine(modelsDir, "Crate.obj");
        await File.WriteAllTextAsync(objPath, """
            mtllib crate.mtl
            usemtl CrateMat
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
            """);

        var baker = new MeshBaker();
        var settings = new MeshBakeSettings { ExtractMaterials = false };
        var subStreams = new Dictionary<string, MemoryStream>();
        var ctx = new AssetBakerContext
        {
            AssetId = Guid.NewGuid(),
            SourceFile = objPath,
            RelativePath = "Props/Crate.obj",
            AssetDirectories = [assetsDir],
            BakeConfig = new BakeConfig(),
            SubAssetStreamFactory = subPath =>
            {
                var ms = new MemoryStream();
                subStreams[subPath] = ms;
                return ms;
            }
        };

        using var outMs = new MemoryStream();
        await baker.BakeAssetAsync(objPath, outMs, settings, ctx, CancellationToken.None);

        Assert.IsFalse(File.Exists(Path.Combine(modelsDir, "Crate_Extracted", "Materials", "CrateMat.gmat")));
        Assert.IsTrue(ctx.SubAssets.Any(s => s.SubPath == "Materials/CrateMat" && s.Type == AssetType.Material && s.Dependencies.Contains(expectedTexGuid)));
    }

    [TestMethod]
    public async Task BakeAssetAsync_ZstdPageCompression_ReducesSizeAndRoundTripsLosslessly()
    {
        const int gridSize = 64;
        var sb = new System.Text.StringBuilder();
        for (var y = 0; y <= gridSize; y++)
        {
            for (var x = 0; x <= gridSize; x++)
            {
                var z = MathF.Sin(x * 0.25f) * MathF.Cos(y * 0.25f) * 0.5f;
                sb.AppendLine($"v {x * 0.1f:F4} {y * 0.1f:F4} {z:F4}");
                sb.AppendLine($"vt {x / (float)gridSize:F4} {y / (float)gridSize:F4}");
            }
        }
        sb.AppendLine("vn 0.0 0.0 1.0");

        var stride = gridSize + 1;
        for (var y = 0; y < gridSize; y++)
        {
            for (var x = 0; x < gridSize; x++)
            {
                var i0 = y * stride + x + 1;
                var i1 = i0 + 1;
                var i2 = i0 + stride;
                var i3 = i2 + 1;
                sb.AppendLine($"f {i0}/{i0}/1 {i1}/{i1}/1 {i3}/{i3}/1");
                sb.AppendLine($"f {i0}/{i0}/1 {i3}/{i3}/1 {i2}/{i2}/1");
            }
        }

        var objPath = Path.Combine(_tempDir, "dense_grid.obj");
        await File.WriteAllTextAsync(objPath, sb.ToString());

        var baker = new MeshBaker();

        async Task<byte[]> BakeWithCompression(PageCompressionMethod method)
        {
            var subStreams = new Dictionary<string, MemoryStream>();
            var ctx = new AssetBakerContext
            {
                AssetDirectories = [_tempDir],
                BakeConfig = new BakeConfig(),
                SubAssetStreamFactory = subPath =>
                {
                    var ms = new MemoryStream();
                    subStreams[subPath] = ms;
                    return ms;
                }
            };
            using var prefabOut = new MemoryStream();
            var settings = new MeshBakeSettings { PageCompression = method };
            await baker.BakeAssetAsync(objPath, prefabOut, settings, ctx, CancellationToken.None);
            return subStreams.Values.First().ToArray();
        }

        var rawBytes = await BakeWithCompression(PageCompressionMethod.None);
        var zstdBytes = await BakeWithCompression(PageCompressionMethod.Zstd);

        Assert.IsLessThan(rawBytes.Length, zstdBytes.Length, $"Expected Zstd baked mesh ({zstdBytes.Length} B) to be smaller than uncompressed ({rawBytes.Length} B).");

        var rawHeader = MemoryMarshal.Read<MeshContentHeader>(rawBytes);
        var zstdHeader = MemoryMarshal.Read<MeshContentHeader>(zstdBytes);
        Assert.AreEqual(MeshContentHeader.VERSION, zstdHeader.version);
        Assert.AreEqual(rawHeader.pageCount, zstdHeader.pageCount);
        Assert.AreEqual(rawHeader.meshletCount, zstdHeader.meshletCount);

        var rawPageDir = MemoryMarshal.Cast<byte, PageDirectoryEntry>(rawBytes.AsSpan((int)rawHeader.pageDirectoryOffset, (int)rawHeader.pageDirectorySize));
        var zstdPageDir = MemoryMarshal.Cast<byte, PageDirectoryEntry>(zstdBytes.AsSpan((int)zstdHeader.pageDirectoryOffset, (int)zstdHeader.pageDirectorySize));

        for (var p = 0; p < zstdHeader.pageCount; p++)
        {
            Assert.AreEqual(rawPageDir[p].uncompressedSize, zstdPageDir[p].uncompressedSize);
            var expectedPage = rawBytes.AsSpan((int)rawPageDir[p].fileOffset, (int)rawPageDir[p].uncompressedSize);
            var compressedPage = zstdBytes.AsSpan((int)zstdPageDir[p].fileOffset, (int)zstdPageDir[p].compressedSize);

            var decoded = new byte[zstdPageDir[p].uncompressedSize];
            if (zstdPageDir[p].compressionMethod == PageCompressionMethod.Zstd)
            {
                Assert.IsTrue(System.IO.Compression.ZstandardDecoder.TryDecompress(compressedPage, decoded, out var written));
                Assert.AreEqual((int)zstdPageDir[p].uncompressedSize, written);
            }
            else
            {
                compressedPage.CopyTo(decoded);
            }

            Assert.IsTrue(expectedPage.SequenceEqual(decoded), $"Decoded page {p} mismatch.");

            var pageHeader = MemoryMarshal.Read<PageHeader>(decoded);
            Assert.AreEqual(PageHeader.MAGIC, pageHeader.magic);
            Assert.AreEqual(0u, pageHeader.meshletOffset);
        }
    }

    [TestMethod]
    public unsafe void PackPages_QuantizedBitstreamAnd18BitIndices_DecodeAccuratelyAndCrackFree()
    {
        // Build a synthetic mesh with 2 meshlets sharing boundary vertices on an uneven extent
        var bounds = new Misaki.HighPerformance.Mathematics.Geometry.AABB(
            new Misaki.HighPerformance.Mathematics.float3(-10.0f, -5.0f, 0.0f),
            new Misaki.HighPerformance.Mathematics.float3(40.0f, 25.0f, 15.0f));

        var pShared0 = new Misaki.HighPerformance.Mathematics.float3(12.3456f, 7.8901f, 3.4567f);
        var pShared1 = new Misaki.HighPerformance.Mathematics.float3(14.5678f, 9.0123f, 5.6789f);
        var pLeft = new Misaki.HighPerformance.Mathematics.float3(-8.25f, 1.5f, 2.25f);
        var pRight = new Misaki.HighPerformance.Mathematics.float3(38.75f, 22.0f, 14.5f);

        var meshletData = (MeshletMeshData*)NativeMemory.AllocZeroed((nuint)sizeof(MeshletMeshData));
        try
        {
            meshletData->meshlets = new Misaki.HighPerformance.LowLevel.Collections.UnsafeList<Meshlet>(2, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
            meshletData->groups = new Misaki.HighPerformance.LowLevel.Collections.UnsafeList<MeshletGroup>(1, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
            meshletData->meshletVertices = new Misaki.HighPerformance.LowLevel.Collections.UnsafeList<PackedVertex>(6, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
            meshletData->meshletTriangles = new Misaki.HighPerformance.LowLevel.Collections.UnsafeList<uint>(2, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
            meshletData->lodLevelCount = 1;

            // Meshlet 0 (white color -> Color32 omitted)
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pShared0, normal = new(0, 1, 0), uv = new(0.1f, 0.2f), color = new Color128(1, 1, 1, 1) }));
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pShared1, normal = new(0, 1, 0), uv = new(0.3f, 0.4f), color = new Color128(1, 1, 1, 1) }));
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pLeft, normal = new(0, 1, 0), uv = new(0.5f, 0.6f), color = new Color128(1, 1, 1, 1) }));
            meshletData->meshletTriangles.Add(0u | (1u << 8) | (2u << 16));
            meshletData->meshlets.Add(new Meshlet { vertexOffset = 0, triangleOffset = 0, vertexCount = 3, triangleCount = 1, lodLevel = 0 });

            // Meshlet 1 (non-white color -> Color32 preserved, shares pShared0 & pShared1)
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pShared1, normal = new(0, 0, 1), uv = new(0.7f, 0.8f), color = new Color128(0.25f, 0.5f, 0.75f, 1.0f) }));
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pShared0, normal = new(0, 0, 1), uv = new(0.9f, 0.1f), color = new Color128(1, 1, 1, 1) }));
            meshletData->meshletVertices.Add(PackedVertex.Pack(new Vertex { position = pRight, normal = new(0, 0, 1), uv = new(0.2f, 0.9f), color = new Color128(1, 1, 1, 1) }));
            meshletData->meshletTriangles.Add(2u | (0u << 8) | (1u << 16));
            meshletData->meshlets.Add(new Meshlet { vertexOffset = 3, triangleOffset = 1, vertexCount = 3, triangleCount = 1, lodLevel = 0 });

            meshletData->groups.Add(new MeshletGroup { meshletStartIndex = 0, meshletCount = 2, lodLevel = 0 });
            meshletData->meshletCount = 2;
            meshletData->meshletGroupCount = 1;

            using var packed = PagePacker.PackPages(meshletData, bounds, 21, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
            Assert.HasCount(1, packed.pageBuffers);

            var page = packed.pageBuffers[0];
#pragma warning disable MSTEST0037 // Use proper 'Assert' methods
            Assert.AreEqual(packed.pageDirectory[0].uncompressedSize, (uint)page.Length);
#pragma warning restore MSTEST0037 // Use proper 'Assert' methods

            static uint ExtractBits(byte[] buf, uint byteBase, int bitOffset, int bitCount)
            {
                if (bitCount == 0) return 0u;
                var wordIndex = bitOffset >> 5;
                var bitInWord = bitOffset & 31;
                var wordAddr = (int)byteBase + wordIndex * 4;
                var val = MemoryMarshal.Read<uint>(buf.AsSpan(wordAddr, 4)) >> bitInWord;
                if (bitInWord + bitCount > 32)
                {
                    val |= MemoryMarshal.Read<uint>(buf.AsSpan(wordAddr + 4, 4)) << (32 - bitInWord);
                }
                return val & ((1u << bitCount) - 1u);
            }

            static Misaki.HighPerformance.Mathematics.float3 DecodeVertexPos(byte[] buf, Meshlet ml, int vIdx, Misaki.HighPerformance.Mathematics.float3 minBounds, float step)
            {
                var bitsX = (int)(ml.packedFormat & 0x1Fu);
                var bitsY = (int)((ml.packedFormat >> 5) & 0x1Fu);
                var bitsZ = (int)((ml.packedFormat >> 10) & 0x1Fu);
                var totalBits = bitsX + bitsY + bitsZ;

                var minX = ml.packedQuantOrigin0 & 0x1FFFFFu;
                var minY = (ml.packedQuantOrigin0 >> 21) | ((ml.packedQuantOrigin1 & 0x3FFu) << 11);
                var minZ = (ml.packedQuantOrigin1 >> 10) & 0x1FFFFFu;

                var bitOff = vIdx * totalBits;
                var qx = minX + ExtractBits(buf, ml.vertexOffset, bitOff, bitsX);
                var qy = minY + ExtractBits(buf, ml.vertexOffset, bitOff + bitsX, bitsY);
                var qz = minZ + ExtractBits(buf, ml.vertexOffset, bitOff + bitsX + bitsY, bitsZ);
                return PackedVertex.DequantizePosition(qx, qy, qz, minBounds, step);
            }

            var ml0 = packed.reorderedMeshlets[0];
            var ml1 = packed.reorderedMeshlets[1];

            Assert.AreEqual(0u, (ml0.packedFormat >> 15) & 1u, "White-only meshlet should omit Color32.");
            Assert.AreEqual(1u, (ml1.packedFormat >> 15) & 1u, "Colored meshlet should preserve Color32.");

            var ml0Shared0 = DecodeVertexPos(page, ml0, 0, bounds.Min, packed.positionQuantStep);
            var ml0Shared1 = DecodeVertexPos(page, ml0, 1, bounds.Min, packed.positionQuantStep);
            var ml1Shared1 = DecodeVertexPos(page, ml1, 0, bounds.Min, packed.positionQuantStep);
            var ml1Shared0 = DecodeVertexPos(page, ml1, 1, bounds.Min, packed.positionQuantStep);

            // Crack-free invariance: bit-identical reconstructed positions across meshlet boundaries
            Assert.AreEqual(ml0Shared0, ml1Shared0);
            Assert.AreEqual(ml0Shared1, ml1Shared1);

            // Precision within 1 quantization step
            Assert.IsLessThanOrEqualTo(packed.positionQuantStep, Misaki.HighPerformance.Mathematics.math.length(ml0Shared0 - pShared0));
            Assert.IsLessThanOrEqualTo(packed.positionQuantStep, Misaki.HighPerformance.Mathematics.math.length(ml0Shared1 - pShared1));

            // 18-bit triangle decode check
            var tri0 = ExtractBits(page, ml0.triangleOffset, 0, 18);
            Assert.AreEqual(0u, tri0 & 0x3Fu);
            Assert.AreEqual(1u, (tri0 >> 6) & 0x3Fu);
            Assert.AreEqual(2u, (tri0 >> 12) & 0x3Fu);

            var tri1 = ExtractBits(page, ml1.triangleOffset, 0, 18);
            Assert.AreEqual(2u, tri1 & 0x3Fu);
            Assert.AreEqual(0u, (tri1 >> 6) & 0x3Fu);
            Assert.AreEqual(1u, (tri1 >> 12) & 0x3Fu);
        }
        finally
        {
            meshletData->Dispose();
            NativeMemory.Free(meshletData);
        }
    }
}



