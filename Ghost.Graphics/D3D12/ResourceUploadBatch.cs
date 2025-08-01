using System.Runtime.CompilerServices;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// Resource upload batch for efficiently uploading resources to GPU memory
/// </summary>
internal unsafe class ResourceUploadBatch : IDisposable
{
    private ComPtr<ID3D12CommandAllocator> _commandAllocator;
    private ComPtr<ID3D12GraphicsCommandList10> _commandList;
    private ComPtr<ID3D12Fence> _fence;

    private readonly GraphicsDevice _device = GraphicsPipeline.GraphicsDevice;
    private readonly AutoResetEvent _fenceEvent = new(false);

    private ulong _fenceValue;
    private bool _isRecording;
    private bool _disposed;

    /// <summary>
    /// Gets whether the batch is currently recording commands
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Creates a new ResourceUploadBatch
    /// </summary>
    internal ResourceUploadBatch()
    {
        Initialize();
    }

    ~ResourceUploadBatch()
    {
        Dispose();
    }

    private void Initialize()
    {
        ThrowIfFailed(_device.NativeDevice.Ptr->CreateCommandAllocator(
            CommandListType.Direct,
            __uuidof<ID3D12CommandAllocator>(),
            _commandAllocator.GetVoidAddressOf()));

        ThrowIfFailed(_device.NativeDevice.Ptr->CreateCommandList1(
            0,
            CommandListType.Direct,
            CommandListFlags.None,
            __uuidof<ID3D12GraphicsCommandList10>(),
            _commandList.GetVoidAddressOf()));

        ThrowIfFailed(_device.NativeDevice.Ptr->CreateFence(0, FenceFlags.None, __uuidof<ID3D12Fence>(), _fence.GetVoidAddressOf()));
    }

    /// <summary>
    /// Begins recording upload commands
    /// </summary>
    public void Begin()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Upload batch is already recording");
        }

        ThrowIfFailed(_commandAllocator.Get()->Reset());
        ThrowIfFailed(_commandList.Get()->Reset(_commandAllocator.Get(), null));

        _isRecording = true;
    }

    /// <summary>
    /// Uploads buffer data to a resource
    /// </summary>
    /// <typeparam name="T">Type of data to upload</typeparam>
    /// <param name="resource">Destination resource</param>
    /// <param name="data">Source data</param>
    public void Upload<T>(ID3D12Resource* resource, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Upload batch is not recording");
        }

        var sizeInBytes = (uint)(data.Length * sizeof(T));
        var uploadBuffer = GraphicsPipeline.ResourceAllocator.CreateUploadBuffer(sizeInBytes, true);

        void* mappedData;
        var uploadResource = uploadBuffer.ResourceHandle.GetAllocation().Resource;
        uploadResource->Map(0, null, &mappedData);
        fixed (T* dataPtr = data)
        {
            Unsafe.CopyBlock(mappedData, dataPtr, sizeInBytes);
        }
        uploadResource->Unmap(0, null);

        // Copy from upload buffer to destination
        _commandList.Get()->CopyBufferRegion(
            resource,
            0,
            uploadResource,
            0,
            sizeInBytes);
    }

    /// <summary>
    /// Uploads subresource data to a texture
    /// </summary>
    /// <param name="resource">Destination texture resource</param>
    /// <param name="firstSubresource">First subresource index</param>
    /// <param name="subresources">Subresource data array</param>
    /// <param name="numSubresources">Number of subresources</param>
    public void Upload(ID3D12Resource* resource, uint firstSubresource, SubresourceData* subresources, uint numSubresources)
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Upload batch is not recording");
        }

        var resourceDesc = resource->GetDesc();
        var requiredSize = GetRequiredIntermediateSize(resource, firstSubresource, numSubresources);
        var uploadBuffer = GraphicsPipeline.ResourceAllocator.CreateUploadBuffer((uint)requiredSize, true);

        UpdateSubresources(
            resource,
            uploadBuffer.ResourceHandle.GetAllocation().Resource,
            0,
            firstSubresource,
            numSubresources,
            subresources);
    }

    /// <summary>
    /// Adds a resource transition barrier
    /// </summary>
    /// <param name="resource">Resource to transition</param>
    /// <param name="stateBefore">State before transition</param>
    /// <param name="stateAfter">State after transition</param>
    public void Transition(ID3D12Resource* resource, ResourceStates stateBefore, ResourceStates stateAfter)
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Upload batch is not recording");
        }

        _commandList.Get()->ResourceBarrierTransition(resource, stateBefore, stateAfter);
    }

    /// <summary>
    /// Generates mipmaps for a texture (if supported)
    /// </summary>
    /// <param name="resource">Texture resource</param>
    public void GenerateMips(GraphicsResource resource)
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Upload batch is not recording");
        }

        // This would require compute shader implementation for mipmap generation
        // For now, this is a placeholder - DirectXTK12 uses a compute shader approach
        throw new NotImplementedException("Mipmap generation not yet implemented");
    }

    /// <summary>
    /// Ends recording and submits the batch for execution
    /// </summary>
    /// <returns>Future that completes when upload is finished</returns>
    public ulong End()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Upload batch is not recording");
        }

        ThrowIfFailed(_commandList.Get()->Close());
        _device.CommandQueue.Ptr->ExecuteCommandLists(1, (ID3D12CommandList**)_commandList.GetAddressOf());
        ThrowIfFailed(_device.CommandQueue.Ptr->Signal(_fence.Get(), ++_fenceValue));

        _isRecording = false;

        return _fenceValue;
    }

    /// <summary>
    /// Waits for the upload batch to complete
    /// </summary>
    /// <param name="fenceValue">Fence value to wait for</param>
    public void WaitForCompletion(ulong fenceValue)
    {
        if (_fence.Get()->GetCompletedValue() < fenceValue)
        {
            var handle = new Handle((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());
            ThrowIfFailed(_fence.Get()->SetEventOnCompletion(fenceValue, handle));
            _fenceEvent.WaitOne();
        }
    }

    private ulong GetRequiredIntermediateSize(ID3D12Resource* destinationResource, uint firstSubresource, uint numSubresources)
    {
        var resourceDesc = destinationResource->GetDesc();

        ulong requiredSize = 0;
        var numRows = stackalloc uint[(int)numSubresources];
        var rowSizeInBytes = stackalloc ulong[(int)numSubresources];

        _device.NativeDevice.Ptr->GetCopyableFootprints(
            &resourceDesc,
            firstSubresource,
            numSubresources,
            0,
            null,
            numRows,
            rowSizeInBytes,
            &requiredSize);

        return requiredSize;
    }

    private void UpdateSubresources(ID3D12Resource* destinationResource, ID3D12Resource* intermediate, ulong intermediateOffset, uint firstSubresource, uint numSubresources, SubresourceData* pSubresourceData)
    {
        var destDesc = destinationResource->GetDesc();

        var layouts = stackalloc PlacedSubresourceFootprint[(int)numSubresources];
        var numRows = stackalloc uint[(int)numSubresources];
        var rowSizeInBytes = stackalloc ulong[(int)numSubresources];
        ulong requiredSize = 0;

        _device.NativeDevice.Ptr->GetCopyableFootprints(
            &destDesc,
            firstSubresource,
            numSubresources,
            intermediateOffset,
            layouts,
            numRows,
            rowSizeInBytes,
            &requiredSize);

        void* pMappedData;
        ThrowIfFailed(intermediate->Map(0, null, &pMappedData));
        for (uint i = 0; i < numSubresources; i++)
        {
            var srcData = pSubresourceData[i];
            var destLayout = layouts[i];
            var pDestSlice = (byte*)pMappedData + destLayout.Offset;
            var pSrcSlice = (byte*)srcData.pData;

            for (uint y = 0; y < numRows[i]; y++)
            {
                var pDestRow = pDestSlice + (y * destLayout.Footprint.RowPitch);
                var pSrcRow = pSrcSlice + (y * srcData.RowPitch);

                Unsafe.CopyBlockUnaligned(pDestRow, pSrcRow, (uint)rowSizeInBytes[i]);
            }
        }

        intermediate->Unmap(0, null);

        if (destDesc.Dimension == ResourceDimension.Buffer)
        {
            _commandList.Get()->CopyBufferRegion(destinationResource, 0, intermediate, intermediateOffset, requiredSize);
        }
        else
        {
            for (uint i = 0; i < numSubresources; i++)
            {
                var destLocation = new TextureCopyLocation(destinationResource, firstSubresource + i);
                var srcLocation = new TextureCopyLocation(intermediate, in layouts[i]);
                var box = new Box
                {
                    left = 0,
                    top = 0,
                    front = 0,
                    right = (uint)destDesc.Width,
                    bottom = destDesc.Height,
                    back = destDesc.DepthOrArraySize
                };

                _commandList.Get()->CopyTextureRegion(&destLocation, 0, 0, 0, &srcLocation, &box);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRecording)
        {
            _commandList.Get()->Close();
            _isRecording = false;
        }

        _fence.Dispose();
        _commandList.Dispose();
        _commandAllocator.Dispose();

        _fenceEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}