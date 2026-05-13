using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.Engine.Streaming;

internal static class SceneLoader
{
    private struct BinaryEntityInfo : IDisposable
    {
        public int entityIndex;
        public int componentCount;
        public struct ComponentInfo
        {
            public uint typeHash;
            public Identifier<IComponent> typeID;
            public int dataSize;
            public int dataOffset;
            public int entityFieldCount;
            public UnsafeArray<int> entityFieldOffsets;
        }

        public UnsafeArray<ComponentInfo> components;

        public void Dispose()
        {
            for (var i = 0; i < components.Length; i++)
            {
                components[i].entityFieldOffsets.Dispose();
            }

            components.Dispose();
        }
    }

    private struct BinaryEntityInfoArray : IDisposable
    {
        public UnsafeArray<BinaryEntityInfo> data;

        public readonly ref BinaryEntityInfo this [int index] => ref data[index];

        public BinaryEntityInfoArray(int count, AllocationHandle handle)
        {
            data = new UnsafeArray<BinaryEntityInfo>(count, handle, AllocationOption.Clear);
        }

        public void Dispose()
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i].Dispose();
            }

            data.Dispose();
        }
    }

    public static unsafe Result<int> LoadSceneIntoWorld(World world, SceneContentHeader header, void* pRawData, nuint dataSize)
    {
        var reader = new BufferReader((byte*)pRawData, dataSize);

        using var scope = AllocationManager.CreateStackScope();

        using var entityInfos = new BinaryEntityInfoArray(header.entityCount, scope.AllocationHandle);
        using var forwardMap = new UnsafeHashMap<int, Entity>(header.entityCount, scope.AllocationHandle);

        for (var i = 0; i < header.entityCount; i++)
        {
            var compCount = reader.Read<int>();
            if (compCount == 0)
            {
                continue;
            }

            var comps = new UnsafeArray<BinaryEntityInfo.ComponentInfo>(compCount, scope.AllocationHandle);

            for (var j = 0; j < compCount; j++)
            {
                var typeHash = reader.Read<uint>();
                var nameLength = reader.Read<int>();
                var typeName = Encoding.UTF8.GetString(reader.ReadSpan<byte>(nameLength));

                var dataSz = reader.Read<int>();
                var dataOff = reader.Position;
                reader.Position += (nuint)dataSz;

                var fieldCount = reader.Read<int>();
                var fieldOffsets = new UnsafeArray<int>(fieldCount, scope.AllocationHandle);
                for (var f = 0; f < fieldCount; f++)
                {
                    fieldOffsets[f] = reader.Read<int>();
                }

                var typeID = ComponentRegistry.GetComponentIDByName(typeName);

                comps[j] = new BinaryEntityInfo.ComponentInfo
                {
                    typeHash = typeHash,
                    typeID = typeID,
                    dataSize = dataSz,
                    dataOffset = (int)dataOff,
                    entityFieldCount = fieldCount,
                    entityFieldOffsets = fieldOffsets,
                };
            }

            entityInfos[i] = new BinaryEntityInfo
            {
                entityIndex = i,
                componentCount = compCount,
                components = comps,
            };
        }

        using var typeIds = new UnsafeList<Identifier<IComponent>>(32, scope.AllocationHandle);
        typeIds.Add(ComponentTypeID<SceneID>.Value);

        for (var i = 0; i < header.entityCount; i++)
        {
            ref var info = ref entityInfos[i];

            for (var j = 0; j < info.componentCount; j++)
            {
                if (info.components[j].typeID.IsValid)
                {
                    typeIds.Add(info.components[j].typeID);
                }
            }

            var set = new ComponentSetView(typeIds);
            var entity = world.EntityManager.CreateEntity(set);

            forwardMap.TryAdd(i, entity);
            typeIds.RemoveRange(1, typeIds.Count - 1);
        }


        var activeScene = SceneManager.CreateScene();

        for (var i = 0; i < header.entityCount; i++)
        {
            if (!forwardMap.TryGetValue(i, out var entity))
            {
                continue;
            }

            world.EntityManager.SetComponent(entity, new SceneID { scene = activeScene });

            var info = entityInfos[i];
            for (var j = 0; j < info.componentCount; j++)
            {
                var comp = info.components[j];
                if (!comp.typeID.IsValid)
                {
                    continue;
                }

                var compSize = ComponentRegistry.GetComponentInfo(comp.typeID).size;
                var pSrc = (byte*)pRawData + comp.dataOffset;

                world.EntityManager.SetComponent(entity, comp.typeID, pSrc);
            }
        }

        for (var i = 0; i < header.entityCount; i++)
        {
            if (!forwardMap.TryGetValue(i, out var entity))
            {
                continue;
            }

            var info = entityInfos[i];
            for (var j = 0; j < info.componentCount; j++)
            {
                var comp = info.components[j];
                if (!comp.typeID.IsValid || comp.entityFieldCount == 0)
                {
                    continue;
                }

                var pComponent = world.EntityManager.GetComponent(entity, comp.typeID);
                if (pComponent == null)
                {
                    continue;
                }

                for (var f = 0; f < comp.entityFieldCount; f++)
                {
                    var fieldOffset = comp.entityFieldOffsets[f];
                    var pField = (byte*)pComponent + fieldOffset;
                    var fileLocalIndex = *(int*)pField;
                    if (!forwardMap.TryGetValue(fileLocalIndex, out var remappedEntity))
                    {
                        remappedEntity = Entity.Invalid;
                    }

                    *(Entity*)pField = remappedEntity;
                }
            }
        }

        return Result.Success(header.entityCount);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct SceneContentHeader
{
    public const uint MAGIC = 0x4E435347; // "GSCN" in little-endian
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public int entityCount;
}

internal unsafe class SceneAssetEntry : ProcessableAssetEntry
{
    public class AdditionalData
    {
        public required World world;
        public SceneLoadingType loadingType;
    }

    private readonly World _targetWorld;
    private readonly SceneLoadingType _loadingType;

    private MemoryBlock _memory;
    private SceneContentHeader _header;

    public SceneAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Scene, dependencies)
    {
        // TODO: How can I get this? Ideally the public api will be something like SceneManager.LoadScene(World, Scene, SceneLoadingType).
        // Should we handle the scene loading explicitly instead of auto loading on the first resolve?
        // For example if we have a component called SceneStreamer{ Scene a; Scene b; }
        // In save data, we convert the Scene(int) to a asset gui, and convert it back during load. So at ResolveScene stage (before the file even been loaded), we need to call the SceneManager.CreateScene().
        // Currently we store the world and loading type directly inside the asset entry, but actually that should not be bound with the asset itself, because we may load scene A along at the first time, then we load it additively at the second time.
        // So, maybe the scene asset entry should only create a unique id from SceneManager.CreateScene() then resolve the scene file without loading it into world.
        // Then we can load the scene into world using our job system, and user can decide to wait it immediatly (sync) or fire-and-forget (async).

        _targetWorld = World.GetWorld(0)!;
        _loadingType = SceneLoadingType.Single;
    }

    public override Result OnLoadRawData([OwnershipTransfer] ref MemoryBlock memory)
    {
        _memory = memory;
        if (_memory.Size < (nuint)sizeof(SceneContentHeader))
        {
            return Result.Failure("The size of scene is too small.");
        }

        var header = _memory.GetElementAt<SceneContentHeader>(0);
        if (header.magic != SceneContentHeader.MAGIC)
        {
            return Result.Failure($"Unexpected scene header. Expect {SceneContentHeader.MAGIC}, got {header.magic}");
        }

        // TODO: Support version update.
        if (header.version != SceneContentHeader.VERSION)
        {
            return Result.Failure($"Unexpected scene version. Expect {SceneContentHeader.VERSION}, got {header.version}");
        }

        _header = header;

        return Result.Success();
    }

    public override Result<JobHandle> OnProcessing(object? context)
    {
        var pData = (byte*)_memory.GetUnsafePtr() + sizeof(SceneContentHeader);

        if (_loadingType == SceneLoadingType.Single)
        {
            // TODO: Support TimeData.
            _targetWorld.Clear(default);
        }

        // TODO: Parallelize.
        SceneLoader.LoadSceneIntoWorld(_targetWorld, _header, pData, _memory.Size - (nuint)sizeof(SceneContentHeader));

        return JobHandle.Invalid;
    }

    public override void OnReleaseResource()
    {
    }
}
