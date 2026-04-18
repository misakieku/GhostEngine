using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
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
    Uploading = 4,
    Ready = 5,
    Failed = 6,
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
    private unsafe partial class AssetEntry : IDisposable
    {
        public struct __storage
        {
            public fixed byte data[64];
        }

        private readonly AssetManager _assetManager;

        private Guid _assetId;
        private __storage _storage;
        private MemoryBlock _rawData;

        private JobHandle _loadJobHandle;
        private AssetType _assetType;
        private int _refCount;
        private int _state;

        private ResourceManager ResourceManager => _assetManager._resourceManager;
        private IResourceDatabase ResourceDatabase => _assetManager._resourceDatabase;
        private IResourceAllocator ResourceAllocator => _assetManager._resourceAllocator;

        public Guid AssetId => _assetId;
        public MemoryBlock RawData => _rawData;
        public JobHandle LoadJobHandle => _loadJobHandle;
        public AssetType AssetType => _assetType;
        public int RefCount => Volatile.Read(ref _refCount);

        public AssetState State
        {
            get => (AssetState)Volatile.Read(ref _state);
            set => Volatile.Write(ref _state, (int)value);
        }

        public AssetEntry(AssetManager manager, Guid assetId, AssetType assetType)
        {
            _assetManager = manager;

            _assetId = assetId;
            _assetType = assetType;
            _refCount = 1;

            switch (assetType)
            {
                case AssetType.Texture:
                    SetStorage(manager.AllocateTextureHandle());
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStorage<T>(T asset)
            where T : unmanaged
        {
            Unsafe.WriteUnaligned(ref _storage.data[0], asset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetStorage<T>()
            where T : unmanaged
        {
            return Unsafe.ReadUnaligned<T>(ref _storage.data[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRawData([OwnershipTransfer] ref MemoryBlock data)
        {
            _rawData = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLoadJobHandle(JobHandle handle)
        {
            _loadJobHandle = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Release()
        {
            var newRefCount = Interlocked.Decrement(ref _refCount);
            Debug.Assert(newRefCount >= 0, "Reference count should not be negative");

            if (newRefCount == 0)
            {
                Dispose();
            }

            return newRefCount;
        }

        public void OnRecordUploadCommands(ICommandBuffer commandBuffer)
        {
            switch (_assetType)
            {
                case AssetType.Texture:
                    RecordTextureUpload(commandBuffer);
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
                    break;
                default:
                    break;
            }
        }

        public void OnUploadComplete()
        {
            switch (_assetType)
            {
                case AssetType.Texture:
                    OnTextureUploadComplete();
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
                    break;
                default:
                    break;
            }

            Volatile.Write(ref _state, (int)AssetState.Ready);
        }

        public void Dispose()
        {
            var handle = GetStorage<Handle<GPUTexture>>();
            ResourceDatabase.ReleaseResource(handle.AsResource());

            _assetManager.RemoveEntry(_assetId);
        }
    }

    private struct LoadAssetJob : IJob
    {
        public Guid assetID;
        public AssetType assetType;

        private static Result LoadRawData(IContentProvider contentProvider, AssetEntry entry)
        {
            try
            {
                using var stream = contentProvider.OpenRead(entry.AssetId).GetValueOrThrow();

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

                entry.SetRawData(ref data);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

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

            entry.State = AssetState.Loading;

            var result = LoadRawData(assetManager._contentProvider, entry);
            if (result.IsFailure)
            {
                entry.State = AssetState.Failed;
                Logger.Error($"Failed to load asset {assetID}: {result.Message}");
                return;
            }

            entry.State = AssetState.Loaded;
        }
    }

    private const int _MAX_UPLOADS_PER_FRAME = 8;

    private readonly IContentProvider _contentProvider;

    private readonly ResourceManager _resourceManager;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly AsyncCopyPipeline _copyPipeline; // Upload via copy queue.

    private readonly JobScheduler _jobScheduler;
    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;
    private readonly ConcurrentQueue<AssetEntry> _pendingFinalize;

    private ulong _pendingCopyFenceValue;

    // TODO
    private Handle<GPUTexture> _fallbackTexture;
    private Handle<GPUTexture> _fallbackNormalMap;
    private Handle<Mesh> _fallbackMesh;
    private Handle<Material> _fallbackMaterial;

    internal AssetManager(IContentProvider contentProvider, ResourceManager resourceManager, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, AsyncCopyPipeline uploadBatch)
    {
        _contentProvider = contentProvider;
        _resourceManager = resourceManager;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _copyPipeline = uploadBatch;

        var desc = new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount < 8 ? 1 : 2,
            ThreadPriority = ThreadPriority.BelowNormal,
            State = this,
        };

        _jobScheduler = new JobScheduler(in desc);
        _entries = new ConcurrentDictionary<Guid, AssetEntry>();
        _pendingFinalize = new ConcurrentQueue<AssetEntry>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveEntry(Guid guid)
    {
        return _entries.TryRemove(guid, out _);
    }

    private void EnsureScheduled(AssetEntry entry)
    {
        if ((int)entry.State >= (int)AssetState.Scheduled)
        {
            return;
        }

        // Resolve dependencies (in-memory manifest/catalog lookup — instant)
        var deps = _contentProvider.GetDependencies(entry.AssetId);

        var dependency = JobHandle.Invalid;
        if (deps.Length > 0)
        {
            // Avoid stack overflow for deep dependency tree like a whole scene.

            // Stack allocator here is fine, because it use virtual memory and has 32 mb capacity per thread.
            using var scope = AllocationManager.CreateStackScope();

            using var list = new UnsafeList<Guid>(deps.Length * 2, scope.AllocationHandle);
            using var stack = new UnsafeStack<Guid>(deps.Length * 2, scope.AllocationHandle);
            using var visited = new UnsafeHashSet<Guid>(deps.Length * 2, scope.AllocationHandle);

            for (var i = 0; i < deps.Length; i++)
            {
                stack.Push(deps[i]);
            }

            while (stack.TryPop(out var guid))
            {
                if (visited.Contains(guid))
                {
                    continue;
                }

                visited.Add(guid);
                list.Add(guid);

                var depss = _contentProvider.GetDependencies(guid);
                foreach (var d in depss)
                {
                    if (!visited.Contains(d))
                    {
                        stack.Push(d);
                    }
                }
            }

            using var depHandles = new UnsafeList<JobHandle>(list.Count, scope.AllocationHandle);

            // Schedule all dependencies first (depth-first)
            for (var i = list.Count - 1; i >= 0; i--)
            {
                // This should create the entry and schedule the job on those assets does not have any dependency first.
                var handle = GetOrCreateEntry(list[i]).LoadJobHandle;
                Debug.Assert(handle.IsValid);

                depHandles.Add(handle);
            }

            dependency = _jobScheduler.CombineDependencies(depHandles);
        }

        var job = new LoadAssetJob
        {
            assetID = entry.AssetId,
            assetType = entry.AssetType,
        };

        entry.SetLoadJobHandle(_jobScheduler.Schedule(ref job, dependency));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AssetEntry GetOrCreateEntry(Guid guid)
    {
        return _entries.GetOrAdd(guid, static (id, self) =>
        {
            var entry = new AssetEntry(self, id, self._contentProvider.GetAssetType(id))
            {
                State = AssetState.Scheduled
            };

            self.EnsureScheduled(entry);
            return entry;
        }, this);
    }

    // NOTE: Render thread only.
    internal void ProcessPendingUploads()
    {
        // 1. If there's a pending copy batch from last frame, check its fence
        if (_pendingCopyFenceValue > 0 && _copyPipeline.CurrentFenceValue() >= _pendingCopyFenceValue)
        {
            while (_pendingFinalize.TryDequeue(out var item))
            {
                item.OnUploadComplete();
            }

            _pendingCopyFenceValue = 0;
        }

        if (_pendingCopyFenceValue > 0)
        {
            return;
        }

        // 2. Collect entries that are in state == Loaded (I/O done, not yet uploaded)
        //    Cap per frame to avoid stalling (e.g., max 8 textures per frame)
        _copyPipeline.Begin();

        var cmdCopy = _copyPipeline.GetCommandBuffer();
        var uploadCount = 0;

        foreach (var (guid, entry) in _entries)
        {
            if (entry.State != AssetState.Loaded)
            {
                continue;
            }

            if (uploadCount >= _MAX_UPLOADS_PER_FRAME)
            {
                break;
            }

            // Record copy commands into cmdCopy
            entry.OnRecordUploadCommands(cmdCopy);
            entry.State = AssetState.Uploading;

            _pendingFinalize.Enqueue(entry);
            uploadCount++;
        }

        // 3. Submit the batch
        if (uploadCount > 0)
        {
            var result = _copyPipeline.End();
            if (result.IsSuccess)
            {
                _pendingCopyFenceValue = _copyPipeline.SignaledFenceValue();
            }
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
