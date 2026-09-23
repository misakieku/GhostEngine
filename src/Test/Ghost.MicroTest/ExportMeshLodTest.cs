using Ghost.AssetForge.Core.Bakers;
using Ghost.Core.Graphics;
using Ghost.MicroTest.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Globalization;
using System.Text;

namespace Ghost.MicroTest;

internal unsafe class ExportMeshLodTest : ITest
{
    public void Setup()
    {
        AllocationManager.Initialize();
    }

    public void Run()
    {
        string? modelPath = null;
        var args = Environment.GetCommandLineArgs();
        for (var i = 1; i < args.Length; i++)
        {
            if (File.Exists(args[i]) && (args[i].EndsWith(".obj", StringComparison.OrdinalIgnoreCase) || args[i].EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
            {
                modelPath = Path.GetFullPath(args[i]);
                break;
            }
        }

        if (modelPath == null)
        {
            modelPath = FindModelPath("dragon.obj");
        }

        if (modelPath == null)
        {
            modelPath = FindModelPath("bunny.obj");
        }

        if (modelPath == null)
        {
            throw new FileNotFoundException("Could not find dragon.obj or bunny.obj in TestGame/Assets/Meshes/.");
        }

        Console.WriteLine($"Found model: {modelPath}");

        // Place output directory in src/Test/Ghost.MicroTest/ExportedLODs for easy access
        var microTestDir = FindMicroTestDir();
        var outputDir = microTestDir != null
            ? Path.Combine(microTestDir, "ExportedLODs")
            : Path.Combine(AppContext.BaseDirectory, "ExportedLODs");

        Directory.CreateDirectory(outputDir);
        Console.WriteLine($"Output directory: {outputDir}");

        ExportMesh(modelPath, outputDir);
    }

    public static void ExportMesh(string modelPath, string outputDir)
    {
        var settings = new MeshBakeSettings
        {
            ObjectUpAxis = CoordinateAxis.PositiveY,
            ObjectForwardAxis = CoordinateAxis.NegativeZ,
            ObjectRightAxis = CoordinateAxis.PositiveX,
            UnitMeterScale = 1.0f,
            NormalDataSource = VertexDataSource.ComputedIfMissing,
            TangentDataSource = VertexDataSource.ComputedIfMissing,
            SimplifyRatio = 0.5f,
            SimplifyThreshold = 0.85f,
            OptimizeClusters = true,
        };

        var parseResult = MeshProcessor.ParseModel(modelPath, settings, AllocationHandle.Persistent);
        if (parseResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to parse model '{modelPath}': {parseResult.Message}");
        }

        var meshes = parseResult.Value;
        try
        {
            for (var m = 0; m < meshes.Count; m++)
            {
                var mesh = meshes[m];
                var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh_{m}" : mesh.Name;
                Console.WriteLine($"\n--- Processing mesh: {meshName} (Vertices: {mesh.vertices.Count}, Triangles: {mesh.indices.Count / 3}) ---");

                // 1. Export original parsed mesh as base / reference
                var originalObjPath = Path.Combine(outputDir, $"{meshName}_original.obj");
                ExportOriginalMeshAsObj(mesh, originalObjPath);
                Console.WriteLine($"[LOD Base] Original mesh exported -> {originalObjPath}");

                // 2. Build meshlets & LOD hierarchy
                using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                    mesh.vertices.AsReadOnly(),
                    mesh.indices.AsReadOnly(),
                    mesh.materialParts.AsReadOnly(),
                    settings,
                    AllocationHandle.Persistent);

                var pMeshletData = meshletDataPtr.Get();
                MeshProcessor.BuildClusterLodHierarchy(pMeshletData, AllocationHandle.Persistent);

                var lodCount = pMeshletData->lodLevelCount;
                var totalMeshlets = pMeshletData->meshlets.Count;
                Console.WriteLine($"Built {totalMeshlets} meshlets across {lodCount} LOD levels.");

                // 3. Export each LOD level as a separate OBJ file
                for (var lod = 0; lod < lodCount; lod++)
                {
                    var lodObjPath = Path.Combine(outputDir, $"{meshName}_lod_{lod}.obj");
                    ExportLodLevelAsObj(mesh, pMeshletData, lod, lodObjPath);
                }
            }
        }
        finally
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                meshes[i].Dispose();
            }
        }

        Console.WriteLine($"\nAll LOD levels successfully exported to: {outputDir}");
    }

    private static void ExportOriginalMeshAsObj(ParsedMesh mesh, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.ASCII, 65536);
        writer.WriteLine($"# GhostEngine Export: Original base mesh");
        writer.WriteLine($"# Vertices: {mesh.vertices.Count}, Triangles: {mesh.indices.Count / 3}");
        writer.WriteLine($"o {mesh.Name}_original");

        var vertSpan = mesh.vertices.AsSpan();
        for (var i = 0; i < vertSpan.Length; i++)
        {
            var p = vertSpan[i].position;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"v {p.x:F6} {p.y:F6} {p.z:F6}"));
        }

        for (var i = 0; i < vertSpan.Length; i++)
        {
            var n = vertSpan[i].normal;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"vn {n.x:F6} {n.y:F6} {n.z:F6}"));
        }

        for (var i = 0; i < vertSpan.Length; i++)
        {
            var uv = vertSpan[i].uv;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"vt {uv.x:F6} {uv.y:F6}"));
        }

        writer.WriteLine("s 1");
        var idxSpan = mesh.indices.AsSpan();
        for (var i = 0; i < idxSpan.Length; i += 3)
        {
            var i0 = idxSpan[i] + 1;
            var i1 = idxSpan[i + 1] + 1;
            var i2 = idxSpan[i + 2] + 1;
            writer.WriteLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
        }
    }

    private static void ExportLodLevelAsObj(ParsedMesh mesh, MeshletMeshData* meshletData, int targetLod, string filePath)
    {
        var meshlets = meshletData->meshlets;
        var meshletVertices = meshletData->meshletVertices;
        var meshletTriangles = meshletData->meshletTriangles;

        // Collect meshlets belonging to this LOD
        var lodMeshletIndices = new List<int>();
        var totalTrianglesInLod = 0;

        for (var m = 0; m < meshlets.Count; m++)
        {
            if (meshlets[m].lodLevel == targetLod)
            {
                lodMeshletIndices.Add(m);
                totalTrianglesInLod += meshlets[m].triangleCount;
            }
        }

        if (lodMeshletIndices.Count == 0)
        {
            Console.WriteLine($"[LOD {targetLod}] No meshlets found at this level.");
            return;
        }

        // Remap unique vertices used by this LOD level to 1-based OBJ vertex indices
        var remap = new int[mesh.vertices.Count];
        Array.Fill(remap, -1);
        var lodVertices = new List<int>();

        for (var i = 0; i < lodMeshletIndices.Count; i++)
        {
            var mIdx = lodMeshletIndices[i];
            var meshlet = meshlets[mIdx];

            for (var t = 0; t < meshlet.triangleCount; t++)
            {
                var packed = meshletTriangles[(int)meshlet.triangleOffset + t];
                var local0 = packed & 0xFFu;
                var local1 = (packed >> 8) & 0xFFu;
                var local2 = (packed >> 16) & 0xFFu;

                var v0 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local0];
                var v1 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local1];
                var v2 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local2];

                if (remap[v0] == -1)
                {
                    remap[v0] = lodVertices.Count + 1;
                    lodVertices.Add(v0);
                }
                if (remap[v1] == -1)
                {
                    remap[v1] = lodVertices.Count + 1;
                    lodVertices.Add(v1);
                }
                if (remap[v2] == -1)
                {
                    remap[v2] = lodVertices.Count + 1;
                    lodVertices.Add(v2);
                }
            }
        }

        using var writer = new StreamWriter(filePath, false, Encoding.ASCII, 65536);
        writer.WriteLine($"# GhostEngine Export: LOD {targetLod}");
        writer.WriteLine($"# Meshlets: {lodMeshletIndices.Count}, Triangles: {totalTrianglesInLod}, Vertices: {lodVertices.Count}");
        writer.WriteLine($"o {mesh.Name}_lod_{targetLod}");

        // Write vertices
        var vertSpan = mesh.vertices.AsSpan();
        for (var i = 0; i < lodVertices.Count; i++)
        {
            var p = vertSpan[lodVertices[i]].position;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"v {p.x:F6} {p.y:F6} {p.z:F6}"));
        }

        // Write normals
        for (var i = 0; i < lodVertices.Count; i++)
        {
            var n = vertSpan[lodVertices[i]].normal;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"vn {n.x:F6} {n.y:F6} {n.z:F6}"));
        }

        // Write UVs
        for (var i = 0; i < lodVertices.Count; i++)
        {
            var uv = vertSpan[lodVertices[i]].uv;
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"vt {uv.x:F6} {uv.y:F6}"));
        }

        writer.WriteLine("s 1");

        // Write faces grouped by meshlet
        for (var i = 0; i < lodMeshletIndices.Count; i++)
        {
            var mIdx = lodMeshletIndices[i];
            var meshlet = meshlets[mIdx];
            writer.WriteLine($"g Meshlet_{mIdx}_group_{meshlet.groupIndex}");

            for (var t = 0; t < meshlet.triangleCount; t++)
            {
                var packed = meshletTriangles[(int)meshlet.triangleOffset + t];
                var local0 = packed & 0xFFu;
                var local1 = (packed >> 8) & 0xFFu;
                var local2 = (packed >> 16) & 0xFFu;

                var v0 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local0];
                var v1 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local1];
                var v2 = (int)meshletVertices[(int)meshlet.vertexOffset + (int)local2];

                var obj0 = remap[v0];
                var obj1 = remap[v1];
                var obj2 = remap[v2];

                writer.WriteLine($"f {obj0}/{obj0}/{obj0} {obj1}/{obj1}/{obj1} {obj2}/{obj2}/{obj2}");
            }
        }

        Console.WriteLine($"[LOD {targetLod}] Exported ({lodMeshletIndices.Count} meshlets, {totalTrianglesInLod} tris, {lodVertices.Count} verts) -> {filePath}");
    }

    private static string? FindMicroTestDir()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (string.IsNullOrEmpty(current)) break;

            var candidate = Path.Combine(current, "src", "Test", "Ghost.MicroTest");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            candidate = Path.Combine(current, "Test", "Ghost.MicroTest");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            if (Path.GetFileName(current).Equals("Ghost.MicroTest", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(current);
            }

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        return null;
    }

    private static string? FindModelPath(string fileName)
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (string.IsNullOrEmpty(current)) break;

            var candidate = Path.Combine(current, "src", "Test", "TestGame", "Assets", "Meshes", fileName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            candidate = Path.Combine(current, "Test", "TestGame", "Assets", "Meshes", fileName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            candidate = Path.Combine(current, "TestGame", "Assets", "Meshes", fileName);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        var directCheck = Path.GetFullPath(Path.Combine("src", "Test", "TestGame", "Assets", "Meshes", fileName));
        if (File.Exists(directCheck))
        {
            return directCheck;
        }

        return null;
    }

    public void Cleanup()
    {
        AllocationManager.Dispose();
    }
}
