using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using System.Text;

namespace Ghost.Engine.Streaming;

internal sealed class LoadedPrefabData
{
    public PrefabContentHeader Header;
    public Identifier<IComponent>[] ComponentTypes = Array.Empty<Identifier<IComponent>>();
    public LoadedPrefabEntity[] Entities = Array.Empty<LoadedPrefabEntity>();
    public PrefabMeshMaterialFixup[] MeshMaterialFixups = Array.Empty<PrefabMeshMaterialFixup>();
}

internal struct LoadedPrefabEntity
{
    public PrefabEntityHeader Header;
    public Identifier<IComponent>[] ComponentIDs;
    public byte[][] ComponentData;
}

internal unsafe class PrefabAssetEntry : AssetEntry, ILoadableAssetEntry
{
    private LoadedPrefabData? _prefabData;

    internal LoadedPrefabData? PrefabData => _prefabData;
    internal new ResourceManager ResourceManager => base.ResourceManager;

    public PrefabAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.EntityPrefab, dependencies, assetName)
    {
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        throw new NotSupportedException("Use PrefabUtility.Instantiate to instantiate an EntityPrefab.");
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        if (typeof(T) == typeof(IAssetEntry) || typeof(T) == typeof(PrefabAssetEntry))
        {
            dst = (T)(object)this;
            return;
        }

        throw new NotSupportedException("Use PrefabUtility.Instantiate to instantiate an EntityPrefab.");
    }

    protected override void OnReleaseResource()
    {
        _prefabData = null;
    }

    public Result OnLoadContent([Owner] ref AssetPayload payload)
    {
        try
        {
            if (payload.Size < sizeof(PrefabContentHeader))
            {
                return Result.Failure("Prefab payload is too small to contain a header.");
            }

            var header = payload.Read<PrefabContentHeader>(0);
            if (header.magic != PrefabContentHeader.MAGIC)
            {
                return Result.Failure($"Unexpected prefab header magic: 0x{header.magic:X8}.");
            }

            if (header.version != PrefabContentHeader.VERSION)
            {
                return Result.Failure($"Unsupported prefab header version {header.version}.");
            }

            var loadedData = new LoadedPrefabData
            {
                Header = header,
                ComponentTypes = new Identifier<IComponent>[header.componentTypeCount],
                Entities = new LoadedPrefabEntity[header.entityCount],
                MeshMaterialFixups = new PrefabMeshMaterialFixup[header.meshMaterialFixupCount]
            };

            var curOffset = header.componentTypesOffset;
            for (var i = 0; i < header.componentTypeCount; i++)
            {
                var len = payload.Read<ushort>(curOffset);
                curOffset += sizeof(ushort);

                var strSpan = payload.Slice(curOffset, len);
                var typeName = Encoding.UTF8.GetString(strSpan);
                curOffset += len;

                var compId = ResolveComponentId(typeName);
                loadedData.ComponentTypes[i] = compId;
            }

            curOffset = header.entitiesOffset;
            for (var i = 0; i < header.entityCount; i++)
            {
                var eHeader = payload.Read<PrefabEntityHeader>(curOffset);
                curOffset += sizeof(PrefabEntityHeader);

                var entity = new LoadedPrefabEntity
                {
                    Header = eHeader,
                    ComponentIDs = new Identifier<IComponent>[eHeader.componentCount],
                    ComponentData = new byte[eHeader.componentCount][]
                };

                for (var c = 0; c < eHeader.componentCount; c++)
                {
                    var cHeader = payload.Read<PrefabComponentHeader>(curOffset);
                    curOffset += sizeof(PrefabComponentHeader);

                    entity.ComponentIDs[c] = loadedData.ComponentTypes[cHeader.componentTypeIndex];
                    var data = new byte[cHeader.dataSize];
                    payload.Slice(curOffset, cHeader.dataSize).CopyTo(data);
                    curOffset += cHeader.dataSize;
                    entity.ComponentData[c] = data;
                }

                loadedData.Entities[i] = entity;
            }

            for (var i = 0; i < header.meshMaterialFixupCount; i++)
            {
                var fixupOffset = header.meshMaterialFixupsOffset + i * sizeof(PrefabMeshMaterialFixup);
                loadedData.MeshMaterialFixups[i] = payload.Read<PrefabMeshMaterialFixup>(fixupOffset);
            }

            _prefabData = loadedData;
            return Result.Success();
        }
        finally
        {
            payload.Dispose();
        }
    }

    private static Identifier<IComponent> ResolveComponentId(string fullName)
    {
        var id = ComponentRegistry.GetComponentIDByName(fullName);
        if (id.IsInvalid)
        {
            Logger.Error($"Failed to resolve component ID for '{fullName}'. Make sure the component is registered in ComponentRegistry.");
        }

        return id;
    }
}
