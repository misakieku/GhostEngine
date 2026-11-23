using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Ghost.Editor.Test;

[TestClass]
[DoNotParallelize]
public class SceneBakerTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GhostSceneBakerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore transient file locks during test cleanup.
            }
        }
    }

    [TestMethod]
    public void BakerRegistry_RegistersSceneBakerExtensionAndSettings()
    {
        using var registry = new BakerRegistry();
        Assert.AreEqual(AssetType.Scene, registry.DetectAssetType(".gscene"));

        var baker = registry.GetBaker(".gscene");
        Assert.IsInstanceOfType<SceneBaker>(baker);

        var settings = registry.CreateDefaultSettings(".gscene");
        Assert.IsInstanceOfType<SceneBakeSettings>(settings);
        Assert.AreEqual(SceneBakeSettings.VERSION, settings.Version);
    }

    [TestMethod]
    public async Task SceneBaker_BakesEntitiesVolumesPrefabsAndDependencies_WithoutInspectingPrefabs()
    {
        var gscenePath = Path.Combine(_tempDir, "Level.gscene");
        var prefabGuid = Guid.NewGuid();
        var meshGuid = Guid.NewGuid();
        var mat0Guid = Guid.NewGuid();
        var mat1Guid = Guid.NewGuid();
        var skyTexGuid = Guid.NewGuid();
        var standaloneParentGuid = Guid.NewGuid();
        var standaloneChildGuid = Guid.NewGuid();
        var prefabNode0Guid = Guid.NewGuid();
        var prefabNode1Guid = Guid.NewGuid();

        var doc = new SceneAssetDocument
        {
            Entities = new List<SceneEntityRecord>
            {
                new()
                {
                    Guid = standaloneParentGuid,
                    Name = "SunLight",
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = "Ghost.Engine.Components.LocalTransform",
                            Fields = new Dictionary<string, JsonElement>
                            {
                                ["position"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
                                ["scale"] = JsonSerializer.SerializeToElement(new[] { 1.0f, 1.0f, 1.0f })
                            }
                        },
                        new()
                        {
                            Type = "Ghost.Engine.Components.RenderVolumeDataContainer",
                            VolumeOverrides = new List<SceneVolumeOverrideRecord>
                            {
                                new()
                                {
                                    Type = "Ghost.Engine.Volumes.HDRISettings",
                                    Fields = new Dictionary<string, JsonElement>
                                    {
                                        ["texture"] = SceneJsonTokens.CreateAssetRef(skyTexGuid),
                                        ["intensity"] = JsonSerializer.SerializeToElement(1.5f)
                                    }
                                }
                            }
                        }
                    }
                },
                new()
                {
                    Guid = standaloneChildGuid,
                    Name = "ChildMesh",
                    ParentGuid = prefabNode1Guid,
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = "Ghost.Engine.Components.MeshInstance",
                            Fields = new Dictionary<string, JsonElement>
                            {
                                ["mesh"] = SceneJsonTokens.CreateAssetRef(meshGuid),
                                ["materialPalette"] = SceneJsonTokens.CreatePaletteRef(new[] { mat0Guid, Guid.Empty, mat1Guid }),
                                ["staticShadowCaster"] = JsonSerializer.SerializeToElement(true)
                            }
                        },
                        new()
                        {
                            Type = "Game.Follower",
                            Fields = new Dictionary<string, JsonElement>
                            {
                                ["target"] = SceneJsonTokens.CreateEntityRef(standaloneParentGuid)
                            }
                        }
                    }
                }
            },
            PrefabInstances = new List<ScenePrefabInstanceRecord>
            {
                new()
                {
                    InstanceGuid = Guid.NewGuid(),
                    PrefabGuid = prefabGuid,
                    ParentGuid = standaloneParentGuid,
                    NodeGuids = new Dictionary<int, Guid>
                    {
                        [0] = prefabNode0Guid,
                        [1] = prefabNode1Guid
                    },
                    NameOverrides = new Dictionary<int, string>
                    {
                        [0] = "HeroCrate"
                    },
                    PropertyOverrides = new List<ScenePropertyOverrideRecord>
                    {
                        new()
                        {
                            TargetGuid = prefabNode0Guid,
                            Component = "Ghost.Engine.Components.LocalTransform",
                            Field = "position",
                            Value = JsonSerializer.SerializeToElement(new[] { 4.5f, 0.0f, -2.0f })
                        }
                    },
                    AddedComponents = new List<SceneAddedComponentRecord>
                    {
                        new()
                        {
                            TargetGuid = prefabNode1Guid,
                            Component = "Ghost.Engine.Components.PunctualLight",
                            Fields = new Dictionary<string, JsonElement>
                            {
                                ["type"] = JsonSerializer.SerializeToElement("Spot"),
                                ["intensity"] = JsonSerializer.SerializeToElement(800.0f)
                            }
                        }
                    },
                    RemovedComponents = new List<SceneRemovedComponentRecord>
                    {
                        new()
                        {
                            TargetGuid = prefabNode1Guid,
                            Component = "Ghost.Engine.Components.MeshInstance"
                        }
                    }
                }
            }
        };

        await File.WriteAllTextAsync(gscenePath, JsonSerializer.Serialize(doc, SceneAssetDocumentContext.Default.SceneAssetDocument));

        var ctx = new AssetBakerContext
        {
            AssetId = Guid.NewGuid(),
            SourceFile = gscenePath,
            RelativePath = "Scenes/Level.gscene",
            AssetDirectories = new[] { _tempDir },
            BakeConfig = new BakeConfig()
        };

        var baker = new SceneBaker();
        using var ms = new MemoryStream();
        await baker.BakeAssetAsync(gscenePath, ms, new SceneBakeSettings(), ctx, CancellationToken.None);

        var bytes = ms.ToArray();
        Assert.IsGreaterThan(Marshal.SizeOf<SceneContentHeader>(), bytes.Length);
        var header = MemoryMarshal.Read<SceneContentHeader>(bytes);
        Assert.AreEqual(SceneContentHeader.MAGIC, header.magic);
        Assert.AreEqual(SceneContentHeader.VERSION, header.version);
        Assert.AreEqual(2, header.entityCount);
        Assert.AreEqual(1, header.prefabInstanceCount);
        Assert.AreEqual(4, header.componentCount);
        Assert.AreEqual(1, header.volumeOverrideCount);
        Assert.AreEqual(1, header.nameOverrideCount);
        Assert.AreEqual(1, header.addedComponentCount);
        Assert.AreEqual(1, header.removedComponentCount);
        Assert.AreEqual(1, header.propertyOverrideCount);
        Assert.AreEqual(3, header.materialDependencyIndexCount);

        // Prefab file does not exist on disk: SceneBaker must have read zero external files and tracked only asset GUIDs.
        Assert.IsEmpty(ctx.Dependencies);
        Assert.Contains(skyTexGuid, ctx.AssetDependencies);
        Assert.Contains(meshGuid, ctx.AssetDependencies);
        Assert.Contains(mat0Guid, ctx.AssetDependencies);
        Assert.Contains(mat1Guid, ctx.AssetDependencies);
        Assert.Contains(prefabGuid, ctx.AssetDependencies);

        var strings = ReadStrings(bytes, in header);
        Assert.Contains("Ghost.Engine.Components.LocalTransform", strings);
        Assert.Contains("Ghost.Engine.Volumes.HDRISettings", strings);
        Assert.Contains("Ghost.Engine.Components.PunctualLight", strings);

        var entities = MemoryMarshal.Cast<byte, SceneEntityHeader>(bytes.AsSpan((int)header.entitiesOffset, header.entityCount * Marshal.SizeOf<SceneEntityHeader>()));
        Assert.AreEqual(SceneEntityTarget.Invalid, entities[0].parentTarget);
        Assert.AreEqual(new SceneEntityTarget { kind = SceneTargetKind.PrefabNode, index = 0, subIndex = 1 }, entities[1].parentTarget);

        var prefabs = MemoryMarshal.Cast<byte, ScenePrefabSpawnHeader>(bytes.AsSpan((int)header.prefabInstancesOffset, header.prefabInstanceCount * Marshal.SizeOf<ScenePrefabSpawnHeader>()));
        Assert.AreEqual(new SceneEntityTarget { kind = SceneTargetKind.Standalone, index = 0, subIndex = -1 }, prefabs[0].parentTarget);
        Assert.AreEqual(prefabGuid, ctx.AssetDependencies[prefabs[0].prefabDependencyIndex]);

        var fields = MemoryMarshal.Cast<byte, SceneFieldRecord>(bytes.AsSpan((int)header.fieldsOffset, header.fieldCount * Marshal.SizeOf<SceneFieldRecord>()));
        var matIndices = MemoryMarshal.Cast<byte, int>(bytes.AsSpan((int)header.materialDependencyIndicesOffset, header.materialDependencyIndexCount * sizeof(int)));
        var dataBlob = bytes.AsSpan((int)header.dataBlobOffset, header.dataBlobSize);

        var followerField = FindField(fields, "target");
        Assert.AreEqual(SceneFieldValueKind.EntityRef, followerField.kind);
        Assert.AreEqual(new SceneEntityTarget { kind = SceneTargetKind.Standalone, index = 0, subIndex = -1 }, followerField.entityTarget);

        var paletteField = FindField(fields, "materialPalette");
        Assert.AreEqual(SceneFieldValueKind.MaterialPalette, paletteField.kind);
        Assert.AreEqual(3, paletteField.payloadCount);
        Assert.AreEqual(mat0Guid, ctx.AssetDependencies[matIndices[paletteField.payloadOffset]]);
        Assert.AreEqual(-1, matIndices[paletteField.payloadOffset + 1]);
        Assert.AreEqual(mat1Guid, ctx.AssetDependencies[matIndices[paletteField.payloadOffset + 2]]);

        var intPosField = fields[0];
        Assert.AreEqual(CacheFileHeader.Fnv1a("position"), intPosField.fieldNameHash);
        Assert.AreEqual(SceneFieldValueKind.IntArray, intPosField.kind);
        Span<float> posFloats = stackalloc float[3];
        Assert.IsTrue(new SceneFieldReader(in intPosField, dataBlob, matIndices).TryReadSingles(posFloats));
        Assert.AreEqual(1.0f, posFloats[0]);
        Assert.AreEqual(2.0f, posFloats[1]);
        Assert.AreEqual(3.0f, posFloats[2]);
    }

    [TestMethod]
    public async Task BakeProjectAsync_ChangingDependencyDoesNotRebakeScene_AndCorruptSceneFails()
    {
        using var bakerRegistry = new BakerRegistry();
        var projectService = new ProjectService(bakerRegistry);
        projectService.CreateProject(_tempDir, "SceneBakeProj");

        var assetsDir = projectService.AssetDirectories[0];
        var matDir = Path.Combine(assetsDir, "Materials");
        var scenesDir = Path.Combine(assetsDir, "Scenes");
        Directory.CreateDirectory(matDir);
        Directory.CreateDirectory(scenesDir);

        var matPath = Path.Combine(matDir, "Floor.gmat");
        await File.WriteAllTextAsync(matPath, """{ "Version": 1, "Properties": { "roughness": 0.5 } }""");
        var matMeta = projectService.GetOrCreateMetadata(matPath)!;
        projectService.SaveMetadata(matPath + ".meta", matMeta);

        var scenePath = Path.Combine(scenesDir, "Main.gscene");
        var sceneDoc = new SceneAssetDocument
        {
            Entities = new List<SceneEntityRecord>
            {
                new()
                {
                    Guid = Guid.NewGuid(),
                    Name = "Floor",
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = "Ghost.Engine.Components.MeshInstance",
                            Fields = new Dictionary<string, JsonElement>
                            {
                                ["materialPalette"] = SceneJsonTokens.CreatePaletteRef(new[] { matMeta.Id })
                            }
                        }
                    }
                }
            }
        };
        await File.WriteAllTextAsync(scenePath, JsonSerializer.Serialize(sceneDoc, SceneAssetDocumentContext.Default.SceneAssetDocument));
        var sceneMeta = projectService.GetOrCreateMetadata(scenePath)!;
        projectService.SaveMetadata(scenePath + ".meta", sceneMeta);

        var bakeService = new BakeService(projectService.GetContext(), bakerRegistry);
        var firstBake = await bakeService.BakeProjectAsync(new BakeConfig());
        Assert.AreEqual(0, firstBake.Failed);
        Assert.AreEqual(2, firstBake.Succeeded);

        // Editing the referenced material must rebake only Floor.gmat on the next incremental bake, never Main.gscene.
        await File.WriteAllTextAsync(matPath, """{ "Version": 1, "Properties": { "roughness": 0.9 } }""");
        File.SetLastWriteTimeUtc(matPath, DateTime.UtcNow.AddSeconds(2));

        var secondBake = await bakeService.BakeProjectAsync(new BakeConfig());
        Assert.AreEqual(0, secondBake.Failed);
        Assert.AreEqual(1, secondBake.Succeeded);
        Assert.AreEqual(1, secondBake.Skipped);

        // Corrupt JSON must fail loudly.
        await File.WriteAllTextAsync(scenePath, "{ not valid json !!!");
        File.SetLastWriteTimeUtc(scenePath, DateTime.UtcNow.AddSeconds(4));
        var badBake = await bakeService.BakeProjectAsync(new BakeConfig());
        Assert.AreEqual(1, badBake.Failed);
    }

    private static string[] ReadStrings(byte[] payload, in SceneContentHeader header)
    {
        var result = new string[header.stringCount];
        var offset = (int)header.stringsOffset;
        for (var i = 0; i < header.stringCount; i++)
        {
            var len = MemoryMarshal.Read<ushort>(payload.AsSpan(offset, sizeof(ushort)));
            offset += sizeof(ushort);
            result[i] = Encoding.UTF8.GetString(payload, offset, len);
            offset += len;
        }

        return result;
    }

    private static SceneFieldRecord FindField(ReadOnlySpan<SceneFieldRecord> fields, string fieldName)
    {
        var hash = CacheFileHeader.Fnv1a(fieldName);
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i].fieldNameHash == hash)
            {
                return fields[i];
            }
        }

        Assert.Fail($"Expected baked field '{fieldName}' (hash=0x{hash:X8}).");
        return default;
    }
}
