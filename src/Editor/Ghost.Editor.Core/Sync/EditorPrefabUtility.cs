using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Options controlling how a linked prefab instance is spawned in the editor.
/// </summary>
public sealed class PrefabSpawnOptions
{
    public Entity Parent { get; init; } = Entity.Invalid;
    public bool ApplyTransform { get; init; } = true;
    public Guid? InstanceGuidOverride { get; init; }
    public IReadOnlyDictionary<int, Guid>? NodeGuidOverrides { get; init; }
    public IReadOnlyDictionary<int, string>? NameOverrides { get; init; }
    public string? RootName { get; init; }
}

/// <summary>
/// Editor-side prefab instantiation helper that links spawned ECS entities back to their source prefab
/// using editor-only <see cref="PrefabLinkInfo"/> metadata and seeds mesh/material authoring GUIDs.
/// </summary>
public static class EditorPrefabUtility
{
    public readonly record struct SpawnedPrefabNode(Entity Entity, PrefabLinkInfo Link, Guid EditorGuid, string Name);

    public static bool TrySpawnLinkedPrefab(World world, IAssetEntry prefabAsset, EditorSyncBridge bridge, float3 position, quaternion rotation, float3 scale, out List<SpawnedPrefabNode> spawnedNodes, PrefabSpawnOptions? options = null)
    {
        spawnedNodes = new List<SpawnedPrefabNode>();
        var count = PrefabUtility.GetTemplateEntityCount(world, prefabAsset);
        if (count <= 0)
        {
            return false;
        }

        var parent = options?.Parent ?? Entity.Invalid;
        var applyTransform = options?.ApplyTransform ?? true;
        var spawned = new Entity[count];
        var res = PrefabUtility.InstantiateAll(world, prefabAsset, spawned, position, rotation, scale, parent, applyTransform);
        if (res.IsFailure)
        {
            return false;
        }

        var instanceGuid = options?.InstanceGuidOverride ?? Guid.NewGuid();
        var defaultRootName = !string.IsNullOrWhiteSpace(options?.RootName) ? options.RootName.Trim() : DerivePrefabDisplayName(prefabAsset);
        var nodeGuidOverrides = options?.NodeGuidOverrides;
        var nameOverrides = options?.NameOverrides;

        var em = world.EntityManager;
        for (var t = 0; t < count; t++)
        {
            var e = spawned[t];
            if (!e.IsValid || !em.Exists(e))
            {
                continue;
            }

            em.AddSharedComponent(e, new SceneID { value = bridge.ActiveSceneId });

            var editorGuid = nodeGuidOverrides != null && nodeGuidOverrides.TryGetValue(t, out var mappedGuid) && mappedGuid != Guid.Empty
                ? mappedGuid
                : Guid.NewGuid();

            var nodeName = nameOverrides != null && nameOverrides.TryGetValue(t, out var customName) && !string.IsNullOrWhiteSpace(customName)
                ? customName
                : (t == 0 ? defaultRootName : $"{defaultRootName}_Node_{t}");

            var link = new PrefabLinkInfo(instanceGuid, prefabAsset.AssetId, t);

            bridge.RegisterEntityGuid(e, editorGuid);
            bridge.SetPrefabLink(e, link);
            bridge.SetEntityName(e, nodeName);

            if (PrefabUtility.TryGetTemplateEntityFixupGuids(prefabAsset, t, out var meshGuid, out var matGuids))
            {
                bridge.SetInitialMeshAuthoring(e, meshGuid, matGuids);
            }

            spawnedNodes.Add(new SpawnedPrefabNode(e, link, editorGuid, nodeName));
        }

        return spawnedNodes.Count > 0;
    }

    public static string DerivePrefabDisplayName(IAssetEntry prefabAsset)
    {
        if (prefabAsset is AssetEntry entry && !string.IsNullOrWhiteSpace(entry.AssetName))
        {
            var fileName = Path.GetFileNameWithoutExtension(entry.AssetName);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return "Prefab";
    }
}
