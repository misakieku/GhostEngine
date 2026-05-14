using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Core;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct SceneContentHeader
{
    public const uint MAGIC = 0x4E435347; // "GSCN" in little-endian
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public int entityCount;
}

public partial class AssetManager
{
    public Result<JobHandle> LoadScene(World world, AssetRef<Scene> sceneAsset, SceneLoadingType loadingType)
    {
        if (!sceneAsset.IsValid)
        {
            return Result.Failure("Invalid scene asset.");
        }

        var openResult = _contentProvider.OpenRead(sceneAsset.ID);
        if (openResult.IsFailure)
        {
            return Result.Failure($"Failed to open scene {sceneAsset.ID}: {openResult.Message}.");
        }

        try
        {
            using var stream = openResult.Value;

            var header = stream.Read<SceneContentHeader>();
            if (header.magic != SceneContentHeader.MAGIC)
            {
                return Result.Failure("Unexpected header format.");
            }

            if (header.version != SceneContentHeader.VERSION)
            {
                return Result.Failure($"Not supported scene header version {header.version}.");
            }

            if (loadingType == SceneLoadingType.Single)
            {
                world.Reset();
            }

            var loadResult = SceneManager.LoadBinarySceneIntoWorld(world, header, stream);
            if (loadResult.IsFailure)
            {
                return Result.Failure(loadResult.Message);
            }

            return JobHandle.Invalid;
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

internal class SceneAssetEntry : AssetEntry
{
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
    }

    public override Result OnLoadContent(Stream contentStream)
    {
        return Result.Success();
    }

    public override void OnReleaseResource()
    {
    }
}
