using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilDebugInfoFactory.xml' path='doc/member[@name="IDxcPixDxilDebugInfoFactory"]/*' />
[Guid("9C2A040D-8068-44EC-8C68-8BFEF1B43789")]
[NativeTypeName("struct IDxcPixDxilDebugInfoFactory : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilDebugInfoFactory : IDxcPixDxilDebugInfoFactory.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilDebugInfoFactory);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfoFactory*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilDebugInfoFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfoFactory*, uint>)(lpVtbl[1]))((IDxcPixDxilDebugInfoFactory*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfoFactory*, uint>)(lpVtbl[2]))((IDxcPixDxilDebugInfoFactory*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilDebugInfoFactory.xml' path='doc/member[@name="IDxcPixDxilDebugInfoFactory.NewDxcPixDxilDebugInfo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int NewDxcPixDxilDebugInfo(IDxcPixDxilDebugInfo** ppDxilDebugInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfoFactory*, IDxcPixDxilDebugInfo**, int>)(lpVtbl[3]))((IDxcPixDxilDebugInfoFactory*)Unsafe.AsPointer(ref this), ppDxilDebugInfo);
    }

    /// <include file='IDxcPixDxilDebugInfoFactory.xml' path='doc/member[@name="IDxcPixDxilDebugInfoFactory.NewDxcPixCompilationInfo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int NewDxcPixCompilationInfo(IDxcPixCompilationInfo** ppCompilationInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfoFactory*, IDxcPixCompilationInfo**, int>)(lpVtbl[4]))((IDxcPixDxilDebugInfoFactory*)Unsafe.AsPointer(ref this), ppCompilationInfo);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int NewDxcPixDxilDebugInfo(IDxcPixDxilDebugInfo** ppDxilDebugInfo);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int NewDxcPixCompilationInfo(IDxcPixCompilationInfo** ppCompilationInfo);
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged, Interface
    {
        [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("HRESULT (IDxcPixDxilDebugInfo **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixDxilDebugInfo**, int> NewDxcPixDxilDebugInfo;

        [NativeTypeName("HRESULT (IDxcPixCompilationInfo **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixCompilationInfo**, int> NewDxcPixCompilationInfo;
    }
}
