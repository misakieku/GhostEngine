using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcLinker.xml' path='doc/member[@name="IDxcLinker"]/*' />
[Guid("F1B5BE2A-62DD-4327-A1C2-42AC1E1E78E6")]
[NativeTypeName("struct IDxcLinker : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcLinker : IDxcLinker.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcLinker);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, Guid*, void**, int>)(lpVtbl[0]))((IDxcLinker*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, uint>)(lpVtbl[1]))((IDxcLinker*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, uint>)(lpVtbl[2]))((IDxcLinker*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcLinker.xml' path='doc/member[@name="IDxcLinker.RegisterLibrary"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterLibrary([NativeTypeName("LPCWSTR")] char* pLibName, IDxcBlob* pLib)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, char*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcLinker*)Unsafe.AsPointer(ref this), pLibName, pLib);
    }

    /// <include file='IDxcLinker.xml' path='doc/member[@name="IDxcLinker.Link"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int Link([NativeTypeName("LPCWSTR")] char* pEntryName, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("const LPCWSTR *")] char** pLibNames, [NativeTypeName("UINT32")] uint libCount, [NativeTypeName("const LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, char*, char*, char**, uint, char**, uint, IDxcOperationResult**, int>)(lpVtbl[4]))((IDxcLinker*)Unsafe.AsPointer(ref this), pEntryName, pTargetProfile, pLibNames, libCount, pArguments, argCount, ppResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int RegisterLibrary([NativeTypeName("LPCWSTR")] char* pLibName, IDxcBlob* pLib);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int Link([NativeTypeName("LPCWSTR")] char* pEntryName, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("const LPCWSTR *")] char** pLibNames, [NativeTypeName("UINT32")] uint libCount, [NativeTypeName("const LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, IDxcOperationResult** ppResult);
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

        [NativeTypeName("HRESULT (LPCWSTR, IDxcBlob *)")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, IDxcBlob*, int> RegisterLibrary;

        [NativeTypeName("HRESULT (LPCWSTR, LPCWSTR, const LPCWSTR *, UINT32, const LPCWSTR *, UINT32, IDxcOperationResult **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, char*, char**, uint, char**, uint, IDxcOperationResult**, int> Link;
    }
}
