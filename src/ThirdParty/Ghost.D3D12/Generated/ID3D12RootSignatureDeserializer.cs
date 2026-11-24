using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("34AB647B-3CC8-46AC-841B-C0965645C046")]
[NativeTypeName("struct ID3D12RootSignatureDeserializer : IUnknown")]
public unsafe partial struct ID3D12RootSignatureDeserializer : ID3D12RootSignatureDeserializer.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12RootSignatureDeserializer);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignatureDeserializer*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12RootSignatureDeserializer*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignatureDeserializer*, uint>)(lpVtbl[1]))((ID3D12RootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignatureDeserializer*, uint>)(lpVtbl[2]))((ID3D12RootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("const D3D12_ROOT_SIGNATURE_DESC *")]
    public D3D12_ROOT_SIGNATURE_DESC* GetRootSignatureDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignatureDeserializer*, D3D12_ROOT_SIGNATURE_DESC*>)(lpVtbl[3]))((ID3D12RootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("const D3D12_ROOT_SIGNATURE_DESC *")]
        D3D12_ROOT_SIGNATURE_DESC* GetRootSignatureDesc();
    }
}
