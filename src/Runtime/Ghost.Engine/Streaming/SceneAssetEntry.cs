using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal static class SceneLoader
{
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

internal unsafe class SceneAssetEntry : AssetEntry
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
        // For example if we have a component called SceneStreamer{ SceneID a; SceneID b; }
        // In save data, we convert the SceneID(ushort) to a asset gui, and convert it back during load. So at ResolveScene stage (before the file even been loaded), we need to call the SceneManager.CreateScene() and return the id immediately.
        // Currently we store the world and loading type directly inside the asset entry, but actually that should not be bound with the asset itself, because we may load scene A along at the first time, then we load it additively at the second time.
        // So, maybe the scene asset entry should only create a unique id from SceneManager.CreateScene() then resolve the scene file without loading it into world.
        // Then we can load the scene into world using our job system, and user can decide to wait it immediatly (sync) or fire-and-forget (async).
        // The workflow may be this:
        // 1. Startup scene load, during resolve, see SceneStreamer has two SceneID fields (which still contains guid now), resolve this two scene via AssetManager. Get the id of those two scene immediately.
        // 2. Background job load the scene into memory, parse the raw memory into the format that runtime understand. (Or maybe we do not load full memory, just check the header to see if it's valid?
        //    If the streamer type has 20 scenes, loading all 20 scenes into memory is very huge.).
        // 3. The streamer called SceneManager.LoadScene(World, SceneID, SceneLoadingType) (example api, may not be this exactly). (Mybe we load the data into memory here every time when LoadScene is
        //    called? It will be fine right since load scene itself is a heavy opeartion and it's not am opeartion that will be performed per frame)
        // 4. Background job load the scene into world by creating entities and setup components for those entities.

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

    public Result<JobHandle> OnProcessing(object? context)
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
