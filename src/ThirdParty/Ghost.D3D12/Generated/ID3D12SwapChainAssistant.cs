using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("F1DF64B6-57FD-49CD-8807-C0EB88B45C8F")]
[NativeTypeName("struct ID3D12SwapChainAssistant : IUnknown")]
public unsafe partial struct ID3D12SwapChainAssistant : ID3D12SwapChainAssistant.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12SwapChainAssistant);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, uint>)(lpVtbl[1]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, uint>)(lpVtbl[2]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public LUID GetLUID()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, LUID>)(lpVtbl[3]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetSwapChainObject([NativeTypeName("const IID &")] Guid* riid, void** ppv)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, Guid*, void**, int>)(lpVtbl[4]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this), riid, ppv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetCurrentResourceAndCommandQueue([NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource, [NativeTypeName("const IID &")] Guid* riidQueue, void** ppvQueue)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, Guid*, void**, Guid*, void**, int>)(lpVtbl[5]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this), riidResource, ppvResource, riidQueue, ppvQueue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT InsertImplicitSync()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SwapChainAssistant*, int>)(lpVtbl[6]))((ID3D12SwapChainAssistant*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        LUID GetLUID();

        [VtblIndex(4)]
        HRESULT GetSwapChainObject([NativeTypeName("const IID &")] Guid* riid, void** ppv);

        [VtblIndex(5)]
        HRESULT GetCurrentResourceAndCommandQueue([NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource, [NativeTypeName("const IID &")] Guid* riidQueue, void** ppvQueue);

        [VtblIndex(6)]
        HRESULT InsertImplicitSync();
    }
}
