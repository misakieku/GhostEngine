using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal sealed class RenderGraphExecutor
{
    private const int INITIAL_COMMAND_BUFFER_CAPACITY = 8;

    private readonly RenderGraphResourceRegistry _resources;
    private ICommandBuffer?[] _commandBuffers;
    private SubmissionHandle[] _submissionHandles;
    private CommandQueueType[] _commandBufferQueueTypes;
    private int[] _dependencyOffsets;
    private int[] _dependencyCounts;
    private int[] _producerCommandBufferIds;
    private int _commandBufferCount;
    private int _producerCommandBufferIdCount;

    public RenderGraphExecutor(RenderGraphResourceRegistry resources)
    {
        _resources = resources;
        _commandBuffers = new ICommandBuffer?[INITIAL_COMMAND_BUFFER_CAPACITY];
        _submissionHandles = new SubmissionHandle[INITIAL_COMMAND_BUFFER_CAPACITY];
        _commandBufferQueueTypes = new CommandQueueType[INITIAL_COMMAND_BUFFER_CAPACITY];
        _dependencyOffsets = new int[INITIAL_COMMAND_BUFFER_CAPACITY];
        _dependencyCounts = new int[INITIAL_COMMAND_BUFFER_CAPACITY];
        _producerCommandBufferIds = new int[INITIAL_COMMAND_BUFFER_CAPACITY];
    }

    private void SetViewport(RenderGraphContext context, ReadOnlySpan<RenderTargetInfo> color, DepthStencilInfo depthStencil)
    {
        // The compiler should reject invalid render-target configurations.
        // Retain a debug assertion here to protect the executor invariant.
        Logger.DebugAssert(color.Length > 0 || depthStencil.texture.IsValid);

        ViewportDesc viewportDesc = default;
        ScissorRectDesc scissorDesc = default;

        if (depthStencil.texture.IsValid)
        {
            viewportDesc = new ViewportDesc
            {
                X = 0,
                Y = 0,
                Width = _resources.GetResource(depthStencil.texture).resolvedWidth,
                Height = _resources.GetResource(depthStencil.texture).resolvedHeight,
                MinDepth = 0,
                MaxDepth = 1
            };

            scissorDesc = new ScissorRectDesc
            {
                Left = 0,
                Top = 0,
                Right = (uint)viewportDesc.Width,
                Bottom = (uint)viewportDesc.Height
            };
        }
        else if (color[0].texture.IsValid)
        {
            viewportDesc = new ViewportDesc
            {
                X = 0,
                Y = 0,
                Width = _resources.GetResource(color[0].texture).resolvedWidth,
                Height = _resources.GetResource(color[0].texture).resolvedHeight,
                MinDepth = 0,
                MaxDepth = 1
            };

            scissorDesc = new ScissorRectDesc
            {
                Left = 0,
                Top = 0,
                Right = (uint)viewportDesc.Width,
                Bottom = (uint)viewportDesc.Height
            };
        }

        context.SetViewport(viewportDesc);
        context.SetScissorRect(scissorDesc);
    }

    public unsafe Error Execute(
        in RenderGraphExecutionContext executionContext,
        RenderGraphContext context,
        scoped in CompiledGraph graph,
        RGExecutionFlags flags,
        out SubmissionHandle graphicsSubmission,
        out SubmissionHandle computeSubmission)
    {
        Logger.DebugAssert(_commandBufferCount == 0, "Render-graph execution scratch was not cleared after the previous execution.");
        graphicsSubmission = default;
        computeSubmission = default;

        ICommandBuffer? activeCommandBuffer = null;
        var insideNativePass = false;

        try
        {
            activeCommandBuffer = AcquireCommandBuffer(
                executionContext,
                CommandQueueType.Graphics,
                ReadOnlySpan<int>.Empty,
                flags);
            context.BeginNewFrame(activeCommandBuffer);

            var pPassRTDescs = stackalloc PassRenderTargetDesc[8];
            var pRtFormats = stackalloc TextureFormat[8];
            var reader = new SpanReader(graph.commandStream);

            while (reader.RemainingBytes > 0)
            {
                var op = reader.Read<RGExecutionOpType>();

                switch (op)
                {
                    case RGExecutionOpType.IssueBarriers:
                    {
                        var barrierCount = reader.Read<int>();
                        var error = ExecuteBarrierBatch(activeCommandBuffer, barrierCount, ref reader);
                        if (error != Error.None)
                        {
                            RollbackRecording(executionContext.GraphicsEngine, insideNativePass);
                            return error;
                        }
                        break;
                    }

                    case RGExecutionOpType.BeginNativePass:
                    {
                        var nativePassIdx = reader.Read<int>();
                        var nativePass = graph.nativePasses[nativePassIdx];

                        SetViewport(context, nativePass.colorAttachments, nativePass.depthAttachment);

                        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                        {
                            var attachment = nativePass.colorAttachments[i];
                            pPassRTDescs[i] = new PassRenderTargetDesc
                            {
                                Texture = _resources.GetResource(attachment.texture).backingResource.AsTexture(),
                                ClearColor = attachment.clearColor,
                                LoadOp = attachment.loadOp,
                                StoreOp = attachment.storeOp
                            };
                        }

                        var depthDesc = new PassDepthStencilDesc
                        {
                            Texture = nativePass.hasDepthAttachment
                                ? _resources.GetResource(nativePass.depthAttachment.texture).backingResource.AsTexture()
                                : Handle<GPUTexture>.Invalid,
                            ClearDepth = nativePass.depthAttachment.clearDepth,
                            ClearStencil = nativePass.depthAttachment.clearStencil,
                            DepthLoadOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.loadOp
                                : AttachmentLoadOp.NoAccess,
                            DepthStoreOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.storeOp
                                : AttachmentStoreOp.NoAccess,
                            StencilLoadOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.stencilLoadOp
                                : AttachmentLoadOp.NoAccess,
                            StencilStoreOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.stencilStoreOp
                                : AttachmentStoreOp.NoAccess,
                        };

                        activeCommandBuffer.BeginRenderPass(new Span<PassRenderTargetDesc>(pPassRTDescs, nativePass.colorAttachmentCount), in depthDesc);
                        insideNativePass = true;

                        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                        {
                            var attachment = nativePass.colorAttachments[i];
                            var resource = _resources.GetResource(attachment.texture);
                            pRtFormats[i] = resource.rgTextureDesc.format;
                        }

                        var depthFormat = nativePass.hasDepthAttachment
                            ? _resources.GetResource(nativePass.depthAttachment.texture).rgTextureDesc.format
                            : TextureFormat.Unknown;
                        context.SetRenderTargetFormats(new ReadOnlySpan<TextureFormat>(pRtFormats, nativePass.colorAttachmentCount), depthFormat);
                        break;
                    }

                    case RGExecutionOpType.ExecutePass:
                    {
                        var passIdx = reader.Read<int>();
                        graph.passes[passIdx].Execute(context);
                        break;
                    }

                    case RGExecutionOpType.EndNativePass:
                    {
                        activeCommandBuffer.EndRenderPass();
                        insideNativePass = false;
                        break;
                    }

                    case RGExecutionOpType.CommandBufferSyncPoint:
                    {
                        var marker = RGCommandStream.ReadSyncMarker(ref reader);
                        if (insideNativePass)
                        {
                            throw new InvalidOperationException("A command-buffer sync point cannot occur inside a native render pass.");
                        }

                        var error = EndCommandBuffer(activeCommandBuffer);
                        if (error != Error.None)
                        {
                            ReturnAcquiredCommandBuffers(executionContext.GraphicsEngine);
                            return error;
                        }

                        activeCommandBuffer = AcquireCommandBuffer(
                            executionContext,
                            marker.NextCommandBufferType,
                            marker.ProducerCommandBufferIds,
                            flags);
                        context.BeginNewFrame(activeCommandBuffer);
                        break;
                    }

                    default:
                        throw new NotSupportedException($"Unsupported RGExecutionOpType: {op}");
                }
            }

            var finalError = EndCommandBuffer(activeCommandBuffer);
            if (finalError != Error.None)
            {
                ReturnAcquiredCommandBuffers(executionContext.GraphicsEngine);
                return finalError;
            }
        }
        catch (Exception ex)
        {
            RollbackRecording(executionContext.GraphicsEngine, insideNativePass);
            Logger.Error(ex);
            return Error.InternalError;
        }

        return SubmitCommandBuffers(executionContext, out graphicsSubmission, out computeSubmission);
    }

    private ICommandBuffer AcquireCommandBuffer(
        in RenderGraphExecutionContext executionContext,
        CommandQueueType requestedQueueType,
        ReadOnlySpan<int> producerCommandBufferIds,
        RGExecutionFlags flags)
    {
        if (requestedQueueType is not CommandQueueType.Graphics and not CommandQueueType.Compute)
        {
            throw new InvalidOperationException($"RenderGraph execution does not support {requestedQueueType} command-buffer segments.");
        }

        ValidateProducerCommandBufferIds(producerCommandBufferIds, _commandBufferCount);
        EnsureScratchCapacity(_commandBufferCount + 1);
        EnsureProducerIdCapacity(_producerCommandBufferIdCount + producerCommandBufferIds.Length);

        var queueType = flags.HasFlag(RGExecutionFlags.ForceGraphics)
            ? CommandQueueType.Graphics
            : requestedQueueType;
        var commandBufferType = queueType == CommandQueueType.Graphics
            ? CommandBufferType.Graphics
            : CommandBufferType.Compute;
        var commandAllocator = queueType == CommandQueueType.Graphics
            ? executionContext.GraphicsCommandAllocator
            : executionContext.ComputeCommandAllocator;
        var commandBuffer = executionContext.GraphicsEngine.GetPooledCommandBuffer(commandBufferType);
        var commandBufferIndex = _commandBufferCount++;
        _commandBuffers[commandBufferIndex] = commandBuffer;
        _commandBufferQueueTypes[commandBufferIndex] = queueType;
        _dependencyOffsets[commandBufferIndex] = _producerCommandBufferIdCount;
        _dependencyCounts[commandBufferIndex] = producerCommandBufferIds.Length;
        producerCommandBufferIds.CopyTo(_producerCommandBufferIds.AsSpan(_producerCommandBufferIdCount));
        _producerCommandBufferIdCount += producerCommandBufferIds.Length;

        if (commandBuffer.Type != commandBufferType)
        {
            throw new InvalidOperationException($"The {commandBufferType} command-buffer pool returned a {commandBuffer.Type} command buffer.");
        }

        commandBuffer.Begin(commandAllocator);
        return commandBuffer;
    }

    private static void ValidateProducerCommandBufferIds(ReadOnlySpan<int> producerCommandBufferIds, int nextCommandBufferId)
    {
        for (var i = 0; i < producerCommandBufferIds.Length; i++)
        {
            var producerId = producerCommandBufferIds[i];
            if ((uint)producerId >= (uint)nextCommandBufferId)
            {
                throw new InvalidOperationException($"Producer command-buffer ID {producerId} must precede command buffer {nextCommandBufferId}.");
            }

            for (var previous = 0; previous < i; previous++)
            {
                if (producerCommandBufferIds[previous] == producerId)
                {
                    throw new InvalidOperationException($"Producer command-buffer ID {producerId} is duplicated for command buffer {nextCommandBufferId}.");
                }
            }
        }
    }

    private static Error EndCommandBuffer(ICommandBuffer commandBuffer)
    {
        var result = commandBuffer.End();
        if (result.IsSuccess)
        {
            return Error.None;
        }

        Logger.Warning("Failed to end a render-graph command buffer: " + result.Message);
        return Error.InternalError;
    }

    private Error SubmitCommandBuffers(
        in RenderGraphExecutionContext executionContext,
        out SubmissionHandle graphicsSubmission,
        out SubmissionHandle computeSubmission)
    {
        graphicsSubmission = default;
        computeSubmission = default;
        SubmissionTransaction transaction = default;

        try
        {
            transaction = executionContext.FrameScheduler.BeginSubmissionTransaction(
                _commandBufferCount,
                _producerCommandBufferIdCount);
            for (var i = 0; i < _commandBufferCount; i++)
            {
                var commandBuffer = _commandBuffers[i]!;
                var submission = executionContext.FrameScheduler.Submit(commandBuffer);

                // Scheduler ownership transfers when Submit returns.
                _commandBuffers[i] = null;
                _submissionHandles[i] = submission;

                if (!submission.IsValid || submission.QueueType != _commandBufferQueueTypes[i])
                {
                    throw new InvalidOperationException("The frame scheduler returned an invalid submission handle.");
                }

                var dependencyOffset = _dependencyOffsets[i];
                var dependencyCount = _dependencyCounts[i];
                for (var dependencyIndex = 0; dependencyIndex < dependencyCount; dependencyIndex++)
                {
                    var producerId = _producerCommandBufferIds[dependencyOffset + dependencyIndex];
                    if (_commandBufferQueueTypes[producerId] == _commandBufferQueueTypes[i])
                    {
                        continue;
                    }

                    executionContext.FrameScheduler.AddDependency(_submissionHandles[producerId], submission);
                }

                if (submission.QueueType == CommandQueueType.Graphics)
                {
                    graphicsSubmission = submission;
                }
                else if (submission.QueueType == CommandQueueType.Compute)
                {
                    computeSubmission = submission;
                }
            }

            executionContext.FrameScheduler.CommitSubmissionTransaction(transaction);
            transaction = default;
            ClearExecutionScratch();
            return Error.None;
        }
        catch (Exception ex)
        {
            if (transaction.IsValid)
            {
                try
                {
                    executionContext.FrameScheduler.RollbackSubmissionTransaction(transaction);
                }
                catch (Exception rollbackException)
                {
                    Logger.Error(rollbackException);
                }
            }

            ReturnAcquiredCommandBuffers(executionContext.GraphicsEngine);
            graphicsSubmission = default;
            computeSubmission = default;
            Logger.Error(ex);
            return Error.InternalError;
        }
    }

    private void RollbackRecording(IGraphicsEngine graphicsEngine, bool insideNativePass)
    {
        var activeCommandBuffer = _commandBufferCount > 0
            ? _commandBuffers[_commandBufferCount - 1]
            : null;

        if (activeCommandBuffer?.State.IsRecording == true)
        {
            if (insideNativePass)
            {
                try
                {
                    activeCommandBuffer.EndRenderPass();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            try
            {
                _ = activeCommandBuffer.End();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        ReturnAcquiredCommandBuffers(graphicsEngine);
    }

    private void ReturnAcquiredCommandBuffers(IGraphicsEngine graphicsEngine)
    {
        for (var i = 0; i < _commandBufferCount; i++)
        {
            var commandBuffer = _commandBuffers[i];
            if (commandBuffer == null)
            {
                continue;
            }

            try
            {
                graphicsEngine.ReturnPooledCommandBuffer(commandBuffer);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _commandBuffers[i] = null;
            }
        }

        ClearExecutionScratch();
    }

    private void EnsureScratchCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _commandBuffers.Length)
        {
            return;
        }

        var newCapacity = Math.Max(requiredCapacity, _commandBuffers.Length * 2);
        Array.Resize(ref _commandBuffers, newCapacity);
        Array.Resize(ref _submissionHandles, newCapacity);
        Array.Resize(ref _commandBufferQueueTypes, newCapacity);
        Array.Resize(ref _dependencyOffsets, newCapacity);
        Array.Resize(ref _dependencyCounts, newCapacity);
    }

    private void EnsureProducerIdCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _producerCommandBufferIds.Length)
        {
            return;
        }

        var newCapacity = Math.Max(requiredCapacity, _producerCommandBufferIds.Length * 2);
        Array.Resize(ref _producerCommandBufferIds, newCapacity);
    }

    private void ClearExecutionScratch()
    {
        Array.Clear(_commandBuffers, 0, _commandBufferCount);
        Array.Clear(_submissionHandles, 0, _commandBufferCount);
        Array.Clear(_commandBufferQueueTypes, 0, _commandBufferCount);
        Array.Clear(_dependencyOffsets, 0, _commandBufferCount);
        Array.Clear(_dependencyCounts, 0, _commandBufferCount);
        Array.Clear(_producerCommandBufferIds, 0, _producerCommandBufferIdCount);
        _commandBufferCount = 0;
        _producerCommandBufferIdCount = 0;
    }

    private Error ExecuteBarrierBatch(
        ICommandBuffer cmd,
        int barrierCount,
        ref SpanReader reader)
    {
        if (barrierCount <= 0)
        {
            return Error.None;
        }

        const int MaxBatch = 64;
        using var scope = Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.CreateStackScope();
        using var barriers = new UnsafeList<BarrierDesc>(MaxBatch, scope.AllocationHandle);

        void Flush()
        {
            if (barriers.Count > 0)
            {
                cmd.Barrier(barriers);
                barriers.Clear();
            }
        }

        for (var i = 0; i < barrierCount; i++)
        {
            var compiledBarrier = reader.Read<CompiledBarrier>();
            if (compiledBarrier.flags.HasFlag(BarrierFlags.QueueRelease))
            {
                // Phase 3 keeps execution contained on one Graphics command buffer. The acquire
                // record below lowers the full producer-to-consumer transition for this mode.
                continue;
            }

            var resourceHandle = _resources.GetResource(compiledBarrier.resource).backingResource;
            var target = compiledBarrier.targetState;
            var explicitSource = compiledBarrier.flags.HasFlag(BarrierFlags.ExplicitSource);
            var isAliasing = compiledBarrier.aliasingPredecessor.IsValid;
            var force = compiledBarrier.flags.HasFlag(BarrierFlags.Force);

            BarrierDesc desc;
            if (compiledBarrier.resourceType == RGResourceType.Texture)
            {
                desc = explicitSource
                    ? BarrierDesc.TextureExplicit(
                        resourceHandle.AsTexture(),
                        compiledBarrier.sourceState,
                        target,
                        force: force,
                        discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard),
                        isAliasing: isAliasing)
                    : BarrierDesc.Texture(
                        resourceHandle.AsTexture(),
                        target.sync,
                        target.access,
                        target.layout,
                        discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard),
                        isAliasing: isAliasing);
            }
            else
            {
                desc = explicitSource
                    ? BarrierDesc.BufferExplicit(
                        resourceHandle.AsBuffer(),
                        compiledBarrier.sourceState,
                        target,
                        force: force,
                        isAliasing: isAliasing)
                    : BarrierDesc.Buffer(
                        resourceHandle.AsBuffer(),
                        target.sync,
                        target.access,
                        isAliasing: isAliasing);
            }

            if (barriers.Count >= MaxBatch)
            {
                Flush();
            }

            barriers.AddNoResize(desc);
        }

        Flush();

        return Error.None;
    }
}
