using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Studio.Backend;

[StructLayout(LayoutKind.Sequential)]
internal struct ImGuiDrawProperties
{
    public uint vertexBuffer;
    public uint indexBuffer;
    public uint textureId;
    public uint vertexByteOffset;
    public uint indexStartOffset;
    public uint triangleCount;
    public float scaleX;
    public float scaleY;
    public float translateX;
    public float translateY;
    public uint forceOpaqueAlpha;
}

internal sealed unsafe class ImGuiPostRenderCommand : IRenderLoopCommand, IDisposable
{
    private readonly ImGuiDrawSnapshot[] _snapshots;
    private readonly Handle<GPUBuffer>[] _vertexBuffers;
    private readonly Handle<GPUBuffer>[] _indexBuffers;
    private readonly nuint[] _vertexCapacities;
    private readonly nuint[] _indexCapacities;
    private readonly Dictionary<uint, Handle<GPUTexture>> _imguiTextures = new();
    private readonly Dictionary<uint, uint> _imguiBindlessIds = new();

    private IAssetEntry? _shaderEntry;
    private Handle<Shader> _shaderHandle;
    private int _forwardPassIndex = -1;

    private IResourceDatabase? _lastResourceDatabase;
    private bool _disposed;

    public ImGuiPostRenderCommand(uint frameBufferCount)
    {
        _snapshots = new ImGuiDrawSnapshot[frameBufferCount];
        _vertexBuffers = new Handle<GPUBuffer>[frameBufferCount];
        _indexBuffers = new Handle<GPUBuffer>[frameBufferCount];
        _vertexCapacities = new nuint[frameBufferCount];
        _indexCapacities = new nuint[frameBufferCount];

        for (var i = 0; i < (int)frameBufferCount; i++)
        {
            _snapshots[i] = new ImGuiDrawSnapshot();
            _vertexBuffers[i] = Handle<GPUBuffer>.Invalid;
            _indexBuffers[i] = Handle<GPUBuffer>.Invalid;
        }
    }

    public ImGuiDrawSnapshot GetSnapshotSlot(int frameIndex)
    {
        return _snapshots[(int)((uint)frameIndex % (uint)_snapshots.Length)];
    }

    public void AttachShaderAsset(IAssetEntry shaderEntry)
    {
        _shaderEntry = shaderEntry;
    }

    public void Execute(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        _lastResourceDatabase = context.ResourceDatabase;

        var slot = (int)((uint)context.FrameIndex % (uint)_snapshots.Length);
        var snapshot = _snapshots[slot];

        SyncTextures(in context, commandBuffer, snapshot);

        if (!EnsureShaderReady(in context))
        {
            ClearAndPresentEmptySwapChain(in context, commandBuffer);
            return;
        }

        if (!context.SwapChainManager.TryGetSwapChain(0, out var swapChain))
        {
            return;
        }

        try
        {
            var backBuffer = swapChain.GetCurrentBackBuffer();
            var width = swapChain.Width;
            var height = swapChain.Height;

            if (!snapshot.HasData || snapshot.DisplaySize.x <= 0.0f || snapshot.DisplaySize.y <= 0.0f)
            {
                ClearAndPresentSwapChainTexture(commandBuffer, backBuffer);
                return;
            }

            if (!EnsureFrameBuffers(in context, slot, (nuint)snapshot.VertexBytes.Length, (nuint)(snapshot.Indices.Length * sizeof(ushort))))
            {
                ClearAndPresentSwapChainTexture(commandBuffer, backBuffer);
                return;
            }

            UploadFrameData(in context, slot, snapshot.VertexBytes, snapshot.Indices);

            var vbIndex = context.ResourceDatabase.GetBindlessIndex(_vertexBuffers[slot].AsResource(), BindlessAccess.ShaderResource);
            var ibIndex = context.ResourceDatabase.GetBindlessIndex(_indexBuffers[slot].AsResource(), BindlessAccess.ShaderResource);

            var backBufferDesc = context.ResourceDatabase.GetResourceDescription(backBuffer.AsResource());
            var rtvFormat = backBufferDesc.Error.IsSuccess ? backBufferDesc.Value.TextureDescriptor.Format : TextureFormat.B8G8R8A8_UNorm;

            var externalTex = snapshot.ExternalTexture;
            var hasExternalTex = externalTex.IsValid && context.ResourceDatabase.GetResourceDescription(externalTex.AsResource()).Error.IsSuccess;

            if (hasExternalTex)
            {
                Span<BarrierDesc> enterBarriers = stackalloc BarrierDesc[2];
                enterBarriers[0] = BarrierDesc.Texture(backBuffer, BarrierSync.None, BarrierSync.RenderTarget, BarrierAccess.NoAccess, BarrierAccess.RenderTarget, BarrierLayout.Present, BarrierLayout.RenderTarget);
                enterBarriers[1] = BarrierDesc.Texture(externalTex, BarrierSync.None, BarrierSync.PixelShading, BarrierAccess.NoAccess, BarrierAccess.ShaderResource, BarrierLayout.Common, BarrierLayout.ShaderResource);
                commandBuffer.Barrier(enterBarriers);
            }
            else
            {
                commandBuffer.Barrier(BarrierDesc.Texture(backBuffer, BarrierSync.None, BarrierSync.RenderTarget, BarrierAccess.NoAccess, BarrierAccess.RenderTarget, BarrierLayout.Present, BarrierLayout.RenderTarget));
            }

            var rtDesc = new PassRenderTargetDesc
            {
                Texture = backBuffer,
                ClearColor = new Color128(0.08f, 0.08f, 0.10f, 1.0f),
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
            };

            var depthDesc = new PassDepthStencilDesc
            {
                Texture = Handle<GPUTexture>.Invalid,
            };

            Span<PassRenderTargetDesc> rtDescs = stackalloc PassRenderTargetDesc[1];
            rtDescs[0] = rtDesc;
            commandBuffer.BeginRenderPass(rtDescs, in depthDesc);

            commandBuffer.SetViewport(new ViewportDesc
            {
                X = 0.0f,
                Y = 0.0f,
                Width = width,
                Height = height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            });

            Span<TextureFormat> rtvFormats = stackalloc TextureFormat[1];
            rtvFormats[0] = rtvFormat;
            if (context.TrySetActiveGraphicsShaderPass(commandBuffer, _shaderHandle, _forwardPassIndex, rtvFormats))
            {
                var scaleX = 2.0f / snapshot.DisplaySize.x;
                var scaleY = -2.0f / snapshot.DisplaySize.y;
                var translateX = -1.0f - snapshot.DisplayPos.x * scaleX;
                var translateY = 1.0f - snapshot.DisplayPos.y * scaleY;

                var commands = snapshot.Commands;
                for (var i = 0; i < commands.Length; i++)
                {
                    ref readonly var cmd = ref commands[i];
                    var triCount = cmd.elemCount / 3u;
                    if (triCount == 0)
                    {
                        continue;
                    }

                    var left = (int)MathF.Floor(cmd.clipRect.x);
                    var top = (int)MathF.Floor(cmd.clipRect.y);
                    var right = (int)MathF.Ceiling(cmd.clipRect.z);
                    var bottom = (int)MathF.Ceiling(cmd.clipRect.w);

                    left = Math.Clamp(left, 0, (int)width);
                    top = Math.Clamp(top, 0, (int)height);
                    right = Math.Clamp(right, 0, (int)width);
                    bottom = Math.Clamp(bottom, 0, (int)height);

                    if (right <= left || bottom <= top)
                    {
                        continue;
                    }

                    if (!TryResolveBindlessTexture(cmd.textureId, out var bindlessTexId))
                    {
                        continue;
                    }

                    commandBuffer.SetScissorRect(new RectDesc
                    {
                        Left = (uint)left,
                        Top = (uint)top,
                        Right = (uint)right,
                        Bottom = (uint)bottom,
                    });

                    var props = new ImGuiDrawProperties
                    {
                        vertexBuffer = vbIndex,
                        indexBuffer = ibIndex,
                        textureId = bindlessTexId,
                        vertexByteOffset = cmd.vertexByteOffset,
                        indexStartOffset = cmd.indexStartOffset,
                        triangleCount = triCount,
                        scaleX = scaleX,
                        scaleY = scaleY,
                        translateX = translateX,
                        translateY = translateY,
                        forceOpaqueAlpha = (cmd.textureId & ImGuiContextManager.IMGUI_MANAGED_TEX_BIT) == 0 ? 1u : 0u,
                    };

                    context.SetPassProperties(commandBuffer, in props);
                    commandBuffer.DispatchMesh((triCount + 31u) / 32u, 1u, 1u);
                }
            }

            commandBuffer.EndRenderPass();
            if (hasExternalTex)
            {
                Span<BarrierDesc> exitBarriers = stackalloc BarrierDesc[2];
                exitBarriers[0] = BarrierDesc.Texture(backBuffer, BarrierSync.RenderTarget, BarrierSync.None, BarrierAccess.RenderTarget, BarrierAccess.NoAccess, BarrierLayout.RenderTarget, BarrierLayout.Present);
                exitBarriers[1] = BarrierDesc.Texture(externalTex, BarrierSync.PixelShading, BarrierSync.None, BarrierAccess.ShaderResource, BarrierAccess.NoAccess, BarrierLayout.ShaderResource, BarrierLayout.Common);
                commandBuffer.Barrier(exitBarriers);
            }
            else
            {
                commandBuffer.Barrier(BarrierDesc.Texture(backBuffer, BarrierSync.RenderTarget, BarrierSync.None, BarrierAccess.RenderTarget, BarrierAccess.NoAccess, BarrierLayout.RenderTarget, BarrierLayout.Present));
            }
        }
        finally
        {
            context.SwapChainManager.ReleaseSwapChain(0);
        }
    }

    private bool TryResolveBindlessTexture(uint textureId, out uint bindlessIndex)
    {
        if ((textureId & ImGuiContextManager.IMGUI_MANAGED_TEX_BIT) != 0)
        {
            return _imguiBindlessIds.TryGetValue(textureId, out bindlessIndex);
        }

        bindlessIndex = textureId;
        return bindlessIndex != 0;
    }

    private void SyncTextures(scoped in RenderContext context, ICommandBuffer commandBuffer, ImGuiDrawSnapshot snapshot)
    {
        var ops = snapshot.TextureOps;
        if (ops.IsEmpty)
        {
            return;
        }

        var pixelBytes = snapshot.TexturePixelBytes;
        for (var i = 0; i < ops.Length; i++)
        {
            ref readonly var op = ref ops[i];
            if (op.isDestroy != 0)
            {
                if (_imguiTextures.Remove(op.textureKey, out var oldHandle) && !oldHandle.IsInvalid)
                {
                    context.ResourceDatabase.ReleaseResource(oldHandle.AsResource());
                }

                _imguiBindlessIds.Remove(op.textureKey);
                continue;
            }

            if (_imguiTextures.TryGetValue(op.textureKey, out var existingHandle) && !existingHandle.IsInvalid)
            {
                context.ResourceDatabase.ReleaseResource(existingHandle.AsResource());
            }

            var texDesc = new TextureDesc
            {
                Dimension = TextureDimension.Texture2D,
                Width = op.width,
                Height = op.height,
                Slice = 1,
                MipLevels = 1,
                Format = TextureFormat.R8G8B8A8_UNorm,
                Usage = TextureUsage.ShaderResource,
            };

            var span = pixelBytes.Slice((int)op.pixelDataOffset, (int)op.pixelDataSize);
            var texHandle = ResourceUtility.CreateTexture(context.ResourceManager, context.ResourceDatabase, context.ResourceAllocator, commandBuffer, span, in texDesc, $"ImGuiTex_{op.textureKey & 0x7FFF_FFFFu}");
            if (!texHandle.IsInvalid)
            {
                commandBuffer.Barrier(BarrierDesc.Texture(texHandle, BarrierSync.None, BarrierSync.AllShading, BarrierAccess.Common, BarrierAccess.ShaderResource, BarrierLayout.Common, BarrierLayout.ShaderResource));
                _imguiTextures[op.textureKey] = texHandle;
                _imguiBindlessIds[op.textureKey] = context.ResourceDatabase.GetBindlessIndex(texHandle.AsResource(), BindlessAccess.ShaderResource);
            }
        }
    }

    private void ClearAndPresentEmptySwapChain(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        if (!context.SwapChainManager.TryGetSwapChain(0, out var swapChain))
        {
            return;
        }

        try
        {
            ClearAndPresentSwapChainTexture(commandBuffer, swapChain.GetCurrentBackBuffer());
        }
        finally
        {
            context.SwapChainManager.ReleaseSwapChain(0);
        }
    }

    private static void ClearAndPresentSwapChainTexture(ICommandBuffer commandBuffer, Handle<GPUTexture> backBuffer)
    {
        commandBuffer.Barrier(BarrierDesc.Texture(backBuffer, BarrierSync.None, BarrierSync.RenderTarget, BarrierAccess.NoAccess, BarrierAccess.RenderTarget, BarrierLayout.Present, BarrierLayout.RenderTarget));

        var rtDesc = new PassRenderTargetDesc
        {
            Texture = backBuffer,
            ClearColor = new Color128(0.08f, 0.08f, 0.10f, 1.0f),
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
        };

        var depthDesc = new PassDepthStencilDesc
        {
            Texture = Handle<GPUTexture>.Invalid,
        };

        Span<PassRenderTargetDesc> rtDescs = stackalloc PassRenderTargetDesc[1];
        rtDescs[0] = rtDesc;
        commandBuffer.BeginRenderPass(rtDescs, in depthDesc);
        commandBuffer.EndRenderPass();

        commandBuffer.Barrier(BarrierDesc.Texture(backBuffer, BarrierSync.RenderTarget, BarrierSync.None, BarrierAccess.RenderTarget, BarrierAccess.NoAccess, BarrierLayout.RenderTarget, BarrierLayout.Present));
    }

    private bool EnsureShaderReady(scoped in RenderContext context)
    {
        if (!_shaderHandle.IsInvalid && _forwardPassIndex >= 0)
        {
            return true;
        }

        if (_shaderEntry is not { State: AssetState.Ready })
        {
            return false;
        }

        var handle = Handle<Shader>.Invalid;
        _shaderEntry.ReadAssetData(ref handle);
        if (handle.IsInvalid)
        {
            return false;
        }

        var shaderResult = context.ResourceManager.GetShaderReference(handle);
        if (shaderResult.IsFailure)
        {
            return false;
        }

        _forwardPassIndex = shaderResult.Value.GetPassIndex("Forward");
        if (_forwardPassIndex < 0)
        {
            return false;
        }

        _shaderHandle = handle;
        return true;
    }

    private bool EnsureFrameBuffers(scoped in RenderContext context, int slot, nuint requiredVertexBytes, nuint requiredIndexBytes)
    {
        if (_vertexBuffers[slot].IsInvalid || _vertexCapacities[slot] < requiredVertexBytes)
        {
            if (!_vertexBuffers[slot].IsInvalid)
            {
                context.ResourceDatabase.ReleaseResource(_vertexBuffers[slot].AsResource());
            }

            var allocSize = MemoryUtility.AlignUp(Math.Max(requiredVertexBytes, 64u * 1024u), 4);
            var desc = new BufferDesc
            {
                Size = allocSize,
                Stride = 0,
                Usage = BufferUsage.ShaderResource | BufferUsage.Raw,
                HeapType = HeapType.Upload,
            };

            _vertexBuffers[slot] = context.ResourceAllocator.CreateBuffer(in desc, $"ImGuiVB_{slot}");
            if (_vertexBuffers[slot].IsInvalid)
            {
                _vertexCapacities[slot] = 0;
                return false;
            }

            _vertexCapacities[slot] = allocSize;
        }

        if (_indexBuffers[slot].IsInvalid || _indexCapacities[slot] < requiredIndexBytes)
        {
            if (!_indexBuffers[slot].IsInvalid)
            {
                context.ResourceDatabase.ReleaseResource(_indexBuffers[slot].AsResource());
            }

            var allocSize = Math.Max(requiredIndexBytes, 32u * 1024u);
            var alignedSize = MemoryUtility.AlignUp(allocSize, 4);
            var desc = new BufferDesc
            {
                Size = alignedSize,
                Stride = 0,
                Usage = BufferUsage.ShaderResource | BufferUsage.Raw,
                HeapType = HeapType.Upload,
            };

            _indexBuffers[slot] = context.ResourceAllocator.CreateBuffer(in desc, $"ImGuiIB_{slot}");
            if (_indexBuffers[slot].IsInvalid)
            {
                _indexCapacities[slot] = 0;
                return false;
            }

            _indexCapacities[slot] = alignedSize;
        }

        return true;
    }

    private void UploadFrameData(scoped in RenderContext context, int slot, ReadOnlySpan<byte> vertexBytes, ReadOnlySpan<ushort> indices)
    {
        var pVb = context.ResourceDatabase.MapResource(_vertexBuffers[slot].AsResource(), 0, null);
        fixed (byte* pSrcVb = vertexBytes)
        {
            MemoryUtility.MemCpy(pVb, pSrcVb, (nuint)vertexBytes.Length);
        }
        context.ResourceDatabase.UnmapResource(_vertexBuffers[slot].AsResource(), 0, null);

        var pIb = context.ResourceDatabase.MapResource(_indexBuffers[slot].AsResource(), 0, null);
        fixed (ushort* pSrcIb = indices)
        {
            MemoryUtility.MemCpy(pIb, pSrcIb, (nuint)(indices.Length * sizeof(ushort)));
        }
        context.ResourceDatabase.UnmapResource(_indexBuffers[slot].AsResource(), 0, null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_lastResourceDatabase != null)
        {
            foreach (var texHandle in _imguiTextures.Values)
            {
                if (!texHandle.IsInvalid)
                {
                    _lastResourceDatabase.ReleaseResource(texHandle.AsResource());
                }
            }

            for (var i = 0; i < _vertexBuffers.Length; i++)
            {
                if (!_vertexBuffers[i].IsInvalid)
                {
                    _lastResourceDatabase.ReleaseResource(_vertexBuffers[i].AsResource());
                    _vertexBuffers[i] = Handle<GPUBuffer>.Invalid;
                }

                if (!_indexBuffers[i].IsInvalid)
                {
                    _lastResourceDatabase.ReleaseResource(_indexBuffers[i].AsResource());
                    _indexBuffers[i] = Handle<GPUBuffer>.Invalid;
                }
            }
        }

        _imguiTextures.Clear();
        _imguiBindlessIds.Clear();

        for (var i = 0; i < _snapshots.Length; i++)
        {
            _snapshots[i].Dispose();
        }
    }
}
