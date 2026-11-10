using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine;

public static unsafe class PrefabUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, Entity.Invalid, applyTransform: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, parent, applyTransform: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, string virtualPath)
    {
        var entry = assetManager.ResolveAsset(virtualPath);
        return InstantiateCore(world, entry, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, string virtualPath, float3 position, quaternion rotation, float3 scale)
    {
        var entry = assetManager.ResolveAsset(virtualPath);
        return InstantiateCore(world, entry, position, rotation, scale, Entity.Invalid, applyTransform: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, string virtualPath, Entity parent)
    {
        var entry = assetManager.ResolveAsset(virtualPath);
        return InstantiateCore(world, entry, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, string virtualPath, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        var entry = assetManager.ResolveAsset(virtualPath);
        return InstantiateCore(world, entry, position, rotation, scale, parent, applyTransform: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, Guid assetId)
    {
        var entry = assetManager.ResolveAsset(assetId);
        return InstantiateCore(world, entry, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, Guid assetId, float3 position, quaternion rotation, float3 scale)
    {
        var entry = assetManager.ResolveAsset(assetId);
        return InstantiateCore(world, entry, position, rotation, scale, Entity.Invalid, applyTransform: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, Guid assetId, Entity parent)
    {
        var entry = assetManager.ResolveAsset(assetId);
        return InstantiateCore(world, entry, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, AssetManager assetManager, Guid assetId, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        var entry = assetManager.ResolveAsset(assetId);
        return InstantiateCore(world, entry, position, rotation, scale, parent, applyTransform: true);
    }

    private static Result<Entity> InstantiateCore(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent, bool applyTransform)
    {
        if (world == null)
        {
            return Result.Failure<Entity>("World cannot be null.");
        }

        if (prefabAsset == null)
        {
            return Result.Failure<Entity>("PrefabAsset cannot be null.");
        }

        if (prefabAsset is not PrefabAssetEntry prefabEntry)
        {
            return Result.Failure<Entity>($"Expected PrefabAssetEntry but received {prefabAsset.GetType().Name}.");
        }

        if (prefabEntry.State != AssetState.Ready)
        {
            if (world.JobScheduler != null && prefabEntry.LoadJobHandle.IsValid)
            {
                world.JobScheduler.Wait(prefabEntry.LoadJobHandle);
            }

            if (prefabEntry.State != AssetState.Ready)
            {
                return Result.Failure<Entity>($"Prefab asset '{prefabEntry.AssetName}' is not ready (State: {prefabEntry.State}).");
            }
        }

        var data = prefabEntry.PrefabData;
        if (data == null || data.Entities.Length == 0)
        {
            return Result.Failure<Entity>($"Prefab asset '{prefabEntry.AssetName}' contains no entities.");
        }

        var entityCount = data.Entities.Length;
        var spawned = new Entity[entityCount];
        var hierarchyID = ComponentTypeID<Hierarchy>.Value;
        var needHierarchy = entityCount > 1 || parent.IsValid;

        var maxCompCount = 0;
        for (var i = 0; i < entityCount; i++)
        {
            if (data.Entities[i].ComponentIDs.Length > maxCompCount)
            {
                maxCompCount = data.Entities[i].ComponentIDs.Length;
            }
        }
        Span<Identifier<IComponent>> compSpan = stackalloc Identifier<IComponent>[maxCompCount + 1];

        for (var i = 0; i < entityCount; i++)
        {
            ref readonly var eData = ref data.Entities[i];
            var hasHierarchy = false;
            for (var c = 0; c < eData.ComponentIDs.Length; c++)
            {
                if (eData.ComponentIDs[c] == hierarchyID)
                {
                    hasHierarchy = true;
                    break;
                }
            }

            var totalCompCount = eData.ComponentIDs.Length + (needHierarchy && !hasHierarchy ? 1 : 0);
            var entityComps = compSpan.Slice(0, totalCompCount);
            eData.ComponentIDs.AsSpan().CopyTo(entityComps);
            if (needHierarchy && !hasHierarchy)
            {
                entityComps[eData.ComponentIDs.Length] = hierarchyID;
            }

            var setView = new ComponentSetView(entityComps);
            var entity = world.EntityManager.CreateEntity(setView);
            spawned[i] = entity;
        }

        for (var i = 0; i < entityCount; i++)
        {
            var entity = spawned[i];
            ref readonly var eData = ref data.Entities[i];
            for (var c = 0; c < eData.ComponentIDs.Length; c++)
            {
                var compId = eData.ComponentIDs[c];
                var bytes = eData.ComponentData[c];
                fixed (byte* pBytes = bytes)
                {
                    world.EntityManager.SetComponent(entity, compId, pBytes);
                }
            }
        }

        if (needHierarchy)
        {
            for (var i = 0; i < entityCount; i++)
            {
                ref readonly var header = ref data.Entities[i].Header;
                var h = new Hierarchy
                {
                    parent = (i == 0)
                        ? parent
                        : (header.parentIndex >= 0 && header.parentIndex < entityCount ? spawned[header.parentIndex] : parent),
                    firstChild = (header.firstChildIndex >= 0 && header.firstChildIndex < entityCount)
                        ? spawned[header.firstChildIndex]
                        : Entity.Invalid,
                    nextSibling = (header.nextSiblingIndex >= 0 && header.nextSiblingIndex < entityCount)
                        ? spawned[header.nextSiblingIndex]
                        : Entity.Invalid
                };
                world.EntityManager.SetComponent(spawned[i], in h);
            }

            if (parent.IsValid)
            {
                if (!world.EntityManager.HasComponent<Hierarchy>(parent))
                {
                    world.EntityManager.AddComponent(parent, new Hierarchy { parent = Entity.Invalid, firstChild = spawned[0], nextSibling = Entity.Invalid });
                }
                else
                {
                    ref var parentH = ref world.EntityManager.GetComponent<Hierarchy>(parent);
                    if (parentH.firstChild == Entity.Invalid)
                    {
                        parentH.firstChild = spawned[0];
                    }
                    else
                    {
                        var cur = parentH.firstChild;
                        while (cur.IsValid)
                        {
                            ref var curH = ref world.EntityManager.GetComponent<Hierarchy>(cur);
                            if (curH.nextSibling == Entity.Invalid)
                            {
                                curH.nextSibling = spawned[0];
                                break;
                            }
                            cur = curH.nextSibling;
                        }
                    }
                }
            }
        }

        var rm = prefabEntry.ResourceManager;
        var maxMatCount = 0;
        for (var f = 0; f < data.MeshMaterialFixups.Length; f++)
        {
            if (data.MeshMaterialFixups[f].materialCount > maxMatCount)
            {
                maxMatCount = data.MeshMaterialFixups[f].materialCount;
            }
        }
        Span<Handle<Material>> matBuffer = stackalloc Handle<Material>[maxMatCount];

        for (var f = 0; f < data.MeshMaterialFixups.Length; f++)
        {
            ref readonly var fixup = ref data.MeshMaterialFixups[f];
            if (fixup.entityIndex < 0 || fixup.entityIndex >= entityCount)
            {
                continue;
            }

            var targetEntity = spawned[fixup.entityIndex];
            var meshHandle = Handle<Mesh>.Invalid;
            if (fixup.meshDependencyIndex >= 0 && fixup.meshDependencyIndex < prefabEntry.Dependencies.Length)
            {
                var meshGuid = prefabEntry.Dependencies[fixup.meshDependencyIndex];
                if (prefabEntry.Manager.TryGetEntry(meshGuid, out var meshAsset))
                {
                    meshAsset.ReadAssetData(ref meshHandle);
                }
            }

            var matPalette = new Identifier<MaterialPalette>(-1);
            if (fixup.materialCount > 0 && rm != null)
            {
                var matHandles = matBuffer.Slice(0, fixup.materialCount);
                for (var m = 0; m < fixup.materialCount; m++)
                {
                    var depIdx = fixup.materialDependencyStartIndex + m;
                    var matHandle = Handle<Material>.Invalid;
                    if (depIdx >= 0 && depIdx < prefabEntry.Dependencies.Length)
                    {
                        var matGuid = prefabEntry.Dependencies[depIdx];
                        if (prefabEntry.Manager.TryGetEntry(matGuid, out var matAsset))
                        {
                            matAsset.ReadAssetData(ref matHandle);
                        }
                    }
                    matHandles[m] = matHandle;
                }

                var paletteIndex = rm.GetOrCreateMaterialPalette(matHandles);
                matPalette = new Identifier<MaterialPalette>(paletteIndex);
            }

            if (world.EntityManager.HasComponent<MeshInstance>(targetEntity))
            {
                ref var meshInstance = ref world.EntityManager.GetComponent<MeshInstance>(targetEntity);
                meshInstance.mesh = meshHandle;
                meshInstance.materialPalette = matPalette;
            }
        }

        var root = spawned[0];
        if (applyTransform)
        {
            if (world.EntityManager.HasComponent<LocalTransform>(root))
            {
                ref var lt = ref world.EntityManager.GetComponent<LocalTransform>(root);
                lt.position = position;
                lt.rotation = rotation;
                lt.scale = scale;
            }

            if (world.EntityManager.HasComponent<LocalToWorld>(root))
            {
                ref var ltw = ref world.EntityManager.GetComponent<LocalToWorld>(root);
                if (parent.IsValid && world.EntityManager.HasComponent<LocalToWorld>(parent))
                {
                    ref readonly var parentLtw = ref world.EntityManager.GetComponent<LocalToWorld>(parent);
                    ltw.matrix = math.mul(parentLtw.matrix, MathUtility.TRS(position, rotation, scale));
                }
                else
                {
                    ltw.matrix = MathUtility.TRS(position, rotation, scale);
                }
            }
        }
        else if (parent.IsValid && world.EntityManager.HasComponent<LocalToWorld>(parent) && world.EntityManager.HasComponent<LocalToWorld>(root))
        {
            ref readonly var parentLtw = ref world.EntityManager.GetComponent<LocalToWorld>(parent);
            ref var ltw = ref world.EntityManager.GetComponent<LocalToWorld>(root);
            if (world.EntityManager.HasComponent<LocalTransform>(root))
            {
                ref readonly var lt = ref world.EntityManager.GetComponent<LocalTransform>(root);
                ltw.matrix = math.mul(parentLtw.matrix, lt.ToMatrix());
            }
        }

        return Result.Success(root);
    }
}
