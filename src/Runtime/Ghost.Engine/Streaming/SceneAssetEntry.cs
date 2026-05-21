using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Core;
using Ghost.Entities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
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
    private struct LoadSceneJob : IJob
    {
        public SceneContentHeader header;
        public Stream stream;

        public LoadedSceneData loadedSceneData;

        public readonly void Execute(ref readonly JobExecutionContext context)
        {
            try
            {
                var loadResult = SceneManager.ParseSceneData(header, stream, AllocationHandle.Persistent);
                if (loadResult.IsFailure)
                {
                    Logger.Error($"Failed to parse scene data: {loadResult.Message}");
                    return;
                }

                loadedSceneData.entities = loadResult.Value.entities;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while loading scene: {ex}");
            }
            finally
            {
                stream.Dispose();
            }
        }
    }

    public unsafe Result<JobHandle> LoadScene(World world, AssetRef<Scene> sceneAsset, SceneLoadingType loadingType, ref LoadedSceneData? loadedSceneData)
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

        var stream = openResult.Value;

        if (stream.Length < sizeof(SceneContentHeader))
        {
            stream.Dispose();
            return Result.Failure("Invalid scene file size.");
        }

        var header = stream.Read<SceneContentHeader>();
        if (header.magic != SceneContentHeader.MAGIC)
        {
            stream.Dispose();
            return Result.Failure("Unexpected header format.");
        }

        if (header.version != SceneContentHeader.VERSION)
        {
            stream.Dispose();
            return Result.Failure($"Not supported scene header version {header.version}.");
        }

        try
        {
            if (loadingType == SceneLoadingType.Single)
            {
                world.Reset();
            }

            loadedSceneData ??= new LoadedSceneData();

            var entry = GetOrCreateEntry(sceneAsset.ID); // Purely to get the dependencies and ensure the asset is tracked, the actual loading is done in the job.

            var job = new LoadSceneJob
            {
                header = header,
                stream = stream,
                loadedSceneData = loadedSceneData
            };

            return _jobScheduler.Schedule(in job, entry.LoadJobHandle);
        }
        catch (Exception ex)
        {
            stream.Dispose();
            return Result.Failure(ex.Message);
        }
    }
}

internal class SceneAssetEntry : AssetEntry
{
    public SceneAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Scene, dependencies)
    {
    }
}