using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("68A6585D-0AC7-4FA4-BA33-E3BD442F2833")]
[NativeTypeName("struct ID3D12StateObjectDatabaseFactory1 : ID3D12StateObjectDatabaseFactory")]
public unsafe partial struct ID3D12StateObjectDatabaseFactory1 : ID3D12StateObjectDatabaseFactory1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectDatabaseFactory1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectDatabaseFactory1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory1*, uint>)(lpVtbl[1]))((ID3D12StateObjectDatabaseFactory1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory1*, uint>)(lpVtbl[2]))((ID3D12StateObjectDatabaseFactory1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT CreateStateObjectDatabaseFromFile([NativeTypeName("LPCWSTR")] char* pDatabaseFile, D3D12_STATE_OBJECT_DATABASE_FLAGS flags, [NativeTypeName("const IID &")] Guid* riid, void** ppvStateObjectDatabase)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory1*, char*, D3D12_STATE_OBJECT_DATABASE_FLAGS, Guid*, void**, int>)(lpVtbl[3]))((ID3D12StateObjectDatabaseFactory1*)Unsafe.AsPointer(ref this), pDatabaseFile, flags, riid, ppvStateObjectDatabase);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT CreateStateObjectDatabaseFromFile1([NativeTypeName("LPCWSTR")] char* pDatabaseFile, [NativeTypeName("const D3D12_STATE_OBJECT_DATABASE_DESC *")] D3D12_STATE_OBJECT_DATABASE_DESC* pDesc, ID3D12InfoQueue* pInfoQueue, [NativeTypeName("const IID &")] Guid* riid, void** ppvStateObjectDatabase)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabaseFactory1*, char*, D3D12_STATE_OBJECT_DATABASE_DESC*, ID3D12InfoQueue*, Guid*, void**, int>)(lpVtbl[4]))((ID3D12StateObjectDatabaseFactory1*)Unsafe.AsPointer(ref this), pDatabaseFile, pDesc, pInfoQueue, riid, ppvStateObjectDatabase);
    }

    public interface Interface : ID3D12StateObjectDatabaseFactory.Interface
    {
        [VtblIndex(4)]
        HRESULT CreateStateObjectDatabaseFromFile1([NativeTypeName("LPCWSTR")] char* pDatabaseFile, [NativeTypeName("const D3D12_STATE_OBJECT_DATABASE_DESC *")] D3D12_STATE_OBJECT_DATABASE_DESC* pDesc, ID3D12InfoQueue* pInfoQueue, [NativeTypeName("const IID &")] Guid* riid, void** ppvStateObjectDatabase);
    }
}
