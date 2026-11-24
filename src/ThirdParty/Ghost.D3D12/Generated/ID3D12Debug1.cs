using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("AFFAA4CA-63FE-4D8E-B8AD-159000AF4304")]
[NativeTypeName("struct ID3D12Debug1 : IUnknown")]
public unsafe partial struct ID3D12Debug1 : ID3D12Debug1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Debug1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Debug1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, uint>)(lpVtbl[1]))((ID3D12Debug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, uint>)(lpVtbl[2]))((ID3D12Debug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableDebugLayer()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, void>)(lpVtbl[3]))((ID3D12Debug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetEnableGPUBasedValidation([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, int, void>)(lpVtbl[4]))((ID3D12Debug1*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void SetEnableSynchronizedCommandQueueValidation([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug1*, int, void>)(lpVtbl[5]))((ID3D12Debug1*)Unsafe.AsPointer(ref this), Enable);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void EnableDebugLayer();

        [VtblIndex(4)]
        void SetEnableGPUBasedValidation([NativeTypeName("BOOL")] int Enable);

        [VtblIndex(5)]
        void SetEnableSynchronizedCommandQueueValidation([NativeTypeName("BOOL")] int Enable);
    }
}
