using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;

namespace Ghost.Engine.Streaming;

internal class ResourceStreamingProcessor : IResourceStreamingProcessor
{
    private const int MAX_UPLOADS_PER_FRAME = 8;

    private readonly ConcurrentQueue<IProcessableAssetEntry> _pendingProcess;
    private readonly ConcurrentQueue<IUploadableAssetEntry> _pendingUpload;
    private readonly ConcurrentQueue<IUploadableAssetEntry> _pendingFinalize;
    private readonly List<IUploadableAssetEntry> _recordedUploads;

    private SubmissionHandle _pendingCopySubmission;

    public ResourceStreamingProcessor()
    {
        _pendingProcess = new ConcurrentQueue<IProcessableAssetEntry>();
        _pendingUpload = new ConcurrentQueue<IUploadableAssetEntry>();
        _pendingFinalize = new ConcurrentQueue<IUploadableAssetEntry>();
        _recordedUploads = new List<IUploadableAssetEntry>(MAX_UPLOADS_PER_FRAME);
        _pendingCopySubmission = default;
    }

    public bool EnqueueForProcess(AssetEntry entry)
    {
        if (entry is IUploadableAssetEntry uploadable)
        {
            _pendingUpload.Enqueue(uploadable);
            return true;
        }
        else if (entry is IProcessableAssetEntry processable)
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
            var result = entry.OnProcessing();
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
        // 1. If there is a pending copy batch from a previous frame, check its opaque completion handle.
        if (_pendingCopySubmission.IsValid && context.FrameScheduler.IsComplete(_pendingCopySubmission))
        {
            while (_pendingFinalize.TryDequeue(out var item))
            {
                item.OnUploadComplete(context);
                item.State = AssetState.Ready;
            }

            _pendingCopySubmission = default;
        }

        if (_pendingCopySubmission.IsValid)
        {
            return;
        }

        // 2. Collect entries that are in state == Loaded (I/O done, not yet uploaded).
        //    Cap per frame to avoid stalling (e.g., max 8 textures per frame).
        if (_pendingUpload.IsEmpty)
        {
            return;
        }

        var copyCommandBuffer = context.GraphicsEngine.GetPooledCommandBuffer(CommandBufferType.Copy);
        var submitted = false;

        try
        {
            copyCommandBuffer.Begin(context.CopyCommandAllocator);

            var uploadContext = context;
            uploadContext.CopyCommandBuffer = copyCommandBuffer;

            while (_recordedUploads.Count < MAX_UPLOADS_PER_FRAME && _pendingUpload.TryDequeue(out var entry))
            {
                if (entry.State != AssetState.Loaded)
                {
                    Logger.Warning($"Asset {entry.AssetId} is in state {entry.State}, expected Loaded. Skipping upload.");
                    continue;
                }

                if (entry.OnRecordUploadCommands(uploadContext).IsFailure)
                {
                    Logger.Error($"Failed to record upload commands for asset {entry.AssetId}. Skipping upload.");
                    entry.State = AssetState.Failed;
                    continue;
                }

                _recordedUploads.Add(entry);
            }

            if (copyCommandBuffer.End().IsFailure)
            {
                Logger.Error("Failed to end copy command list for resource streaming.");
                return;
            }

            if (_recordedUploads.Count == 0)
            {
                return;
            }

            var submission = context.FrameScheduler.Submit(copyCommandBuffer);
            submitted = true;

            for (var i = 0; i < _recordedUploads.Count; i++)
            {
                var entry = _recordedUploads[i];
                entry.State = AssetState.Processing;
                _pendingFinalize.Enqueue(entry);
            }

            _recordedUploads.Clear();
            _pendingCopySubmission = submission;
        }
        finally
        {
            if (!submitted)
            {
                for (var i = 0; i < _recordedUploads.Count; i++)
                {
                    _recordedUploads[i].State = AssetState.Failed;
                }

                _recordedUploads.Clear();
                context.GraphicsEngine.ReturnPooledCommandBuffer(copyCommandBuffer);
            }
        }
    }
}
