using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("D6F12DD6-76FB-406E-8961-4296EEFC0409")]
[NativeTypeName("struct ID3D12ProtectedResourceSession1 : ID3D12ProtectedResourceSession")]
public unsafe partial struct ID3D12ProtectedResourceSession1 : ID3D12ProtectedResourceSession1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ProtectedResourceSession1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, uint>)(lpVtbl[1]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, uint>)(lpVtbl[2]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, char*, int>)(lpVtbl[6]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetStatusFence([NativeTypeName("const IID &")] Guid* riid, void** ppFence)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, Guid*, void**, int>)(lpVtbl[8]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this), riid, ppFence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public D3D12_PROTECTED_SESSION_STATUS GetSessionStatus()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, D3D12_PROTECTED_SESSION_STATUS>)(lpVtbl[9]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public D3D12_PROTECTED_RESOURCE_SESSION_DESC GetDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, D3D12_PROTECTED_RESOURCE_SESSION_DESC>)(lpVtbl[10]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public D3D12_PROTECTED_RESOURCE_SESSION_DESC1 GetDesc1()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ProtectedResourceSession1*, D3D12_PROTECTED_RESOURCE_SESSION_DESC1>)(lpVtbl[11]))((ID3D12ProtectedResourceSession1*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12ProtectedResourceSession.Interface
    {
        [VtblIndex(11)]
        D3D12_PROTECTED_RESOURCE_SESSION_DESC1 GetDesc1();
    }
}
