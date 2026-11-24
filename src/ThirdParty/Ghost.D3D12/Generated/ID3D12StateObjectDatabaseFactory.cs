using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("F5B066F0-648A-4611-BD41-27FD0948B9EB")]
[NativeTypeName("struct ID3D12StateObjectDatabaseFactory : IUnknown")]
public unsafe partial struct ID3D12StateObjectDatabaseFactory : ID3D12StateObjectDatabaseFactory.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectDatabaseFactory);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectDatabaseFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory*, uint>)(lpVtbl[1]))((ID3D12StateObjectDatabaseFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory*, uint>)(lpVtbl[2]))((ID3D12StateObjectDatabaseFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int CreateStateObjectDatabaseFromFile([NativeTypeName("LPCWSTR")] char* pDatabaseFile, D3D12_STATE_OBJECT_DATABASE_FLAGS flags, [NativeTypeName("const IID &")] Guid* riid, void** ppvStateObjectDatabase)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory*, char*, D3D12_STATE_OBJECT_DATABASE_FLAGS, Guid*, void**, int>)(lpVtbl[3]))((ID3D12StateObjectDatabaseFactory*)Unsafe.AsPointer(ref this), pDatabaseFile, flags, riid, ppvStateObjectDatabase);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int CreateStateObjectDatabaseFromFile([NativeTypeName("LPCWSTR")] char* pDatabaseFile, D3D12_STATE_OBJECT_DATABASE_FLAGS flags, [NativeTypeName("const IID &")] Guid* riid, void** ppvStateObjectDatabase);
    }
}
