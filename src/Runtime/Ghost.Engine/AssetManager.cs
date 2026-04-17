using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.AssetLoader;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.Buffer;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine;

public enum AssetState : byte
{
    Unloaded = 0,
    Scheduled = 1,
    Loading = 2,
    Loaded = 3,
    Ready = 4,
    Failed = 5,
}

public enum AssetType : byte
{
    Texture = 0,
    Mesh = 1,
    Material = 2,
    Audio = 3,
    Scene = 4,
    Video = 5,
    Json = 6,

    Unknown = 255,
}

internal interface IContentProvider
{
    bool HasAsset(Guid guid);

    Result<Stream> OpenRead(Guid guid, CancellationToken token = default);

    Guid[] GetDependencies(Guid guid);

    AssetType GetAssetType(Guid guid);
}

// TODO: Support DirectStorage.
public partial class AssetManager : IDisposable
{
    private unsafe class AssetEntry : IDisposable
    {
        private static readonly ObjectPool<AssetEntry> s_pool = new ObjectPool<AssetEntry>(() => new AssetEntry(), (entry) => entry.Reset());

        public struct __storage
        {
            public fixed byte data[64];
        }

        public Guid assetId;
        public __storage storage;
        public MemoryBlock rawData;

        public JobHandle loadJobHandle;
        public AssetType assetType;
        public int state;
        public int refCount;

        public static AssetEntry Create()
        {
            return s_pool.Rent();
        }

        private AssetEntry()
        {
        }

        private void Reset()
        {
            assetId = Guid.Empty;
            assetType = AssetType.Unknown;
            storage = default;
            rawData = default;
            state = (int)AssetState.Unloaded;
            refCount = 0;
            loadJobHandle = default;
        }

        public void SetStorage<T>(T asset)
            where T : unmanaged
        {
            Unsafe.WriteUnaligned(ref storage.data[0], asset);
        }

        public T GetStorage<T>()
            where T : unmanaged
        {
            return Unsafe.ReadUnaligned<T>(ref storage.data[0]);
        }

        public void Dispose()
        {
            s_pool.Return(this);
        }
    }

    private struct LoadAssetJob : IJob
    {
        public Guid assetID;
        public AssetType assetType;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            var assetManager = ctx.State as AssetManager;

            Debug.Assert(assetManager is not null);
            Debug.Assert(assetManager._contentProvider.GetAssetType(assetID) == assetType);

            if (!assetManager._entries.TryGetValue(assetID, out var entry))
            {
                Logger.Error($"Asset entry not found for {assetID}");
                return;
            }

            var result = assetManager.LoadRawData(entry);
            if (result.IsFailure)
            {
                Volatile.Write(ref entry.state, (int)AssetState.Failed);
                Logger.Error($"Failed to load asset {assetID}: {result.Message}");
                return;
            }

            Volatile.Write(ref entry.state, (int)AssetState.Loaded);

            // Ensure the buffer inside the resource database does not move.
            assetManager._resourceDatabase.EnterParallelRead();

            try
            {
                switch (assetType)
                {
                    case AssetType.Texture:
                        result = assetManager.UploadTexture(entry);
                        break;
                    case AssetType.Mesh:
                        break;
                    case AssetType.Material:
                        break;
                    case AssetType.Audio:
                        break;
                    case AssetType.Scene:
                        break;
                    case AssetType.Video:
                        break;
                    case AssetType.Json:
                        break;
                    case AssetType.Unknown:
                    default:
                        break;
                }

                if (result.IsFailure)
                {
                    Logger.Error($"Failed to upload asset {assetID}: {result.Message}");
                    return;
                }
            }
            finally
            {
                assetManager._resourceDatabase.ExitParallelRead();
            }
        }
    }

    private readonly IContentProvider _contentProvider;

    private readonly ResourceManager _resourceManager;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly ResourceUploadBatch _uploadedBatch; // Upload via copy queue.

    private readonly JobScheduler _jobScheduler;
    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;
    private readonly ConcurrentQueue<Guid> _pendingUploads;

    // TODO
    private Handle<GPUTexture> _fallbackTexture;
    private Handle<GPUTexture> _fallbackNormalMap;
    private Handle<Mesh> _fallbackMesh;
    private Handle<Material> _fallbackMaterial;

    internal AssetManager(IContentProvider contentProvider, ResourceManager resourceManager, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, ResourceUploadBatch uploadBatch)
    {
        _contentProvider = contentProvider;
        _resourceManager = resourceManager;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _uploadedBatch = uploadBatch;

        // Ideally we should use a single JobScheduler across the entire engine, and schedule the streaming jobs to that scheduler as low priority background jobs.
        // But how can we get the reference to the AssetManager? Is force job types to be unmanaged a wrong decision? Because we don't have burst compiler at all.
        var threadCount = Environment.ProcessorCount < 8 ? 1 : 2;
        _jobScheduler = new JobScheduler(threadCount, ThreadPriority.BelowNormal, this);
        _entries = new ConcurrentDictionary<Guid, AssetEntry>();
        _pendingUploads = new ConcurrentQueue<Guid>();
    }

    private JobHandle EnsureScheduled(Guid assetID)
    {
        if (_entries.TryGetValue(assetID, out var existing) && existing.state >= (int)AssetState.Scheduled)
        {
            return existing.loadJobHandle;
        }

        // Resolve dependencies (in-memory manifest/catalog lookup — instant)
        var deps = _contentProvider.GetDependencies(assetID);

        // Schedule all dependencies first (recursive, depth-first)
        JobHandle dependency = default;
        if (deps.Length > 0)
        {
            var depHandles = deps.Length <= 8
                ? stackalloc JobHandle[deps.Length]
                : new JobHandle[deps.Length];

            for (int i = 0; i < deps.Length; i++)
            {
                var depEntry = GetOrCreateEntry(deps[i]);
                depHandles[i] = depEntry.loadJobHandle;
            }

            dependency = _jobScheduler.CombineDependencies(depHandles);
        }

        if (_entries.TryGetValue(assetID, out var entry))
        {
            var job = new LoadAssetJob
            {
                assetID = assetID,
                assetType = entry.assetType,
            };

            entry.loadJobHandle = _jobScheduler.Schedule(ref job, dependency);
            return entry.loadJobHandle;
        }

        // This should not happen, because GetOrCreateEntry should have created the entry and scheduled the job.
        Debug.Fail($"Entry for {assetID} should have been created by GetOrCreateEntry");
        return JobHandle.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AssetEntry GetOrCreateEntry(Guid guid)
    {
        return _entries.GetOrAdd(guid, static (id, self) =>
        {
            var entry = AssetEntry.Create();
            entry.assetId = id;
            entry.assetType = self._contentProvider.GetAssetType(id);
            entry.state = (int)AssetState.Scheduled;

            switch (entry.assetType)
            {
                case AssetType.Texture:
                    entry.SetStorage(self.AllocateTextureHandle());
                    break;
                case AssetType.Mesh:
                    break;
                case AssetType.Material:
                    break;
                case AssetType.Audio:
                    break;
                case AssetType.Scene:
                    break;
                case AssetType.Video:
                    break;
                case AssetType.Json:
                    break;
                case AssetType.Unknown:
                default:
                    break;
            }

            entry.loadJobHandle = self.EnsureScheduled(entry.assetId);

            return entry;
        }, this);
    }

    private Result LoadRawData(AssetEntry entry)
    {
        try
        {
            using var stream = _contentProvider.OpenRead(entry.assetId).GetValueOrThrow();

            var data = new MemoryBlock((nuint)stream.Length, MemoryUtility.AlignOf<IntPtr>(), AllocationHandle.Persistent);

            // C# built-in collections use int for indexing, so we need to ensure that the buffer size does not exceed int.MaxValue
            var maxChunkSize = (int)Math.Min(0x7fffffffu, data.Size);
            var offset = 0u;

            while (offset < data.Size)
            {
                using var mem = NativeMemoryManager<byte>.FromMemoryBlock(data, (int)offset, maxChunkSize);
                stream.ReadExactly(mem.Memory.Span);
                offset += (uint)mem.Memory.Length;
            }

            entry.rawData = data;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    // ── Render thread only — single-threaded by design ──
    // TODO: Does this must be called on render thread? Can you just create a dedicated thred or a worker in thread pool for uploading?
    // Also, I think we may don't need this RenderContext at all, because the CommandBuffer is from the ResourceUploadBatch (via async upload), and the ResourceManager/Database/Allocator can be passed in the constructor.
    public void ProcessUploads(RenderContext ctx, int maxPerFrame = 4)
    {
        _uploadedBatch.Begin();

        for (var i = 0; i < maxPerFrame; i++)
        {
            if (!_pendingUploads.TryDequeue(out var guid))
            {
                break;
            }

            if (!_entries.TryGetValue(guid, out var entry) || entry.asset is null)
            {
                continue;
            }

            var error = Error.Success;
            switch (entry.assetType)
            {
                case AssetType.Texture:
                    var textureDesc = new TextureDesc
                    {
                        Width = textureAsset.Width,
                        Height = textureAsset.Height,
                        MipLevels = textureAsset.MipLevels,
                        Slice = 1,
                        Format = GetTextureFormat(textureAsset.Depth, textureAsset.ColorComponents),
                        Dimension = GetTextureDimension(textureAsset.Dimension),
                        Usage = TextureUsage.ShaderResource,
                    };

                    // NOTE: We use Color128 here to avoid that c# span can't hold 16k x 16k x sizeof(float) x 4 textures, because the max span length is int.MaxValue.
                    // Internal method will cast the data to void* so the type does not matter as long as the format and size are correct.
                    var handle = RenderingUtility.CreateTexture(
                        ctx.ResourceManager,
                        ctx.ResourceDatabase,
                        ctx.ResourceAllocator,
                        _uploadedBatch.CommandBuffer,
                        textureAsset.GeData<Color128>(),
                        in textureDesc);

                    textureAsset.SetTextureHandle(handle);
                    break;

                default:
                    error = Error.NotSupported;
                    break;
            }

            if (error.IsSuccess)
            {
                Volatile.Write(ref entry.state, (int)AssetState.Ready);
            }
            else
            {
                _pendingUploads.Enqueue(guid); // retry next frame
            }
        }

        _uploadedBatch.End();

        // TODO: Do we need to wait?
        // await _uploadedBatch.WaitAsync(); // WaitIdle();
    }

    /// <summary>
    /// Blocking load. Returns when the asset reaches at least Loaded state.
    /// GPU upload still happens via ProcessUploads on the render thread.
    /// Use for loading screens or synchronous initialization.
    /// </summary>
    public async ValueTask<T?> LoadAsync<T>(AssetRef<T> assetRef, CancellationToken token = default)
        where T : Asset
    {
        if (!assetRef.IsValid)
        {
            return null;
        }

        var entry = _entries.GetOrAdd(assetRef.guid, static (guid, self) =>
        {
            var e = new AssetEntryOld { assetId = guid, state = (int)AssetState.Loading };
            e.loadTask = Task.Run(() => self.ExecuteLoadAsync(e));
            return e;
        }, this);

        if (Volatile.Read(ref entry.state) >= (int)AssetState.Loaded)
        {
            return entry.asset as T;
        }

        var loadTask = entry.loadTask;
        if (loadTask is not null)
        {
            await loadTask.WaitAsync(token).ConfigureAwait(false);
        }

        return _entries.TryGetValue(assetRef.guid, out var e) ? e.asset as T : null;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
