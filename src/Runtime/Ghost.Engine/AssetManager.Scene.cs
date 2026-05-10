using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Text;

namespace Ghost.Engine;

internal partial class AssetEntry
{
    private static void RegisterSceneCallback()
    {
        s_onCreation[(int)AssetType.Scene] = null;
        s_onParseRawData[(int)AssetType.Scene] = static (e) => e.ParseSceneData();
        s_onRecordUpload[(int)AssetType.Scene] = static (e, ctx) => Result.Success();
        s_onUploadComplete[(int)AssetType.Scene] = null;
        s_onReleaseResource[(int)AssetType.Scene] = null;
    }

    private unsafe Result ParseSceneData()
    {
        var pData = (byte*)_rawData.GetUnsafePtr();
        var dataSize = _rawData.Size;

        if (dataSize < 12u)
        {
            return Result.Failure("Scene binary data is too small.");
        }

        var magic = Encoding.UTF8.GetString(pData, 4);
        if (magic != "GSCN")
        {
            return Result.Failure("Invalid scene binary magic number.");
        }

        return Result.Success();
    }
}

internal partial class AssetManager
{
    internal unsafe void* GetSceneRawDataPtr(Guid assetID)
    {
        var entry = GetOrCreateEntry(assetID);
        Logger.DebugAssert(entry.AssetType == AssetType.Scene);
        Logger.DebugAssert(entry.State >= AssetState.Loaded);

        return entry.RawData.GetUnsafePtr();
    }

    internal int ReleaseScene(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return 0;
        }

        if (!_entries.TryGetValue(assetID, out var entry) || entry.AssetType != AssetType.Scene)
        {
            return 0;
        }

        return entry.Release();
    }
}

public static class SceneLoader
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

    public static unsafe Result<int> LoadSceneIntoWorld(World world, void* pRawData, int dataSize)
    {
        var reader = new SpanReader(new ReadOnlySpan<byte>(pRawData, dataSize));

        var magic = Encoding.UTF8.GetString(reader.ReadSpan<byte>(4));
        if (magic != "GSCN")
        {
            return Result.Failure("Invalid scene binary magic.");
        }

        var version = reader.Read<int>();
        if (version != 1)
        {
            return Result.Failure($"Unsupported scene binary version: {version}");
        }

        using var scope = AllocationManager.CreateStackScope();

        var entityCount = reader.Read<int>();
        using var entityInfos = new BinaryEntityInfoArray(entityCount, scope.AllocationHandle);
        using var forwardMap = new UnsafeHashMap<int, Entity>(entityCount, scope.AllocationHandle);

        for (var i = 0; i < entityCount; i++)
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
                reader.Position += dataSz;

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
                    dataOffset = dataOff,
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

        for (var i = 0; i < entityCount; i++)
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

        for (var i = 0; i < entityCount; i++)
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

        for (var i = 0; i < entityCount; i++)
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

        return Result.Success(entityCount);
    }
}
