using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("D67447BC-4D6C-4CA5-A2A8-DEEEA4C145C4")]
[NativeTypeName("struct ID3D12StateObjectDatabaseCapture : IUnknown")]
public unsafe partial struct ID3D12StateObjectDatabaseCapture : ID3D12StateObjectDatabaseCapture.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectDatabaseCapture);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseCapture*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectDatabaseCapture*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseCapture*, uint>)(lpVtbl[1]))((ID3D12StateObjectDatabaseCapture*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseCapture*, uint>)(lpVtbl[2]))((ID3D12StateObjectDatabaseCapture*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableCapture([NativeTypeName("BOOL")] int Enable, [NativeTypeName("LPCWSTR")] char* pDatabaseFile)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseCapture*, int, char*, void>)(lpVtbl[3]))((ID3D12StateObjectDatabaseCapture*)Unsafe.AsPointer(ref this), Enable, pDatabaseFile);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void EnableCapture([NativeTypeName("BOOL")] int Enable, [NativeTypeName("LPCWSTR")] char* pDatabaseFile);
    }
}
