using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("A06EB39A-50DA-425B-8C31-4EECD6C270F3")]
[NativeTypeName("struct ID3DDestructionNotifier : IUnknown")]
public unsafe partial struct ID3DDestructionNotifier : ID3DDestructionNotifier.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3DDestructionNotifier);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DDestructionNotifier*, Guid*, void**, int>)(lpVtbl[0]))((ID3DDestructionNotifier*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DDestructionNotifier*, uint>)(lpVtbl[1]))((ID3DDestructionNotifier*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DDestructionNotifier*, uint>)(lpVtbl[2]))((ID3DDestructionNotifier*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT RegisterDestructionCallback([NativeTypeName("PFN_DESTRUCTION_CALLBACK")] delegate* unmanaged[Stdcall]<void*, void> callbackFn, void* pData, uint* pCallbackID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DDestructionNotifier*, delegate* unmanaged[Stdcall]<void*, void>, void*, uint*, int>)(lpVtbl[3]))((ID3DDestructionNotifier*)Unsafe.AsPointer(ref this), callbackFn, pData, pCallbackID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT UnregisterDestructionCallback(uint callbackID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DDestructionNotifier*, uint, int>)(lpVtbl[4]))((ID3DDestructionNotifier*)Unsafe.AsPointer(ref this), callbackID);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT RegisterDestructionCallback([NativeTypeName("PFN_DESTRUCTION_CALLBACK")] delegate* unmanaged[Stdcall]<void*, void> callbackFn, void* pData, uint* pCallbackID);

        [VtblIndex(4)]
        HRESULT UnregisterDestructionCallback(uint callbackID);
    }
}
