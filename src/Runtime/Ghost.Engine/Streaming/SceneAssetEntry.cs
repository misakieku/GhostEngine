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

        public PendingSceneLoad pendingSceneLoad;

        public readonly void Execute(ref readonly JobExecutionContext context)
        {
            try
            {
                if (pendingSceneLoad.Status == SceneLoadStatus.Canceled)
                {
                    pendingSceneLoad.Dispose();
                    return;
                }

                pendingSceneLoad.SetStatus(SceneLoadStatus.Parsing);

                var loadResult = SceneManager.ParseSceneData(header, stream, AllocationHandle.Persistent);
                if (loadResult.IsFailure)
                {
                    pendingSceneLoad.Fail(loadResult.Message ?? "Failed to parse scene data.");
                    return;
                }

                if (pendingSceneLoad.Status == SceneLoadStatus.Canceled)
                {
                    loadResult.Value.Dispose();
                    pendingSceneLoad.Dispose();
                    return;
                }

                pendingSceneLoad.CompleteParsing(loadResult.Value);
                if (pendingSceneLoad.Options.AutoMaterialize)
                {
                    SceneManager.EnqueuePendingScene(pendingSceneLoad);
                }
            }
            catch (Exception ex)
            {
                pendingSceneLoad.Fail(ex.Message);
            }
            finally
            {
                stream.Dispose();
            }
        }
    }

    public unsafe Result<SceneLoadOperation> LoadScene(World world, AssetRef<Scene> sceneAsset, SceneLoadingType loadingType, SceneLoadOptions options = default)
    {
        if (!sceneAsset.IsValid)
        {
            return Result.Failure("Invalid scene asset.");
        }

        var openResult = _contentProvider.OpenReadAsync(sceneAsset.ID);
        if (openResult.IsFailure)
        {
            return Result.Failure($"Failed to open scene {sceneAsset.ID}: {openResult.Message}.");
        }

        var data = openResult.Value;

        if (data.stream.Length < sizeof(SceneContentHeader))
        {
            data.Dispose();
            return Result.Failure("Invalid scene file size.");
        }

        var header = data.stream.Read<SceneContentHeader>();
        if (header.magic != SceneContentHeader.MAGIC)
        {
            data.Dispose();
            return Result.Failure("Unexpected header format.");
        }

        if (header.version != SceneContentHeader.VERSION)
        {
            data.Dispose();
            return Result.Failure($"Not supported scene header version {header.version}.");
        }

        try
        {
            var entry = GetOrCreateEntry(sceneAsset.ID); // Purely to get the dependencies and ensure the asset is tracked, the actual loading is done in the job.
            var pendingSceneLoad = new PendingSceneLoad(world, sceneAsset, loadingType, options, entry);
            pendingSceneLoad.SetStatus(SceneLoadStatus.WaitingForDependencies);

            var job = new LoadSceneJob
            {
                header = header,
                stream = data.stream,
                pendingSceneLoad = pendingSceneLoad
            };

            _jobScheduler.Schedule(in job, entry.LoadJobHandle);
            return new SceneLoadOperation(pendingSceneLoad);
        }
        catch (Exception ex)
        {
            data.Dispose();
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

    public override void ReadAssetData(Span<byte> dst)
    {
        // Should we write anything here?
        throw new NotImplementedException();
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        throw new NotImplementedException();
    }
}
