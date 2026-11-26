using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("80EABF42-2568-4E5E-BD82-C37F86961DC3")]
[NativeTypeName("struct ID3D12PipelineLibrary1 : ID3D12PipelineLibrary")]
public unsafe partial struct ID3D12PipelineLibrary1 : ID3D12PipelineLibrary1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12PipelineLibrary1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, uint>)(lpVtbl[1]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, uint>)(lpVtbl[2]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, char*, int>)(lpVtbl[6]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT StorePipeline([NativeTypeName("LPCWSTR")] char* pName, ID3D12PipelineState* pPipeline)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, char*, ID3D12PipelineState*, int>)(lpVtbl[8]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), pName, pPipeline);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT LoadGraphicsPipeline([NativeTypeName("LPCWSTR")] char* pName, [NativeTypeName("const D3D12_GRAPHICS_PIPELINE_STATE_DESC *")] D3D12_GRAPHICS_PIPELINE_STATE_DESC* pDesc, [NativeTypeName("const IID &")] Guid* riid, void** ppPipelineState)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, char*, D3D12_GRAPHICS_PIPELINE_STATE_DESC*, Guid*, void**, int>)(lpVtbl[9]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), pName, pDesc, riid, ppPipelineState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT LoadComputePipeline([NativeTypeName("LPCWSTR")] char* pName, [NativeTypeName("const D3D12_COMPUTE_PIPELINE_STATE_DESC *")] D3D12_COMPUTE_PIPELINE_STATE_DESC* pDesc, [NativeTypeName("const IID &")] Guid* riid, void** ppPipelineState)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, char*, D3D12_COMPUTE_PIPELINE_STATE_DESC*, Guid*, void**, int>)(lpVtbl[10]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), pName, pDesc, riid, ppPipelineState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("SIZE_T")]
    public nuint GetSerializedSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, nuint>)(lpVtbl[11]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT Serialize(void* pData, [NativeTypeName("SIZE_T")] nuint DataSizeInBytes)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, void*, nuint, int>)(lpVtbl[12]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), pData, DataSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT LoadPipeline([NativeTypeName("LPCWSTR")] char* pName, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc, [NativeTypeName("const IID &")] Guid* riid, void** ppPipelineState)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12PipelineLibrary1*, char*, D3D12_PIPELINE_STATE_STREAM_DESC*, Guid*, void**, int>)(lpVtbl[13]))((ID3D12PipelineLibrary1*)Unsafe.AsPointer(ref this), pName, pDesc, riid, ppPipelineState);
    }

    public interface Interface : ID3D12PipelineLibrary.Interface
    {
        [VtblIndex(13)]
        HRESULT LoadPipeline([NativeTypeName("LPCWSTR")] char* pName, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc, [NativeTypeName("const IID &")] Guid* riid, void** ppPipelineState);
    }
}
