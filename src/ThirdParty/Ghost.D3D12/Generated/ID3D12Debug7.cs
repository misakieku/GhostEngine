using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("31D409F9-22B4-409D-9C44-F72DA8A2BA61")]
[NativeTypeName("struct ID3D12Debug7 : ID3D12Debug6")]
public unsafe partial struct ID3D12Debug7 : ID3D12Debug7.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Debug7);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, uint>)(lpVtbl[1]))((ID3D12Debug7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, uint>)(lpVtbl[2]))((ID3D12Debug7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableDebugLayer()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, void>)(lpVtbl[3]))((ID3D12Debug7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetEnableGPUBasedValidation([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, int, void>)(lpVtbl[4]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void SetEnableSynchronizedCommandQueueValidation([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, int, void>)(lpVtbl[5]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void SetGPUBasedValidationFlags(D3D12_GPU_BASED_VALIDATION_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, D3D12_GPU_BASED_VALIDATION_FLAGS, void>)(lpVtbl[6]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public void DisableDebugLayer()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, void>)(lpVtbl[7]))((ID3D12Debug7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public void SetEnableAutoName([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, int, void>)(lpVtbl[8]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public void SetForceLegacyBarrierValidation([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, int, void>)(lpVtbl[9]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public void SetEnableDriverReportedMessages([NativeTypeName("BOOL")] int Enable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Debug7*, int, void>)(lpVtbl[10]))((ID3D12Debug7*)Unsafe.AsPointer(ref this), Enable);
    }

    public interface Interface : ID3D12Debug6.Interface
    {
        [VtblIndex(10)]
        void SetEnableDriverReportedMessages([NativeTypeName("BOOL")] int Enable);
    }
}
