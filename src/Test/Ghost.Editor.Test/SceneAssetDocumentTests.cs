using System.Reflection;
using System.Text.Json;
using Ghost.AssetForge.Core.Models;
using Ghost.Engine.Components;
using Ghost.Entities;

namespace Ghost.Editor.Test;

[TestClass]
public class SceneAssetDocumentTests
{
    [TestMethod]
    public void TransientComponentAttribute_MarksOnlyRuntimeAllocatedComponents()
    {
        Assert.IsNotNull(typeof(GPUInstanceRef).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNotNull(typeof(GPUViewRef).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNotNull(typeof(SceneID).GetCustomAttribute<TransientComponentAttribute>());

        Assert.IsNull(typeof(LocalToWorld).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNull(typeof(LocalTransform).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNull(typeof(MeshInstance).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNull(typeof(RenderVolume).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNull(typeof(RenderVolumeDataContainer).GetCustomAttribute<TransientComponentAttribute>());
        Assert.IsNull(typeof(Hierarchy).GetCustomAttribute<TransientComponentAttribute>());
    }

    [TestMethod]
    public void SceneJsonTokens_RoundTripTaggedReferencesAndRejectPlainStrings()
    {
        var assetGuid = Guid.NewGuid();
        var entityGuid = Guid.NewGuid();
        var mat1 = Guid.NewGuid();
        var mat2 = Guid.Empty;
        var mat3 = Guid.NewGuid();

        var assetElem = SceneJsonTokens.CreateAssetRef(assetGuid);
        var entityElem = SceneJsonTokens.CreateEntityRef(entityGuid);
        var paletteElem = SceneJsonTokens.CreatePaletteRef(new[] { mat1, mat2, mat3 });
        var plainString = JsonSerializer.SerializeToElement(assetGuid.ToString(), SceneAssetDocumentContext.Default.String);

        Assert.IsTrue(SceneJsonTokens.TryGetAssetRef(assetElem, out var parsedAsset));
        Assert.AreEqual(assetGuid, parsedAsset);
        Assert.IsFalse(SceneJsonTokens.TryGetEntityRef(assetElem, out _));
        Assert.IsFalse(SceneJsonTokens.TryGetPaletteRef(assetElem, out _));

        Assert.IsTrue(SceneJsonTokens.TryGetEntityRef(entityElem, out var parsedEntity));
        Assert.AreEqual(entityGuid, parsedEntity);
        Assert.IsFalse(SceneJsonTokens.TryGetAssetRef(entityElem, out _));

        Assert.IsTrue(SceneJsonTokens.TryGetPaletteRef(paletteElem, out var parsedPalette));
        CollectionAssert.AreEqual(new[] { mat1, mat2, mat3 }, parsedPalette);
        Assert.IsFalse(SceneJsonTokens.TryGetAssetRef(paletteElem, out _));

        Assert.IsFalse(SceneJsonTokens.TryGetAssetRef(plainString, out _));
        Assert.IsFalse(SceneJsonTokens.TryGetEntityRef(plainString, out _));
        Assert.IsFalse(SceneJsonTokens.TryGetPaletteRef(plainString, out _));
    }

    [TestMethod]
    public void SceneAssetDocument_RoundTripsEntitiesVolumesAndPrefabOverrides()
    {
        var camGuid = Guid.NewGuid();
        var volGuid = Guid.NewGuid();
        var childGuid = Guid.NewGuid();
        var prefabInstanceGuid = Guid.NewGuid();
        var prefabAssetGuid = Guid.NewGuid();
        var node0Guid = Guid.NewGuid();
        var node1Guid = Guid.NewGuid();
        var skyboxGuid = Guid.NewGuid();
        var matOverrideGuid = Guid.NewGuid();

        var doc = new SceneAssetDocument
        {
            Entities = new List<SceneEntityRecord>
            {
                new()
                {
                    Guid = camGuid,
                    Name = "Main Camera",
                    ParentGuid = null,
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = typeof(LocalTransform).FullName!,
                            Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                            {
                                ["position"] = JsonSerializer.SerializeToElement(new[] { 0.0f, 2.0f, -5.0f }, SceneAssetDocumentContext.Default.SingleArray),
                                ["scale"] = JsonSerializer.SerializeToElement(new[] { 1.0f, 1.0f, 1.0f }, SceneAssetDocumentContext.Default.SingleArray)
                            }
                        },
                        new()
                        {
                            Type = typeof(Camera).FullName!,
                            Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                            {
                                ["focalLength"] = JsonSerializer.SerializeToElement(24.0f, SceneAssetDocumentContext.Default.Single),
                                ["gateFit"] = JsonSerializer.SerializeToElement("Vertical", SceneAssetDocumentContext.Default.String)
                            }
                        }
                    }
                },
                new()
                {
                    Guid = volGuid,
                    Name = "Global Volume",
                    ParentGuid = null,
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = typeof(RenderVolumeDataContainer).FullName!,
                            VolumeOverrides = new List<SceneVolumeOverrideRecord>
                            {
                                new()
                                {
                                    Type = "Ghost.Engine.Volumes.ExposureSettings",
                                    Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                                    {
                                        ["fixedExposure"] = JsonSerializer.SerializeToElement(10.5f, SceneAssetDocumentContext.Default.Single)
                                    }
                                },
                                new()
                                {
                                    Type = "Ghost.Engine.Volumes.EnvironmentSettings",
                                    Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                                    {
                                        ["skybox"] = SceneJsonTokens.CreateAssetRef(skyboxGuid)
                                    }
                                }
                            }
                        }
                    }
                },
                new()
                {
                    Guid = childGuid,
                    Name = "Camera Mount",
                    ParentGuid = node1Guid,
                    Components = new List<SceneComponentRecord>
                    {
                        new()
                        {
                            Type = "Game.FollowTarget",
                            Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                            {
                                ["target"] = SceneJsonTokens.CreateEntityRef(node0Guid)
                            }
                        }
                    }
                }
            },
            PrefabInstances = new List<ScenePrefabInstanceRecord>
            {
                new()
                {
                    InstanceGuid = prefabInstanceGuid,
                    PrefabGuid = prefabAssetGuid,
                    ParentGuid = camGuid,
                    NodeGuids = new Dictionary<int, Guid>
                    {
                        [0] = node0Guid,
                        [1] = node1Guid
                    },
                    NameOverrides = new Dictionary<int, string>
                    {
                        [0] = "Hero Vehicle"
                    },
                    PropertyOverrides = new List<ScenePropertyOverrideRecord>
                    {
                        new()
                        {
                            TargetGuid = node1Guid,
                            Component = typeof(MeshInstance).FullName!,
                            Field = nameof(MeshInstance.materialPalette),
                            Value = SceneJsonTokens.CreatePaletteRef(new[] { matOverrideGuid, Guid.Empty })
                        }
                    },
                    AddedComponents = new List<SceneAddedComponentRecord>
                    {
                        new()
                        {
                            TargetGuid = node0Guid,
                            Component = typeof(RenderVolume).FullName!,
                            Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                            {
                                ["isGlobal"] = JsonSerializer.SerializeToElement(false, SceneAssetDocumentContext.Default.Boolean),
                                ["priority"] = JsonSerializer.SerializeToElement(5, SceneAssetDocumentContext.Default.Int32)
                            }
                        }
                    },
                    RemovedComponents = new List<SceneRemovedComponentRecord>
                    {
                        new()
                        {
                            TargetGuid = node1Guid,
                            Component = "Game.OptionalMarker"
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(doc, SceneAssetDocumentContext.Default.SceneAssetDocument);
        var loaded = JsonSerializer.Deserialize(json, SceneAssetDocumentContext.Default.SceneAssetDocument);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(SceneAssetDocument.CURRENT_VERSION, loaded.Version);
        Assert.HasCount(3, loaded.Entities);
        Assert.HasCount(1, loaded.PrefabInstances);

        Assert.AreEqual("Main Camera", loaded.Entities[0].Name);
        Assert.IsNull(loaded.Entities[0].ParentGuid);
        Assert.AreEqual(24.0f, loaded.Entities[0].Components[1].Fields["focalLength"].GetSingle());
        Assert.AreEqual("Vertical", loaded.Entities[0].Components[1].Fields["gateFit"].GetString());

        var volOverrides = loaded.Entities[1].Components[0].VolumeOverrides;
        Assert.IsNotNull(volOverrides);
        Assert.HasCount(2, volOverrides);
        Assert.IsTrue(SceneJsonTokens.TryGetAssetRef(volOverrides[1].Fields["skybox"], out var loadedSky));
        Assert.AreEqual(skyboxGuid, loadedSky);

        Assert.AreEqual(node1Guid, loaded.Entities[2].ParentGuid);
        Assert.IsTrue(SceneJsonTokens.TryGetEntityRef(loaded.Entities[2].Components[0].Fields["target"], out var loadedTarget));
        Assert.AreEqual(node0Guid, loadedTarget);

        var loadedPrefab = loaded.PrefabInstances[0];
        Assert.AreEqual(prefabInstanceGuid, loadedPrefab.InstanceGuid);
        Assert.AreEqual(prefabAssetGuid, loadedPrefab.PrefabGuid);
        Assert.AreEqual(camGuid, loadedPrefab.ParentGuid);
        Assert.AreEqual(node0Guid, loadedPrefab.NodeGuids[0]);
        Assert.AreEqual(node1Guid, loadedPrefab.NodeGuids[1]);
        Assert.AreEqual("Hero Vehicle", loadedPrefab.NameOverrides![0]);

        Assert.IsNotNull(loadedPrefab.PropertyOverrides);
        Assert.HasCount(1, loadedPrefab.PropertyOverrides);
        Assert.IsTrue(SceneJsonTokens.TryGetPaletteRef(loadedPrefab.PropertyOverrides[0].Value, out var loadedPalette));
        CollectionAssert.AreEqual(new[] { matOverrideGuid, Guid.Empty }, loadedPalette);

        Assert.IsNotNull(loadedPrefab.AddedComponents);
        Assert.HasCount(1, loadedPrefab.AddedComponents);
        Assert.AreEqual(5, loadedPrefab.AddedComponents[0].Fields["priority"].GetInt32());

        Assert.IsNotNull(loadedPrefab.RemovedComponents);
        Assert.HasCount(1, loadedPrefab.RemovedComponents);
        Assert.AreEqual("Game.OptionalMarker", loadedPrefab.RemovedComponents[0].Component);
    }
}
