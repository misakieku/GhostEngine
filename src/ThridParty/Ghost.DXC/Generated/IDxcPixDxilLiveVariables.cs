using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilLiveVariables.xml' path='doc/member[@name="IDxcPixDxilLiveVariables"]/*' />
[Guid("C59D302F-34A2-4FE5-9646-32CE7A52D03F")]
[NativeTypeName("struct IDxcPixDxilLiveVariables : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilLiveVariables : IDxcPixDxilLiveVariables.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilLiveVariables);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, uint>)(lpVtbl[1]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, uint>)(lpVtbl[2]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilLiveVariables.xml' path='doc/member[@name="IDxcPixDxilLiveVariables.GetCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetCount([NativeTypeName("DWORD *")] uint* dwSize)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, uint*, int>)(lpVtbl[3]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this), dwSize);
    }

    /// <include file='IDxcPixDxilLiveVariables.xml' path='doc/member[@name="IDxcPixDxilLiveVariables.GetVariableByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetVariableByIndex([NativeTypeName("DWORD")] uint Index, IDxcPixVariable** ppVariable)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, uint, IDxcPixVariable**, int>)(lpVtbl[4]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this), Index, ppVariable);
    }

    /// <include file='IDxcPixDxilLiveVariables.xml' path='doc/member[@name="IDxcPixDxilLiveVariables.GetVariableByName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetVariableByName([NativeTypeName("LPCWSTR")] char* Name, IDxcPixVariable** ppVariable)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilLiveVariables*, char*, IDxcPixVariable**, int>)(lpVtbl[5]))((IDxcPixDxilLiveVariables*)Unsafe.AsPointer(ref this), Name, ppVariable);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetCount([NativeTypeName("DWORD *")] uint* dwSize);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetVariableByIndex([NativeTypeName("DWORD")] uint Index, IDxcPixVariable** ppVariable);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetVariableByName([NativeTypeName("LPCWSTR")] char* Name, IDxcPixVariable** ppVariable);
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

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetCount;

        [NativeTypeName("HRESULT (DWORD, IDxcPixVariable **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPixVariable**, int> GetVariableByIndex;

        [NativeTypeName("HRESULT (LPCWSTR, IDxcPixVariable **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, IDxcPixVariable**, int> GetVariableByName;
    }
}
