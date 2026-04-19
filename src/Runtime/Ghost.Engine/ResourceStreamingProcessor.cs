using Ghost.Core;
using Ghost.Graphics;
using SharpCompress.Common;
using System.Collections.Concurrent;

namespace Ghost.Engine;

internal class ResourceStreamingProcessor : IResourceStreamingProcessor
{
    private const int _MAX_UPLOADS_PER_FRAME = 8;

    private readonly ConcurrentQueue<AssetEntry> _pendingUpload;
    private readonly ConcurrentQueue<AssetEntry> _pendingFinalize;

    private ulong _pendingCopyFenceValue;

    public ResourceStreamingProcessor()
    {
        _pendingUpload = new ConcurrentQueue<AssetEntry>();
        _pendingFinalize = new ConcurrentQueue<AssetEntry>();
        _pendingCopyFenceValue = 0;
    }

    public void EnqueueForUpload(AssetEntry entry)
    {
        _pendingUpload.Enqueue(entry);
    }

    public void ProcessPendingUploads(ResourceStreamingContext context)
    {
        // 1. If there's a pending copy batch from last frame, check its fence
        if (_pendingCopyFenceValue > 0 && context.CopyPipeline.CurrentFenceValue() >= _pendingCopyFenceValue)
        {
            while (_pendingFinalize.TryDequeue(out var item))
            {
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
        while (uploadCount < _MAX_UPLOADS_PER_FRAME && _pendingUpload.TryDequeue(out var entry))
        {
            if (entry.State != AssetState.Loaded)
            {
                Logger.Warning($"Asset {entry.AssetId} is in state {entry.State}, expected Loaded. Skipping upload.");
                continue;
            }

            // Record copy commands into cmdCopy
            entry.OnRecordUploadCommands(context);
            entry.State = AssetState.Uploading;

            _pendingFinalize.Enqueue(entry);
            uploadCount++;
        }

        var result = context.CopyPipeline.End();

        // 3. Submit the batch
        if (uploadCount > 0 && result.IsSuccess)
        {
            _pendingCopyFenceValue = context.CopyPipeline.SignaledFenceValue();
        }
    }
}
