using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("C706C811-3663-4BF1-91B9-1E8A7C114AB9")]
[NativeTypeName("struct ID3D12RuntimeValidationControl : IUnknown")]
public unsafe partial struct ID3D12RuntimeValidationControl : ID3D12RuntimeValidationControl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12RuntimeValidationControl);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RuntimeValidationControl*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12RuntimeValidationControl*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RuntimeValidationControl*, uint>)(lpVtbl[1]))((ID3D12RuntimeValidationControl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RuntimeValidationControl*, uint>)(lpVtbl[2]))((ID3D12RuntimeValidationControl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void DisableFailuresFromStricterValidationInAppLocalRuntime([NativeTypeName("BOOL")] int bDisable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12RuntimeValidationControl*, int, void>)(lpVtbl[3]))((ID3D12RuntimeValidationControl*)Unsafe.AsPointer(ref this), bDisable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("BOOL")]
    public int FailuresFromStricterValidationInAppLocalRuntimeDisabled()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RuntimeValidationControl*, int>)(lpVtbl[4]))((ID3D12RuntimeValidationControl*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void DisableFailuresFromStricterValidationInAppLocalRuntime([NativeTypeName("BOOL")] int bDisable);

        [VtblIndex(4)]
        [return: NativeTypeName("BOOL")]
        int FailuresFromStricterValidationInAppLocalRuntimeDisabled();
    }
}
