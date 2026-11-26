using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("E667AF9F-CD56-4F46-83CE-032E595D70A8")]
[NativeTypeName("struct ID3D12LifetimeOwner : IUnknown")]
public unsafe partial struct ID3D12LifetimeOwner : ID3D12LifetimeOwner.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12LifetimeOwner);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12LifetimeOwner*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12LifetimeOwner*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12LifetimeOwner*, uint>)(lpVtbl[1]))((ID3D12LifetimeOwner*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12LifetimeOwner*, uint>)(lpVtbl[2]))((ID3D12LifetimeOwner*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void LifetimeStateUpdated(D3D12_LIFETIME_STATE NewState)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12LifetimeOwner*, D3D12_LIFETIME_STATE, void>)(lpVtbl[3]))((ID3D12LifetimeOwner*)Unsafe.AsPointer(ref this), NewState);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void LifetimeStateUpdated(D3D12_LIFETIME_STATE NewState);
    }
}
