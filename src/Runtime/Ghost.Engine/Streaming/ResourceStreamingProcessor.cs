using Ghost.Core;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;

namespace Ghost.Engine.Streaming;

internal class ResourceStreamingProcessor : IResourceStreamingProcessor
{
    private const int MAX_UPLOADS_PER_FRAME = 8;

    private readonly ConcurrentQueue<ProcessableAssetEntry> _pendingProcess;
    private readonly ConcurrentQueue<UploadableAssetEntry> _pendingUpload;
    private readonly ConcurrentQueue<UploadableAssetEntry> _pendingFinalize;

    private ulong _pendingCopyFenceValue;

    public ResourceStreamingProcessor()
    {
        _pendingProcess = new ConcurrentQueue<ProcessableAssetEntry>();
        _pendingUpload = new ConcurrentQueue<UploadableAssetEntry>();
        _pendingFinalize = new ConcurrentQueue<UploadableAssetEntry>();
        _pendingCopyFenceValue = 0;
    }

    public bool EnqueueForProcess(AssetEntry entry)
    {
        if (entry is UploadableAssetEntry uploadable)
        {
            _pendingUpload.Enqueue(uploadable);
            return true;
        }
        else if (entry is ProcessableAssetEntry processable)
        {
            _pendingProcess.Enqueue(processable);
            return true;
        }

        return false;
    }

    public void ProcessPendingResource(JobScheduler jobScheduler, object? context)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var handles = new UnsafeList<JobHandle>(_pendingProcess.Count, scope.AllocationHandle);

        while (_pendingProcess.TryDequeue(out var entry))
        {
            var result = entry.OnProcessing(context);
            if (result.IsFailure)
            {
                Logger.Error(result.Message);
                continue;
            }

            var handle = result.Value;
            if (!handle.IsValid)
            {
                continue;
            }

            handles.Add(handle);
        }

        jobScheduler.WaitAll(handles);
    }

    public void ProcessPendingUploads(ResourceStreamingContext context)
    {
        // 1. If there's a pending copy batch from last frame, check its fence
        if (_pendingCopyFenceValue > 0 && context.CopyPipeline.CurrentFenceValue() >= _pendingCopyFenceValue)
        {
            while (_pendingFinalize.TryDequeue(out var item))
            {
                Volatile.Write(ref item.StateValue, (int)AssetState.Ready);
                if (Interlocked.CompareExchange(ref item.PendingReimport, false, true))
                {
                    item.AssetManager.ReimportAsset(item.AssetId);  // re-queue
                }

                item.OnUploadComplete(context);
            }

            _pendingCopyFenceValue = 0;
        }

        if (_pendingCopyFenceValue > 0)
        {
            return;
        }

        // 2. Collect entries that are in state == Loaded (I/O done, not yet uploaded)
        //    Cap per frame to avoid stalling (e.g., max 8 textures per frame)
        if (_pendingUpload.IsEmpty)
        {
            return;
        }

        context.CopyPipeline.Begin();

        var uploadCount = 0;
        while (uploadCount < MAX_UPLOADS_PER_FRAME && _pendingUpload.TryDequeue(out var entry))
        {
            if (entry.State != AssetState.Loaded)
            {
                Logger.Warning($"Asset {entry.AssetId} is in state {entry.State}, expected Loaded. Skipping upload.");
                continue;
            }

            // Record copy commands into cmdCopy
            if (entry.OnRecordUploadCommands(context).IsFailure)
            {
                Logger.Error($"Failed to record upload commands for asset {entry.AssetId}. Skipping upload.");
                continue;
            }

            entry.State = AssetState.Processing;

            _pendingFinalize.Enqueue(entry);
            uploadCount++;
        }

        // 3. Submit the batch
        if (context.CopyPipeline.End().IsFailure)
        {
            Logger.Error("Failed to submit copy command list for resource streaming.");
            return;
        }

        if (uploadCount > 0)
        {
            _pendingCopyFenceValue = context.CopyPipeline.SignaledFenceValue();
        }
    }
}
