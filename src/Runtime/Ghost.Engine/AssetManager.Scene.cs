using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Text;

namespace Ghost.Engine;

internal partial class AssetEntry
{
    private static void RegisterSceneCallback()
    {
        s_onCreation[(int)AssetType.Scene] = static (e) =>
        {
        };

        s_onParseRawData[(int)AssetType.Scene] = static (e) => e.ParseSceneData();
        s_onRecordUpload[(int)AssetType.Scene] = static (e, ctx) => Result.Success();
        s_onUploadComplete[(int)AssetType.Scene] = static (e, ctx) =>
        {
        };

        s_onReleaseResource[(int)AssetType.Scene] = static (e) =>
        {
        };
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
    private struct BinaryEntityInfo
    {
        public int entityIndex;
        public int componentCount;
        public struct ComponentInfo
        {
            public uint typeHash;
            public string typeName;
            public Identifier<IComponent> typeID;
            public int dataSize;
            public int dataOffset;
            public int entityFieldCount;
            public int[] entityFieldOffsets;
        }

        public ComponentInfo[] components;
    }

    public static unsafe Result<int> LoadSceneIntoWorld(World world, void* pRawData, int dataSize)
    {
        RegisterKnownComponentTypes();

        var pData = (byte*)pRawData;
        var offset = 0;

        var magic = Encoding.UTF8.GetString(pData + offset, 4);
        offset += 4;
        if (magic != "GSCN")
        {
            return Result.Failure("Invalid scene binary magic.");
        }

        var version = ReadInt32(pData, ref offset);
        if (version != 1)
        {
            return Result.Failure($"Unsupported scene binary version: {version}");
        }

        var entityCount = ReadInt32(pData, ref offset);
        var entityInfos = new BinaryEntityInfo[entityCount];
        var forwardMap = new Dictionary<int, Entity>(entityCount);

        for (var i = 0; i < entityCount; i++)
        {
            var compCount = ReadInt32(pData, ref offset);
            var comps = new BinaryEntityInfo.ComponentInfo[compCount];

            for (var j = 0; j < compCount; j++)
            {
                var typeHash = (uint)ReadInt32(pData, ref offset);
                var nameLength = ReadInt32(pData, ref offset);
                var typeName = Encoding.UTF8.GetString(pData + offset, nameLength);
                offset += nameLength;

                var dataSz = ReadInt32(pData, ref offset);
                var dataOff = offset;
                offset += dataSz;

                var fieldCount = ReadInt32(pData, ref offset);
                var fieldOffsets = new int[fieldCount];
                for (var f = 0; f < fieldCount; f++)
                {
                    fieldOffsets[f] = ReadInt32(pData, ref offset);
                }

                var typeID = ComponentRegistry.GetComponentIDByName(typeName);

                comps[j] = new BinaryEntityInfo.ComponentInfo
                {
                    typeHash = typeHash,
                    typeName = typeName,
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

        var pTypeIds = stackalloc Identifier<IComponent>[32];
        for (var i = 0; i < entityCount; i++)
        {
            var info = entityInfos[i];
            var validCount = 0;

            for (var j = 0; j < info.componentCount; j++)
            {
                if (info.components[j].typeID.IsValid)
                {
                    if (validCount < 32)
                    {
                        pTypeIds[validCount] = info.components[j].typeID;
                    }

                    validCount++;
                }
            }

            if (validCount > 0 && validCount <= 32)
            {
                using var scope = AllocationManager.CreateStackScope();
                using var set = new ComponentSet(scope.AllocationHandle, new ReadOnlySpan<Identifier<IComponent>>(pTypeIds, validCount));
                var entity = world.EntityManager.CreateEntity(set);
                forwardMap[i] = entity;
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
                if (!comp.typeID.IsValid)
                {
                    continue;
                }

                var compSize = ComponentRegistry.GetComponentInfo(comp.typeID).size;
                var pSrc = pData + comp.dataOffset;

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

    private static void RegisterKnownComponentTypes()
    {
        _ = ComponentTypeID<Ghost.Engine.Components.Hierarchy>.Value;
        _ = ComponentTypeID<Ghost.Engine.Components.LocalToWorld>.Value;
        _ = ComponentTypeID<Ghost.Engine.Components.SceneID>.Value;
    }

    private static unsafe int ReadInt32(byte* pData, ref int offset)
    {
        var value = *(int*)(pData + offset);
        offset += 4;
        return value;
    }
}
