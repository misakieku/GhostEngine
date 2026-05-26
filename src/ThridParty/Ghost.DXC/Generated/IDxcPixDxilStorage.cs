using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage"]/*' />
[Guid("74D522F5-16C4-40CB-867B-4B4149E3DB0E")]
[NativeTypeName("struct IDxcPixDxilStorage : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilStorage : IDxcPixDxilStorage.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilStorage);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, uint>)(lpVtbl[1]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, uint>)(lpVtbl[2]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage.AccessField"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int AccessField([NativeTypeName("LPCWSTR")] char* Name, IDxcPixDxilStorage** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, char*, IDxcPixDxilStorage**, int>)(lpVtbl[3]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this), Name, ppResult);
    }

    /// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage.Index"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int Index([NativeTypeName("DWORD")] uint Index, IDxcPixDxilStorage** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, uint, IDxcPixDxilStorage**, int>)(lpVtbl[4]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this), Index, ppResult);
    }

    /// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage.GetRegisterNumber"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetRegisterNumber([NativeTypeName("DWORD *")] uint* pRegNum)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, uint*, int>)(lpVtbl[5]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this), pRegNum);
    }

    /// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage.GetIsAlive"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetIsAlive()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, int>)(lpVtbl[6]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilStorage.xml' path='doc/member[@name="IDxcPixDxilStorage.GetType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetType(IDxcPixType** ppType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilStorage*, IDxcPixType**, int>)(lpVtbl[7]))((IDxcPixDxilStorage*)Unsafe.AsPointer(ref this), ppType);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int AccessField([NativeTypeName("LPCWSTR")] char* Name, IDxcPixDxilStorage** ppResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int Index([NativeTypeName("DWORD")] uint Index, IDxcPixDxilStorage** ppResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetRegisterNumber([NativeTypeName("DWORD *")] uint* pRegNum);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetIsAlive();

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetType(IDxcPixType** ppType);
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

        [NativeTypeName("HRESULT (LPCWSTR, IDxcPixDxilStorage **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, IDxcPixDxilStorage**, int> AccessField;

        [NativeTypeName("HRESULT (DWORD, IDxcPixDxilStorage **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPixDxilStorage**, int> Index;

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetRegisterNumber;

        [NativeTypeName("HRESULT () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int> GetIsAlive;

        [NativeTypeName("HRESULT (IDxcPixType **) __attribute__((stdcall))")]
        public new delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixType**, int> GetType;
    }
}
