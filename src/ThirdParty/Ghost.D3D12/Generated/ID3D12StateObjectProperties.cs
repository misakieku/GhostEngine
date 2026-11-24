using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("DE5FA827-9BF9-4F26-89FF-D7F56FDE3860")]
[NativeTypeName("struct ID3D12StateObjectProperties : IUnknown")]
public unsafe partial struct ID3D12StateObjectProperties : ID3D12StateObjectProperties.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectProperties);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, uint>)(lpVtbl[1]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, uint>)(lpVtbl[2]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void* GetShaderIdentifier([NativeTypeName("LPCWSTR")] char* pExportName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, char*, void*>)(lpVtbl[3]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this), pExportName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("UINT64")]
    public ulong GetShaderStackSize([NativeTypeName("LPCWSTR")] char* pExportName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, char*, ulong>)(lpVtbl[4]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this), pExportName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("UINT64")]
    public ulong GetPipelineStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, ulong>)(lpVtbl[5]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void SetPipelineStackSize([NativeTypeName("UINT64")] ulong PipelineStackSizeInBytes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties*, ulong, void>)(lpVtbl[6]))((ID3D12StateObjectProperties*)Unsafe.AsPointer(ref this), PipelineStackSizeInBytes);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void* GetShaderIdentifier([NativeTypeName("LPCWSTR")] char* pExportName);

        [VtblIndex(4)]
        [return: NativeTypeName("UINT64")]
        ulong GetShaderStackSize([NativeTypeName("LPCWSTR")] char* pExportName);

        [VtblIndex(5)]
        [return: NativeTypeName("UINT64")]
        ulong GetPipelineStackSize();

        [VtblIndex(6)]
        void SetPipelineStackSize([NativeTypeName("UINT64")] ulong PipelineStackSizeInBytes);
    }
}
