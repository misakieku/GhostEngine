using Ghost.Core;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.Engine.Streaming;

internal sealed class LoadedSceneData
{
    public SceneContentHeader Header;
    public string[] Strings = Array.Empty<string>();
    public Identifier<IComponent>[] StringComponentIDs = Array.Empty<Identifier<IComponent>>();
    public ushort[] StringVolumeTypeIDs = Array.Empty<ushort>();
    public SceneEntityHeader[] Entities = Array.Empty<SceneEntityHeader>();
    public ScenePrefabSpawnHeader[] PrefabInstances = Array.Empty<ScenePrefabSpawnHeader>();
    public SceneComponentHeader[] Components = Array.Empty<SceneComponentHeader>();
    public SceneFieldRecord[] Fields = Array.Empty<SceneFieldRecord>();
    public SceneVolumeOverrideHeader[] VolumeOverrides = Array.Empty<SceneVolumeOverrideHeader>();
    public int[] MaterialDependencyIndices = Array.Empty<int>();
    public ScenePrefabNameOverrideHeader[] NameOverrides = Array.Empty<ScenePrefabNameOverrideHeader>();
    public SceneAddedComponentHeader[] AddedComponents = Array.Empty<SceneAddedComponentHeader>();
    public SceneRemovedComponentHeader[] RemovedComponents = Array.Empty<SceneRemovedComponentHeader>();
    public ScenePropertyOverrideHeader[] PropertyOverrides = Array.Empty<ScenePropertyOverrideHeader>();
    public byte[] DataBlob = Array.Empty<byte>();
}

internal sealed unsafe class SceneAssetEntry : AssetEntry, ILoadableAssetEntry
{
    private LoadedSceneData? _sceneData;

    internal LoadedSceneData? SceneData => _sceneData;
    internal new ResourceManager ResourceManager => base.ResourceManager;

    public SceneAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Scene, dependencies, assetName)
    {
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        throw new NotSupportedException("Use SceneManager.LoadSceneAsync to load a Scene into a World.");
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        if (typeof(T) == typeof(IAssetEntry) || typeof(T) == typeof(SceneAssetEntry))
        {
            dst = (T)(object)this;
            return;
        }

        throw new NotSupportedException("Use SceneManager.LoadSceneAsync to load a Scene into a World.");
    }

    protected override void OnReleaseResource()
    {
        _sceneData = null;
    }

    public Result OnLoadContent([Owner] ref AssetPayload payload)
    {
        try
        {
            if (payload.Size < sizeof(SceneContentHeader))
            {
                return Result.Failure("Scene payload is too small to contain a header.");
            }

            var header = payload.Read<SceneContentHeader>(0);
            if (header.magic != SceneContentHeader.MAGIC)
            {
                return Result.Failure($"Unexpected scene header magic: 0x{header.magic:X8}.");
            }

            if (header.version != SceneContentHeader.VERSION)
            {
                return Result.Failure($"Unsupported scene header version {header.version}.");
            }

            if (!ValidateCounts(in header, payload.Size))
            {
                return Result.Failure("Scene payload header offsets or counts exceed payload bounds.");
            }

            var data = new LoadedSceneData
            {
                Header = header,
                Strings = new string[header.stringCount],
                StringComponentIDs = new Identifier<IComponent>[header.stringCount],
                StringVolumeTypeIDs = new ushort[header.stringCount],
                Entities = ReadArray<SceneEntityHeader>(ref payload, header.entitiesOffset, header.entityCount),
                PrefabInstances = ReadArray<ScenePrefabSpawnHeader>(ref payload, header.prefabInstancesOffset, header.prefabInstanceCount),
                Components = ReadArray<SceneComponentHeader>(ref payload, header.componentsOffset, header.componentCount),
                Fields = ReadArray<SceneFieldRecord>(ref payload, header.fieldsOffset, header.fieldCount),
                VolumeOverrides = ReadArray<SceneVolumeOverrideHeader>(ref payload, header.volumeOverridesOffset, header.volumeOverrideCount),
                MaterialDependencyIndices = ReadArray<int>(ref payload, header.materialDependencyIndicesOffset, header.materialDependencyIndexCount),
                NameOverrides = ReadArray<ScenePrefabNameOverrideHeader>(ref payload, header.nameOverridesOffset, header.nameOverrideCount),
                AddedComponents = ReadArray<SceneAddedComponentHeader>(ref payload, header.addedComponentsOffset, header.addedComponentCount),
                RemovedComponents = ReadArray<SceneRemovedComponentHeader>(ref payload, header.removedComponentsOffset, header.removedComponentCount),
                PropertyOverrides = ReadArray<ScenePropertyOverrideHeader>(ref payload, header.propertyOverridesOffset, header.propertyOverrideCount),
                DataBlob = header.dataBlobSize > 0 ? payload.Slice(header.dataBlobOffset, header.dataBlobSize).ToArray() : Array.Empty<byte>()
            };

            var curOffset = header.stringsOffset;
            for (var i = 0; i < header.stringCount; i++)
            {
                if (curOffset + sizeof(ushort) > payload.Size)
                {
                    return Result.Failure("Scene string table extends past payload size.");
                }

                var len = payload.Read<ushort>(curOffset);
                curOffset += sizeof(ushort);
                if (curOffset + len > payload.Size)
                {
                    return Result.Failure("Scene string entry extends past payload size.");
                }

                var typeName = len == 0 ? string.Empty : Encoding.UTF8.GetString(payload.Slice(curOffset, len));
                curOffset += len;

                data.Strings[i] = typeName;
                data.StringComponentIDs[i] = ComponentRegistry.GetComponentIDByName(typeName);
                data.StringVolumeTypeIDs[i] = VolumeRegistry.TryGetBlenderByName(typeName, out var volReg) ? volReg.typeId : ushort.MaxValue;
            }

            _sceneData = data;
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to parse scene payload: {ex.Message}");
        }
        finally
        {
            payload.Dispose();
        }
    }

    private static bool ValidateCounts(in SceneContentHeader header, long payloadSize)
    {
        return header.stringCount >= 0
            && header.stringsOffset >= sizeof(SceneContentHeader)
            && CheckRange<SceneEntityHeader>(header.entitiesOffset, header.entityCount, payloadSize)
            && CheckRange<ScenePrefabSpawnHeader>(header.prefabInstancesOffset, header.prefabInstanceCount, payloadSize)
            && CheckRange<SceneComponentHeader>(header.componentsOffset, header.componentCount, payloadSize)
            && CheckRange<SceneFieldRecord>(header.fieldsOffset, header.fieldCount, payloadSize)
            && CheckRange<SceneVolumeOverrideHeader>(header.volumeOverridesOffset, header.volumeOverrideCount, payloadSize)
            && CheckRange<int>(header.materialDependencyIndicesOffset, header.materialDependencyIndexCount, payloadSize)
            && CheckRange<ScenePrefabNameOverrideHeader>(header.nameOverridesOffset, header.nameOverrideCount, payloadSize)
            && CheckRange<SceneAddedComponentHeader>(header.addedComponentsOffset, header.addedComponentCount, payloadSize)
            && CheckRange<SceneRemovedComponentHeader>(header.removedComponentsOffset, header.removedComponentCount, payloadSize)
            && CheckRange<ScenePropertyOverrideHeader>(header.propertyOverridesOffset, header.propertyOverrideCount, payloadSize)
            && CheckByteRange(header.dataBlobOffset, header.dataBlobSize, payloadSize);
    }

    private static bool CheckRange<T>(long offset, int count, long payloadSize)
        where T : unmanaged
    {
        if (count < 0 || offset < 0)
        {
            return false;
        }

        if (count == 0)
        {
            return offset <= payloadSize;
        }

        return offset + (long)count * sizeof(T) <= payloadSize;
    }

    private static bool CheckByteRange(long offset, int count, long payloadSize)
    {
        if (count < 0 || offset < 0)
        {
            return false;
        }

        return offset + count <= payloadSize;
    }

    private static T[] ReadArray<T>(ref AssetPayload payload, long offset, int count)
        where T : unmanaged
    {
        if (count <= 0)
        {
            return Array.Empty<T>();
        }

        var result = new T[count];
        payload.Slice(offset, count * sizeof(T)).CopyTo(MemoryMarshal.AsBytes(result.AsSpan()));
        return result;
    }
}
