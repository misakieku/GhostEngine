using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine;

public static unsafe class PrefabUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset)
    {
        Span<Entity> root = stackalloc Entity[1];
        var res = InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false, 1, root);
        return res.IsSuccess ? Result.Success(root[0]) : Result.Failure<Entity>(res.Message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale)
    {
        Span<Entity> root = stackalloc Entity[1];
        var res = InstantiateCore(world, prefabAsset, position, rotation, scale, Entity.Invalid, applyTransform: true, 1, root);
        return res.IsSuccess ? Result.Success(root[0]) : Result.Failure<Entity>(res.Message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, Entity parent)
    {
        Span<Entity> root = stackalloc Entity[1];
        var res = InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false, 1, root);
        return res.IsSuccess ? Result.Success(root[0]) : Result.Failure<Entity>(res.Message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<Entity> Instantiate(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        Span<Entity> root = stackalloc Entity[1];
        var res = InstantiateCore(world, prefabAsset, position, rotation, scale, parent, applyTransform: true, 1, root);
        return res.IsSuccess ? Result.Success(root[0]) : Result.Failure<Entity>(res.Message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, Span<Entity> outEntities)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false, outEntities.Length, outEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, Span<Entity> outEntities, float3 position, quaternion rotation, float3 scale)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, Entity.Invalid, applyTransform: true, outEntities.Length, outEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, Span<Entity> outEntities, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false, outEntities.Length, outEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, Span<Entity> outEntities, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, parent, applyTransform: true, outEntities.Length, outEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, int count)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), Entity.Invalid, applyTransform: false, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, int count, float3 position, quaternion rotation, float3 scale)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, Entity.Invalid, applyTransform: true, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, int count, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, float3.zero, quaternion.identity, new float3(1.0f), parent, applyTransform: false, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Instantiate(World world, IAssetEntry prefabAsset, int count, float3 position, quaternion rotation, float3 scale, Entity parent)
    {
        return InstantiateCore(world, prefabAsset, position, rotation, scale, parent, applyTransform: true, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Result InstantiateCore(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent, bool applyTransform, int count)
    {
        return InstantiateCoreInternal(world, prefabAsset, position, rotation, scale, parent, applyTransform, count, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Result InstantiateCore(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent, bool applyTransform, int count, Span<Entity> outRoots)
    {
        return InstantiateCoreInternal(world, prefabAsset, position, rotation, scale, parent, applyTransform, count, outRoots);
    }

    private static Result InstantiateCoreInternal(World world, IAssetEntry prefabAsset, float3 position, quaternion rotation, float3 scale, Entity parent, bool applyTransform, int count, Span<Entity> outRoots)
    {
        if (count <= 0)
        {
            return Result.Failure("Count must be greater than zero.");
        }

        var validRes = ValidateAndEnsureReady(world, prefabAsset, out var prefabEntry, out var data);
        if (validRes.IsFailure)
        {
            return validRes;
        }

        var templateEntityCount = data.Entities.Length;
        var hierarchyID = ComponentTypeID<Hierarchy>.Value;
        var needHierarchy = templateEntityCount > 1 || parent.IsValid;

        // 1. Pre-resolve fixups
        var fixupCount = data.MeshMaterialFixups.Length;
        Span<Handle<Mesh>> resolvedMeshes = stackalloc Handle<Mesh>[fixupCount];
        Span<Identifier<MaterialPalette>> resolvedPalettes = stackalloc Identifier<MaterialPalette>[fixupCount];
        ResolveFixups(prefabEntry, data, resolvedMeshes, resolvedPalettes);

        // 2. Allocate and spawn all entities grouped by template entity
        using var scope = AllocationManager.CreateStackScope();
        using var spawned = new UnsafeArray<Entity>(templateEntityCount * count, scope.AllocationHandle);

        var maxCompCount = 0;
        for (var t = 0; t < templateEntityCount; t++)
        {
            if (data.Entities[t].ComponentIDs.Length > maxCompCount)
            {
                maxCompCount = data.Entities[t].ComponentIDs.Length;
            }
        }
        Span<Identifier<IComponent>> compSpan = stackalloc Identifier<IComponent>[maxCompCount + 1];
        void** ppv = stackalloc void*[maxCompCount];

        for (var t = 0; t < templateEntityCount; t++)
        {
            ref readonly var eData = ref data.Entities[t];
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
            var tSlice = spawned.AsSpan().Slice(t * count, count);
            world.EntityManager.CreateEntities(tSlice, setView);

            // Bulk populate template component data using SetComponentsSequential
            if (eData.ComponentIDs.Length > 0)
            {
                for (var c = 0; c < eData.ComponentIDs.Length; c++)
                {
                    fixed (byte* p = eData.ComponentData[c])
                    {
                        ppv[c] = p;
                    }
                }
                world.EntityManager.SetComponentsSequential(tSlice, eData.ComponentIDs, ppv);
            }
        }

        // 3. Apply mesh and material fixups in bulk
        for (var f = 0; f < fixupCount; f++)
        {
            ref readonly var fixup = ref data.MeshMaterialFixups[f];
            if (fixup.entityIndex < 0 || fixup.entityIndex >= templateEntityCount)
            {
                continue;
            }

            var tSlice = spawned.AsSpan().Slice(fixup.entityIndex * count, count);
            if (tSlice.Length > 0 && world.EntityManager.HasComponent<MeshInstance>(tSlice[0]))
            {
                var meshInstance = new MeshInstance
                {
                    mesh = resolvedMeshes[f],
                    materialPalette = resolvedPalettes[f]
                };
                world.EntityManager.SetComponentsSequential(tSlice, in meshInstance);
            }
        }

        // 4. Wire hierarchy
        if (needHierarchy)
        {
            WireHierarchy(world, data, spawned.AsSpan(), count, templateEntityCount, parent);
        }

        // 5. Apply transforms to root entities
        var roots = spawned.AsSpan().Slice(0, count);
        ApplyTransforms(world, roots, position, rotation, scale, parent, applyTransform);

        // 6. Copy output roots directly to caller's span if requested
        if (!outRoots.IsEmpty)
        {
            var copyLen = Math.Min(outRoots.Length, count);
            roots.Slice(0, copyLen).CopyTo(outRoots);
        }

        return Result.Success();
    }

    private static Result ValidateAndEnsureReady(World world, IAssetEntry prefabAsset, out PrefabAssetEntry prefabEntry, out LoadedPrefabData prefabData)
    {
        prefabEntry = null!;
        prefabData = null!;

        if (world == null)
        {
            return Result.Failure("World cannot be null.");
        }

        if (prefabAsset == null)
        {
            return Result.Failure("PrefabAsset cannot be null.");
        }

        if (prefabAsset is not PrefabAssetEntry entry)
        {
            return Result.Failure($"Expected PrefabAssetEntry but received {prefabAsset.GetType().Name}.");
        }

        if (entry.State != AssetState.Ready)
        {
            if (world.JobScheduler != null && entry.LoadJobHandle.IsValid)
            {
                world.JobScheduler.Wait(entry.LoadJobHandle);
            }

            if (entry.State != AssetState.Ready)
            {
                return Result.Failure($"Prefab asset '{entry.AssetName}' is not ready (State: {entry.State}).");
            }
        }

        var data = entry.PrefabData;
        if (data == null || data.Entities.Length == 0)
        {
            return Result.Failure($"Prefab asset '{entry.AssetName}' contains no entities.");
        }

        prefabEntry = entry;
        prefabData = data;
        return Result.Success();
    }

    private static void ResolveFixups(PrefabAssetEntry prefabEntry, LoadedPrefabData data, Span<Handle<Mesh>> resolvedMeshes, Span<Identifier<MaterialPalette>> resolvedPalettes)
    {
        var rm = prefabEntry.ResourceManager;
        var fixupCount = data.MeshMaterialFixups.Length;
        var maxMatCount = 0;
        for (var f = 0; f < fixupCount; f++)
        {
            if (data.MeshMaterialFixups[f].materialCount > maxMatCount)
            {
                maxMatCount = data.MeshMaterialFixups[f].materialCount;
            }
        }

        Span<Handle<Material>> matBuffer = stackalloc Handle<Material>[maxMatCount];

        for (var f = 0; f < fixupCount; f++)
        {
            ref readonly var fixup = ref data.MeshMaterialFixups[f];
            var meshHandle = Handle<Mesh>.Invalid;
            if (fixup.meshDependencyIndex >= 0 && fixup.meshDependencyIndex < prefabEntry.Dependencies.Length)
            {
                var meshGuid = prefabEntry.Dependencies[fixup.meshDependencyIndex];
                if (prefabEntry.Manager.TryGetEntry(meshGuid, out var meshAsset))
                {
                    meshAsset.ReadAssetData(ref meshHandle);
                }
            }
            resolvedMeshes[f] = meshHandle;

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
            resolvedPalettes[f] = matPalette;
        }
    }

    private static void WireHierarchy(World world, LoadedPrefabData data, ReadOnlySpan<Entity> spawned, int count, int templateEntityCount, Entity parent)
    {
        for (var t = 0; t < templateEntityCount; t++)
        {
            ref readonly var header = ref data.Entities[t].Header;
            var tSlice = spawned.Slice(t * count, count);

            for (var i = 0; i < count; i++)
            {
                var myEntity = tSlice[i];
                var myParent = (t == 0)
                    ? parent
                    : (header.parentIndex >= 0 && header.parentIndex < templateEntityCount
                        ? spawned[header.parentIndex * count + i]
                        : parent);

                var myFirstChild = (header.firstChildIndex >= 0 && header.firstChildIndex < templateEntityCount)
                    ? spawned[header.firstChildIndex * count + i]
                    : Entity.Invalid;

                var myNextSibling = (header.nextSiblingIndex >= 0 && header.nextSiblingIndex < templateEntityCount)
                    ? spawned[header.nextSiblingIndex * count + i]
                    : Entity.Invalid;

                var h = new Hierarchy
                {
                    parent = myParent,
                    firstChild = myFirstChild,
                    nextSibling = myNextSibling
                };
                world.EntityManager.SetComponent(myEntity, in h);
            }
        }

        if (parent.IsValid)
        {
            var rootSlice = spawned.Slice(0, count);
            if (!world.EntityManager.HasComponent<Hierarchy>(parent))
            {
                world.EntityManager.AddComponent(parent, new Hierarchy { parent = Entity.Invalid, firstChild = rootSlice[0], nextSibling = Entity.Invalid });
            }
            else
            {
                ref var parentH = ref world.EntityManager.GetComponent<Hierarchy>(parent);
                if (parentH.firstChild == Entity.Invalid)
                {
                    parentH.firstChild = rootSlice[0];
                }
                else
                {
                    var cur = parentH.firstChild;
                    while (cur.IsValid)
                    {
                        ref var curH = ref world.EntityManager.GetComponent<Hierarchy>(cur);
                        if (curH.nextSibling == Entity.Invalid)
                        {
                            curH.nextSibling = rootSlice[0];
                            break;
                        }
                        cur = curH.nextSibling;
                    }
                }
            }

            for (var i = 0; i < count - 1; i++)
            {
                ref var h = ref world.EntityManager.GetComponent<Hierarchy>(rootSlice[i]);
                h.nextSibling = rootSlice[i + 1];
            }
        }
    }

    private static void ApplyTransforms(World world, ReadOnlySpan<Entity> roots, float3 position, quaternion rotation, float3 scale, Entity parent, bool applyTransform)
    {
        var count = roots.Length;
        if (count == 0)
        {
            return;
        }

        var parentHasLtw = parent.IsValid && world.EntityManager.HasComponent<LocalToWorld>(parent);
        var parentMatrix = parentHasLtw ? world.EntityManager.GetComponent<LocalToWorld>(parent).matrix : float4x4.identity;

        if (applyTransform)
        {
            var trs = MathUtility.TRS(position, rotation, scale);
            var ltwMatrix = parentHasLtw ? math.mul(parentMatrix, trs) : trs;
            var lt = new LocalTransform { position = position, rotation = rotation, scale = scale };
            var ltw = new LocalToWorld { matrix = ltwMatrix };

            if (world.EntityManager.HasComponent<LocalTransform>(roots[0]))
            {
                world.EntityManager.SetComponentsSequential(roots, in lt);
            }

            if (world.EntityManager.HasComponent<LocalToWorld>(roots[0]))
            {
                world.EntityManager.SetComponentsSequential(roots, in ltw);
            }
        }
        else if (parentHasLtw)
        {
            for (var i = 0; i < count; i++)
            {
                var r = roots[i];
                if (world.EntityManager.HasComponent<LocalToWorld>(r))
                {
                    ref var ltw = ref world.EntityManager.GetComponent<LocalToWorld>(r);
                    if (world.EntityManager.HasComponent<LocalTransform>(r))
                    {
                        ref readonly var lt = ref world.EntityManager.GetComponent<LocalTransform>(r);
                        ltw.matrix = math.mul(parentMatrix, lt.ToMatrix());
                    }
                }
            }
        }
    }
}
