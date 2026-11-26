using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("9524EA5E-C1B0-4FE5-A203-A75ABC5D8E3A")]
[NativeTypeName("struct ID3D12GraphicsCommandList12 : ID3D12GraphicsCommandList11")]
public unsafe partial struct ID3D12GraphicsCommandList12 : ID3D12GraphicsCommandList12.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12GraphicsCommandList12);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint>)(lpVtbl[1]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint>)(lpVtbl[2]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, char*, int>)(lpVtbl[6]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public new D3D12_COMMAND_LIST_TYPE GetType()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_COMMAND_LIST_TYPE>)(lpVtbl[8]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT Close()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, int>)(lpVtbl[9]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT Reset(ID3D12CommandAllocator* pAllocator, ID3D12PipelineState* pInitialState)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12CommandAllocator*, ID3D12PipelineState*, int>)(lpVtbl[10]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pAllocator, pInitialState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public void ClearState(ID3D12PipelineState* pPipelineState)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12PipelineState*, void>)(lpVtbl[11]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pPipelineState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public void DrawInstanced(uint VertexCountPerInstance, uint InstanceCount, uint StartVertexLocation, uint StartInstanceLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, uint, void>)(lpVtbl[12]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), VertexCountPerInstance, InstanceCount, StartVertexLocation, StartInstanceLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public void DrawIndexedInstanced(uint IndexCountPerInstance, uint InstanceCount, uint StartIndexLocation, int BaseVertexLocation, uint StartInstanceLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, int, uint, void>)(lpVtbl[13]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), IndexCountPerInstance, InstanceCount, StartIndexLocation, BaseVertexLocation, StartInstanceLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public void Dispatch(uint ThreadGroupCountX, uint ThreadGroupCountY, uint ThreadGroupCountZ)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, void>)(lpVtbl[14]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), ThreadGroupCountX, ThreadGroupCountY, ThreadGroupCountZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public void CopyBufferRegion(ID3D12Resource* pDstBuffer, [NativeTypeName("UINT64")] ulong DstOffset, ID3D12Resource* pSrcBuffer, [NativeTypeName("UINT64")] ulong SrcOffset, [NativeTypeName("UINT64")] ulong NumBytes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, ulong, ID3D12Resource*, ulong, ulong, void>)(lpVtbl[15]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstBuffer, DstOffset, pSrcBuffer, SrcOffset, NumBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public void CopyTextureRegion([NativeTypeName("const D3D12_TEXTURE_COPY_LOCATION *")] D3D12_TEXTURE_COPY_LOCATION* pDst, uint DstX, uint DstY, uint DstZ, [NativeTypeName("const D3D12_TEXTURE_COPY_LOCATION *")] D3D12_TEXTURE_COPY_LOCATION* pSrc, [NativeTypeName("const D3D12_BOX *")] D3D12_BOX* pSrcBox)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_TEXTURE_COPY_LOCATION*, uint, uint, uint, D3D12_TEXTURE_COPY_LOCATION*, D3D12_BOX*, void>)(lpVtbl[16]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDst, DstX, DstY, DstZ, pSrc, pSrcBox);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public void CopyResource(ID3D12Resource* pDstResource, ID3D12Resource* pSrcResource)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, ID3D12Resource*, void>)(lpVtbl[17]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstResource, pSrcResource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public void CopyTiles(ID3D12Resource* pTiledResource, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pTileRegionStartCoordinate, [NativeTypeName("const D3D12_TILE_REGION_SIZE *")] D3D12_TILE_REGION_SIZE* pTileRegionSize, ID3D12Resource* pBuffer, [NativeTypeName("UINT64")] ulong BufferStartOffsetInBytes, D3D12_TILE_COPY_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, D3D12_TILED_RESOURCE_COORDINATE*, D3D12_TILE_REGION_SIZE*, ID3D12Resource*, ulong, D3D12_TILE_COPY_FLAGS, void>)(lpVtbl[18]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pTiledResource, pTileRegionStartCoordinate, pTileRegionSize, pBuffer, BufferStartOffsetInBytes, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public void ResolveSubresource(ID3D12Resource* pDstResource, uint DstSubresource, ID3D12Resource* pSrcResource, uint SrcSubresource, DXGI_FORMAT Format)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, uint, ID3D12Resource*, uint, DXGI_FORMAT, void>)(lpVtbl[19]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstResource, DstSubresource, pSrcResource, SrcSubresource, Format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public void IASetPrimitiveTopology([NativeTypeName("D3D12_PRIMITIVE_TOPOLOGY")] D3D_PRIMITIVE_TOPOLOGY PrimitiveTopology)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D_PRIMITIVE_TOPOLOGY, void>)(lpVtbl[20]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), PrimitiveTopology);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public void RSSetViewports(uint NumViewports, [NativeTypeName("const D3D12_VIEWPORT *")] D3D12_VIEWPORT* pViewports)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_VIEWPORT*, void>)(lpVtbl[21]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumViewports, pViewports);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public void RSSetScissorRects(uint NumRects, [NativeTypeName("const D3D12_RECT *")] RECT* pRects)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, RECT*, void>)(lpVtbl[22]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumRects, pRects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public void OMSetBlendFactor([NativeTypeName("const FLOAT[4]")] float* BlendFactor)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, float*, void>)(lpVtbl[23]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), BlendFactor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    public void OMSetStencilRef(uint StencilRef)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, void>)(lpVtbl[24]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), StencilRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public void SetPipelineState(ID3D12PipelineState* pPipelineState)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12PipelineState*, void>)(lpVtbl[25]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pPipelineState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    public void ResourceBarrier(uint NumBarriers, [NativeTypeName("const D3D12_RESOURCE_BARRIER *")] D3D12_RESOURCE_BARRIER* pBarriers)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_RESOURCE_BARRIER*, void>)(lpVtbl[26]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumBarriers, pBarriers);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    public void ExecuteBundle(ID3D12GraphicsCommandList* pCommandList)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12GraphicsCommandList*, void>)(lpVtbl[27]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pCommandList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(28)]
    public void SetDescriptorHeaps(uint NumDescriptorHeaps, [NativeTypeName("ID3D12DescriptorHeap *const *")] ID3D12DescriptorHeap** ppDescriptorHeaps)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ID3D12DescriptorHeap**, void>)(lpVtbl[28]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumDescriptorHeaps, ppDescriptorHeaps);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(29)]
    public void SetComputeRootSignature(ID3D12RootSignature* pRootSignature)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12RootSignature*, void>)(lpVtbl[29]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pRootSignature);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(30)]
    public void SetGraphicsRootSignature(ID3D12RootSignature* pRootSignature)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12RootSignature*, void>)(lpVtbl[30]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pRootSignature);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(31)]
    public void SetComputeRootDescriptorTable(uint RootParameterIndex, D3D12_GPU_DESCRIPTOR_HANDLE BaseDescriptor)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_GPU_DESCRIPTOR_HANDLE, void>)(lpVtbl[31]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BaseDescriptor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(32)]
    public void SetGraphicsRootDescriptorTable(uint RootParameterIndex, D3D12_GPU_DESCRIPTOR_HANDLE BaseDescriptor)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_GPU_DESCRIPTOR_HANDLE, void>)(lpVtbl[32]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BaseDescriptor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(33)]
    public void SetComputeRoot32BitConstant(uint RootParameterIndex, uint SrcData, uint DestOffsetIn32BitValues)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, void>)(lpVtbl[33]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, SrcData, DestOffsetIn32BitValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(34)]
    public void SetGraphicsRoot32BitConstant(uint RootParameterIndex, uint SrcData, uint DestOffsetIn32BitValues)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, void>)(lpVtbl[34]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, SrcData, DestOffsetIn32BitValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(35)]
    public void SetComputeRoot32BitConstants(uint RootParameterIndex, uint Num32BitValuesToSet, [NativeTypeName("const void *")] void* pSrcData, uint DestOffsetIn32BitValues)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, void*, uint, void>)(lpVtbl[35]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, Num32BitValuesToSet, pSrcData, DestOffsetIn32BitValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(36)]
    public void SetGraphicsRoot32BitConstants(uint RootParameterIndex, uint Num32BitValuesToSet, [NativeTypeName("const void *")] void* pSrcData, uint DestOffsetIn32BitValues)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, void*, uint, void>)(lpVtbl[36]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, Num32BitValuesToSet, pSrcData, DestOffsetIn32BitValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(37)]
    public void SetComputeRootConstantBufferView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[37]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(38)]
    public void SetGraphicsRootConstantBufferView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[38]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(39)]
    public void SetComputeRootShaderResourceView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[39]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(40)]
    public void SetGraphicsRootShaderResourceView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[40]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(41)]
    public void SetComputeRootUnorderedAccessView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[41]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(42)]
    public void SetGraphicsRootUnorderedAccessView(uint RootParameterIndex, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong BufferLocation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ulong, void>)(lpVtbl[42]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RootParameterIndex, BufferLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(43)]
    public void IASetIndexBuffer([NativeTypeName("const D3D12_INDEX_BUFFER_VIEW *")] D3D12_INDEX_BUFFER_VIEW* pView)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_INDEX_BUFFER_VIEW*, void>)(lpVtbl[43]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pView);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(44)]
    public void IASetVertexBuffers(uint StartSlot, uint NumViews, [NativeTypeName("const D3D12_VERTEX_BUFFER_VIEW *")] D3D12_VERTEX_BUFFER_VIEW* pViews)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, D3D12_VERTEX_BUFFER_VIEW*, void>)(lpVtbl[44]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), StartSlot, NumViews, pViews);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(45)]
    public void SOSetTargets(uint StartSlot, uint NumViews, [NativeTypeName("const D3D12_STREAM_OUTPUT_BUFFER_VIEW *")] D3D12_STREAM_OUTPUT_BUFFER_VIEW* pViews)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, D3D12_STREAM_OUTPUT_BUFFER_VIEW*, void>)(lpVtbl[45]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), StartSlot, NumViews, pViews);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(46)]
    public void OMSetRenderTargets(uint NumRenderTargetDescriptors, [NativeTypeName("const D3D12_CPU_DESCRIPTOR_HANDLE *")] D3D12_CPU_DESCRIPTOR_HANDLE* pRenderTargetDescriptors, [NativeTypeName("BOOL")] int RTsSingleHandleToDescriptorRange, [NativeTypeName("const D3D12_CPU_DESCRIPTOR_HANDLE *")] D3D12_CPU_DESCRIPTOR_HANDLE* pDepthStencilDescriptor)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_CPU_DESCRIPTOR_HANDLE*, int, D3D12_CPU_DESCRIPTOR_HANDLE*, void>)(lpVtbl[46]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumRenderTargetDescriptors, pRenderTargetDescriptors, RTsSingleHandleToDescriptorRange, pDepthStencilDescriptor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(47)]
    public void ClearDepthStencilView(D3D12_CPU_DESCRIPTOR_HANDLE DepthStencilView, D3D12_CLEAR_FLAGS ClearFlags, float Depth, [NativeTypeName("UINT8")] byte Stencil, uint NumRects, [NativeTypeName("const D3D12_RECT *")] RECT* pRects)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_CPU_DESCRIPTOR_HANDLE, D3D12_CLEAR_FLAGS, float, byte, uint, RECT*, void>)(lpVtbl[47]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), DepthStencilView, ClearFlags, Depth, Stencil, NumRects, pRects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(48)]
    public void ClearRenderTargetView(D3D12_CPU_DESCRIPTOR_HANDLE RenderTargetView, [NativeTypeName("const FLOAT[4]")] float* ColorRGBA, uint NumRects, [NativeTypeName("const D3D12_RECT *")] RECT* pRects)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_CPU_DESCRIPTOR_HANDLE, float*, uint, RECT*, void>)(lpVtbl[48]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RenderTargetView, ColorRGBA, NumRects, pRects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(49)]
    public void ClearUnorderedAccessViewUint(D3D12_GPU_DESCRIPTOR_HANDLE ViewGPUHandleInCurrentHeap, D3D12_CPU_DESCRIPTOR_HANDLE ViewCPUHandle, ID3D12Resource* pResource, [NativeTypeName("const UINT[4]")] uint* Values, uint NumRects, [NativeTypeName("const D3D12_RECT *")] RECT* pRects)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_GPU_DESCRIPTOR_HANDLE, D3D12_CPU_DESCRIPTOR_HANDLE, ID3D12Resource*, uint*, uint, RECT*, void>)(lpVtbl[49]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), ViewGPUHandleInCurrentHeap, ViewCPUHandle, pResource, Values, NumRects, pRects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(50)]
    public void ClearUnorderedAccessViewFloat(D3D12_GPU_DESCRIPTOR_HANDLE ViewGPUHandleInCurrentHeap, D3D12_CPU_DESCRIPTOR_HANDLE ViewCPUHandle, ID3D12Resource* pResource, [NativeTypeName("const FLOAT[4]")] float* Values, uint NumRects, [NativeTypeName("const D3D12_RECT *")] RECT* pRects)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_GPU_DESCRIPTOR_HANDLE, D3D12_CPU_DESCRIPTOR_HANDLE, ID3D12Resource*, float*, uint, RECT*, void>)(lpVtbl[50]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), ViewGPUHandleInCurrentHeap, ViewCPUHandle, pResource, Values, NumRects, pRects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(51)]
    public void DiscardResource(ID3D12Resource* pResource, [NativeTypeName("const D3D12_DISCARD_REGION *")] D3D12_DISCARD_REGION* pRegion)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, D3D12_DISCARD_REGION*, void>)(lpVtbl[51]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pResource, pRegion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(52)]
    public void BeginQuery(ID3D12QueryHeap* pQueryHeap, D3D12_QUERY_TYPE Type, uint Index)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12QueryHeap*, D3D12_QUERY_TYPE, uint, void>)(lpVtbl[52]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pQueryHeap, Type, Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(53)]
    public void EndQuery(ID3D12QueryHeap* pQueryHeap, D3D12_QUERY_TYPE Type, uint Index)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12QueryHeap*, D3D12_QUERY_TYPE, uint, void>)(lpVtbl[53]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pQueryHeap, Type, Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(54)]
    public void ResolveQueryData(ID3D12QueryHeap* pQueryHeap, D3D12_QUERY_TYPE Type, uint StartIndex, uint NumQueries, ID3D12Resource* pDestinationBuffer, [NativeTypeName("UINT64")] ulong AlignedDestinationBufferOffset)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12QueryHeap*, D3D12_QUERY_TYPE, uint, uint, ID3D12Resource*, ulong, void>)(lpVtbl[54]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pQueryHeap, Type, StartIndex, NumQueries, pDestinationBuffer, AlignedDestinationBufferOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(55)]
    public void SetPredication(ID3D12Resource* pBuffer, [NativeTypeName("UINT64")] ulong AlignedBufferOffset, D3D12_PREDICATION_OP Operation)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, ulong, D3D12_PREDICATION_OP, void>)(lpVtbl[55]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pBuffer, AlignedBufferOffset, Operation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(56)]
    public void SetMarker(uint Metadata, [NativeTypeName("const void *")] void* pData, uint Size)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, void*, uint, void>)(lpVtbl[56]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Metadata, pData, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(57)]
    public void BeginEvent(uint Metadata, [NativeTypeName("const void *")] void* pData, uint Size)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, void*, uint, void>)(lpVtbl[57]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Metadata, pData, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(58)]
    public void EndEvent()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, void>)(lpVtbl[58]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(59)]
    public void ExecuteIndirect(ID3D12CommandSignature* pCommandSignature, uint MaxCommandCount, ID3D12Resource* pArgumentBuffer, [NativeTypeName("UINT64")] ulong ArgumentBufferOffset, ID3D12Resource* pCountBuffer, [NativeTypeName("UINT64")] ulong CountBufferOffset)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12CommandSignature*, uint, ID3D12Resource*, ulong, ID3D12Resource*, ulong, void>)(lpVtbl[59]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pCommandSignature, MaxCommandCount, pArgumentBuffer, ArgumentBufferOffset, pCountBuffer, CountBufferOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(60)]
    public void AtomicCopyBufferUINT(ID3D12Resource* pDstBuffer, [NativeTypeName("UINT64")] ulong DstOffset, ID3D12Resource* pSrcBuffer, [NativeTypeName("UINT64")] ulong SrcOffset, uint Dependencies, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDependentResources, [NativeTypeName("const D3D12_SUBRESOURCE_RANGE_UINT64 *")] D3D12_SUBRESOURCE_RANGE_UINT64* pDependentSubresourceRanges)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, ulong, ID3D12Resource*, ulong, uint, ID3D12Resource**, D3D12_SUBRESOURCE_RANGE_UINT64*, void>)(lpVtbl[60]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstBuffer, DstOffset, pSrcBuffer, SrcOffset, Dependencies, ppDependentResources, pDependentSubresourceRanges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(61)]
    public void AtomicCopyBufferUINT64(ID3D12Resource* pDstBuffer, [NativeTypeName("UINT64")] ulong DstOffset, ID3D12Resource* pSrcBuffer, [NativeTypeName("UINT64")] ulong SrcOffset, uint Dependencies, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDependentResources, [NativeTypeName("const D3D12_SUBRESOURCE_RANGE_UINT64 *")] D3D12_SUBRESOURCE_RANGE_UINT64* pDependentSubresourceRanges)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, ulong, ID3D12Resource*, ulong, uint, ID3D12Resource**, D3D12_SUBRESOURCE_RANGE_UINT64*, void>)(lpVtbl[61]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstBuffer, DstOffset, pSrcBuffer, SrcOffset, Dependencies, ppDependentResources, pDependentSubresourceRanges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(62)]
    public void OMSetDepthBounds(float Min, float Max)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, float, float, void>)(lpVtbl[62]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Min, Max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(63)]
    public void SetSamplePositions(uint NumSamplesPerPixel, uint NumPixels, D3D12_SAMPLE_POSITION* pSamplePositions)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, D3D12_SAMPLE_POSITION*, void>)(lpVtbl[63]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumSamplesPerPixel, NumPixels, pSamplePositions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(64)]
    public void ResolveSubresourceRegion(ID3D12Resource* pDstResource, uint DstSubresource, uint DstX, uint DstY, ID3D12Resource* pSrcResource, uint SrcSubresource, [NativeTypeName("D3D12_RECT *")] RECT* pSrcRect, DXGI_FORMAT Format, D3D12_RESOLVE_MODE ResolveMode)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, uint, uint, uint, ID3D12Resource*, uint, RECT*, DXGI_FORMAT, D3D12_RESOLVE_MODE, void>)(lpVtbl[64]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDstResource, DstSubresource, DstX, DstY, pSrcResource, SrcSubresource, pSrcRect, Format, ResolveMode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(65)]
    public void SetViewInstanceMask(uint Mask)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, void>)(lpVtbl[65]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(66)]
    public void WriteBufferImmediate(uint Count, [NativeTypeName("const D3D12_WRITEBUFFERIMMEDIATE_PARAMETER *")] D3D12_WRITEBUFFERIMMEDIATE_PARAMETER* pParams, [NativeTypeName("const D3D12_WRITEBUFFERIMMEDIATE_MODE *")] D3D12_WRITEBUFFERIMMEDIATE_MODE* pModes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_WRITEBUFFERIMMEDIATE_PARAMETER*, D3D12_WRITEBUFFERIMMEDIATE_MODE*, void>)(lpVtbl[66]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), Count, pParams, pModes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(67)]
    public void SetProtectedResourceSession(ID3D12ProtectedResourceSession* pProtectedResourceSession)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12ProtectedResourceSession*, void>)(lpVtbl[67]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pProtectedResourceSession);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(68)]
    public void BeginRenderPass(uint NumRenderTargets, [NativeTypeName("const D3D12_RENDER_PASS_RENDER_TARGET_DESC *")] D3D12_RENDER_PASS_RENDER_TARGET_DESC* pRenderTargets, [NativeTypeName("const D3D12_RENDER_PASS_DEPTH_STENCIL_DESC *")] D3D12_RENDER_PASS_DEPTH_STENCIL_DESC* pDepthStencil, D3D12_RENDER_PASS_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_RENDER_PASS_RENDER_TARGET_DESC*, D3D12_RENDER_PASS_DEPTH_STENCIL_DESC*, D3D12_RENDER_PASS_FLAGS, void>)(lpVtbl[68]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumRenderTargets, pRenderTargets, pDepthStencil, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(69)]
    public void EndRenderPass()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, void>)(lpVtbl[69]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(70)]
    public void InitializeMetaCommand(ID3D12MetaCommand* pMetaCommand, [NativeTypeName("const void *")] void* pInitializationParametersData, [NativeTypeName("SIZE_T")] nuint InitializationParametersDataSizeInBytes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12MetaCommand*, void*, nuint, void>)(lpVtbl[70]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pMetaCommand, pInitializationParametersData, InitializationParametersDataSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(71)]
    public void ExecuteMetaCommand(ID3D12MetaCommand* pMetaCommand, [NativeTypeName("const void *")] void* pExecutionParametersData, [NativeTypeName("SIZE_T")] nuint ExecutionParametersDataSizeInBytes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12MetaCommand*, void*, nuint, void>)(lpVtbl[71]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pMetaCommand, pExecutionParametersData, ExecutionParametersDataSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(72)]
    public void BuildRaytracingAccelerationStructure([NativeTypeName("const D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC *")] D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC* pDesc, uint NumPostbuildInfoDescs, [NativeTypeName("const D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC *")] D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC* pPostbuildInfoDescs)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC*, uint, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC*, void>)(lpVtbl[72]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDesc, NumPostbuildInfoDescs, pPostbuildInfoDescs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(73)]
    public void EmitRaytracingAccelerationStructurePostbuildInfo([NativeTypeName("const D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC *")] D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC* pDesc, uint NumSourceAccelerationStructures, [NativeTypeName("const D3D12_GPU_VIRTUAL_ADDRESS *")] ulong* pSourceAccelerationStructureData)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC*, uint, ulong*, void>)(lpVtbl[73]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDesc, NumSourceAccelerationStructures, pSourceAccelerationStructureData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(74)]
    public void CopyRaytracingAccelerationStructure([NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong DestAccelerationStructureData, [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong SourceAccelerationStructureData, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE Mode)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ulong, ulong, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE, void>)(lpVtbl[74]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), DestAccelerationStructureData, SourceAccelerationStructureData, Mode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(75)]
    public void SetPipelineState1(ID3D12StateObject* pStateObject)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12StateObject*, void>)(lpVtbl[75]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pStateObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(76)]
    public void DispatchRays([NativeTypeName("const D3D12_DISPATCH_RAYS_DESC *")] D3D12_DISPATCH_RAYS_DESC* pDesc)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_DISPATCH_RAYS_DESC*, void>)(lpVtbl[76]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(77)]
    public void RSSetShadingRate(D3D12_SHADING_RATE baseShadingRate, [NativeTypeName("const D3D12_SHADING_RATE_COMBINER *")] D3D12_SHADING_RATE_COMBINER* combiners)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_SHADING_RATE, D3D12_SHADING_RATE_COMBINER*, void>)(lpVtbl[77]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), baseShadingRate, combiners);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(78)]
    public void RSSetShadingRateImage(ID3D12Resource* shadingRateImage)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, void>)(lpVtbl[78]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), shadingRateImage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(79)]
    public void DispatchMesh(uint ThreadGroupCountX, uint ThreadGroupCountY, uint ThreadGroupCountZ)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, uint, void>)(lpVtbl[79]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), ThreadGroupCountX, ThreadGroupCountY, ThreadGroupCountZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(80)]
    public void Barrier([NativeTypeName("UINT32")] uint NumBarrierGroups, [NativeTypeName("const D3D12_BARRIER_GROUP *")] D3D12_BARRIER_GROUP* pBarrierGroups)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_BARRIER_GROUP*, void>)(lpVtbl[80]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumBarrierGroups, pBarrierGroups);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(81)]
    public void OMSetFrontAndBackStencilRef(uint FrontStencilRef, uint BackStencilRef)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, uint, void>)(lpVtbl[81]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), FrontStencilRef, BackStencilRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(82)]
    public void RSSetDepthBias(float DepthBias, float DepthBiasClamp, float SlopeScaledDepthBias)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, float, float, float, void>)(lpVtbl[82]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), DepthBias, DepthBiasClamp, SlopeScaledDepthBias);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(83)]
    public void IASetIndexBufferStripCutValue(D3D12_INDEX_BUFFER_STRIP_CUT_VALUE IBStripCutValue)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_INDEX_BUFFER_STRIP_CUT_VALUE, void>)(lpVtbl[83]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), IBStripCutValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(84)]
    public void SetProgram([NativeTypeName("const D3D12_SET_PROGRAM_DESC *")] D3D12_SET_PROGRAM_DESC* pDesc)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_SET_PROGRAM_DESC*, void>)(lpVtbl[84]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(85)]
    public void DispatchGraph([NativeTypeName("const D3D12_DISPATCH_GRAPH_DESC *")] D3D12_DISPATCH_GRAPH_DESC* pDesc)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_DISPATCH_GRAPH_DESC*, void>)(lpVtbl[85]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(86)]
    public void WaitBarrier(uint NumBarrierGroups, [NativeTypeName("const D3D12_BARRIER_GROUP *")] D3D12_BARRIER_GROUP* pBarrierGroups, uint NumFences, [NativeTypeName("ID3D12Fence *const *")] ID3D12Fence** ppFences, [NativeTypeName("const UINT64 *")] ulong* pFenceValues)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_BARRIER_GROUP*, uint, ID3D12Fence**, ulong*, void>)(lpVtbl[86]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumBarrierGroups, pBarrierGroups, NumFences, ppFences, pFenceValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(87)]
    public void SignalBarrier(uint NumBarrierGroups, [NativeTypeName("const D3D12_BARRIER_GROUP *")] D3D12_BARRIER_GROUP* pBarrierGroups, ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong FenceValue)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_BARRIER_GROUP*, ID3D12Fence*, ulong, void>)(lpVtbl[87]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumBarrierGroups, pBarrierGroups, pFence, FenceValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(88)]
    public void CopyBufferRegions(uint NumRegions, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDestBuffers, [NativeTypeName("const UINT64 *")] ulong* pDestOffsets, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppSourceBuffers, [NativeTypeName("const UINT64 *")] ulong* pSourceOffsets, [NativeTypeName("const UINT64 *")] ulong* pSizes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ID3D12Resource**, ulong*, ID3D12Resource**, ulong*, ulong*, void>)(lpVtbl[88]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumRegions, ppDestBuffers, pDestOffsets, ppSourceBuffers, pSourceOffsets, pSizes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(89)]
    public void CopyTextureRegions(uint NumCopyDescs, [NativeTypeName("const D3D12_TEXTURE_COPY_DESC *")] D3D12_TEXTURE_COPY_DESC* pCopyDescs)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_TEXTURE_COPY_DESC*, void>)(lpVtbl[89]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumCopyDescs, pCopyDescs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(90)]
    public void CopyResources(uint NumSourceDestPairs, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDestResources, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppSourceResources)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, ID3D12Resource**, ID3D12Resource**, void>)(lpVtbl[90]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumSourceDestPairs, ppDestResources, ppSourceResources);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(91)]
    public void CopyTilesAsync(ID3D12Resource* pTiledResource, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pTileRegionStartCoordinate, [NativeTypeName("const D3D12_TILE_REGION_SIZE *")] D3D12_TILE_REGION_SIZE* pTileRegionSize, ID3D12Resource* pBuffer, [NativeTypeName("UINT64")] ulong BufferOffset, D3D12_TILE_COPY_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, D3D12_TILED_RESOURCE_COORDINATE*, D3D12_TILE_REGION_SIZE*, ID3D12Resource*, ulong, D3D12_TILE_COPY_FLAGS, void>)(lpVtbl[91]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pTiledResource, pTileRegionStartCoordinate, pTileRegionSize, pBuffer, BufferOffset, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(92)]
    public void ResolveSubresourceRegionAsync(ID3D12Resource* pDestResource, uint DestSubresource, uint DestX, uint DestY, ID3D12Resource* pSourceResource, uint SourceSubresource, [NativeTypeName("const D3D12_RECT *")] RECT* pSourceRect, DXGI_FORMAT Format, D3D12_RESOLVE_MODE ResolveMode)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12Resource*, uint, uint, uint, ID3D12Resource*, uint, RECT*, DXGI_FORMAT, D3D12_RESOLVE_MODE, void>)(lpVtbl[92]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pDestResource, DestSubresource, DestX, DestY, pSourceResource, SourceSubresource, pSourceRect, Format, ResolveMode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(93)]
    public void ResolveQueryDataAsync(ID3D12QueryHeap* pQueryHeap, D3D12_QUERY_TYPE Type, uint StartIndex, uint NumQueries, ID3D12Resource* pDestBuffer, [NativeTypeName("UINT64")] ulong DestOffset)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, ID3D12QueryHeap*, D3D12_QUERY_TYPE, uint, uint, ID3D12Resource*, ulong, void>)(lpVtbl[93]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), pQueryHeap, Type, StartIndex, NumQueries, pDestBuffer, DestOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(94)]
    public void ClearBoundDepthStencilView(D3D12_CLEAR_FLAGS ClearFlags, float Depth, [NativeTypeName("UINT8")] byte Stencil, uint NumRegions, [NativeTypeName("const D3D12_BOX *")] D3D12_BOX* pRegions)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, D3D12_CLEAR_FLAGS, float, byte, uint, D3D12_BOX*, void>)(lpVtbl[94]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), ClearFlags, Depth, Stencil, NumRegions, pRegions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(95)]
    public void ClearBoundRenderTargetViews(uint RenderTargetMask, [NativeTypeName("const D3D12_CLEAR_DATA *")] D3D12_CLEAR_DATA* pClearValues, [NativeTypeName("const UINT *")] uint* pNumRegions, [NativeTypeName("const D3D12_BOX *const *")] D3D12_BOX** ppRegions)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_CLEAR_DATA*, uint*, D3D12_BOX**, void>)(lpVtbl[95]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), RenderTargetMask, pClearValues, pNumRegions, ppRegions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(96)]
    public void ClearTextureSubresources(uint NumClearDescs, [NativeTypeName("const D3D12_CLEAR_TEXTURE_DESC *")] D3D12_CLEAR_TEXTURE_DESC* pClearDescs)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_CLEAR_TEXTURE_DESC*, void>)(lpVtbl[96]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumClearDescs, pClearDescs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(97)]
    public void FillBuffers(uint NumFillDescs, [NativeTypeName("const D3D12_FILL_BUFFER_DESC *")] D3D12_FILL_BUFFER_DESC* pFillDescs)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GraphicsCommandList12*, uint, D3D12_FILL_BUFFER_DESC*, void>)(lpVtbl[97]))((ID3D12GraphicsCommandList12*)Unsafe.AsPointer(ref this), NumFillDescs, pFillDescs);
    }

    public interface Interface : ID3D12GraphicsCommandList11.Interface
    {
        [VtblIndex(88)]
        void CopyBufferRegions(uint NumRegions, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDestBuffers, [NativeTypeName("const UINT64 *")] ulong* pDestOffsets, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppSourceBuffers, [NativeTypeName("const UINT64 *")] ulong* pSourceOffsets, [NativeTypeName("const UINT64 *")] ulong* pSizes);

        [VtblIndex(89)]
        void CopyTextureRegions(uint NumCopyDescs, [NativeTypeName("const D3D12_TEXTURE_COPY_DESC *")] D3D12_TEXTURE_COPY_DESC* pCopyDescs);

        [VtblIndex(90)]
        void CopyResources(uint NumSourceDestPairs, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppDestResources, [NativeTypeName("ID3D12Resource *const *")] ID3D12Resource** ppSourceResources);

        [VtblIndex(91)]
        void CopyTilesAsync(ID3D12Resource* pTiledResource, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pTileRegionStartCoordinate, [NativeTypeName("const D3D12_TILE_REGION_SIZE *")] D3D12_TILE_REGION_SIZE* pTileRegionSize, ID3D12Resource* pBuffer, [NativeTypeName("UINT64")] ulong BufferOffset, D3D12_TILE_COPY_FLAGS Flags);

        [VtblIndex(92)]
        void ResolveSubresourceRegionAsync(ID3D12Resource* pDestResource, uint DestSubresource, uint DestX, uint DestY, ID3D12Resource* pSourceResource, uint SourceSubresource, [NativeTypeName("const D3D12_RECT *")] RECT* pSourceRect, DXGI_FORMAT Format, D3D12_RESOLVE_MODE ResolveMode);

        [VtblIndex(93)]
        void ResolveQueryDataAsync(ID3D12QueryHeap* pQueryHeap, D3D12_QUERY_TYPE Type, uint StartIndex, uint NumQueries, ID3D12Resource* pDestBuffer, [NativeTypeName("UINT64")] ulong DestOffset);

        [VtblIndex(94)]
        void ClearBoundDepthStencilView(D3D12_CLEAR_FLAGS ClearFlags, float Depth, [NativeTypeName("UINT8")] byte Stencil, uint NumRegions, [NativeTypeName("const D3D12_BOX *")] D3D12_BOX* pRegions);

        [VtblIndex(95)]
        void ClearBoundRenderTargetViews(uint RenderTargetMask, [NativeTypeName("const D3D12_CLEAR_DATA *")] D3D12_CLEAR_DATA* pClearValues, [NativeTypeName("const UINT *")] uint* pNumRegions, [NativeTypeName("const D3D12_BOX *const *")] D3D12_BOX** ppRegions);

        [VtblIndex(96)]
        void ClearTextureSubresources(uint NumClearDescs, [NativeTypeName("const D3D12_CLEAR_TEXTURE_DESC *")] D3D12_CLEAR_TEXTURE_DESC* pClearDescs);

        [VtblIndex(97)]
        void FillBuffers(uint NumFillDescs, [NativeTypeName("const D3D12_FILL_BUFFER_DESC *")] D3D12_FILL_BUFFER_DESC* pFillDescs);
    }
}
