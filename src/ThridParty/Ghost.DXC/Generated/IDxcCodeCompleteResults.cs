using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcCodeCompleteResults.xml' path='doc/member[@name="IDxcCodeCompleteResults"]/*' />
[Guid("1E06466A-FD8B-45F3-A78F-8A3F76EBB552")]
[NativeTypeName("struct IDxcCodeCompleteResults : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCodeCompleteResults : IDxcCodeCompleteResults.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCodeCompleteResults);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint>)(lpVtbl[1]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint>)(lpVtbl[2]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcCodeCompleteResults.xml' path='doc/member[@name="IDxcCodeCompleteResults.GetNumResults"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumResults([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint*, int>)(lpVtbl[3]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCodeCompleteResults.xml' path='doc/member[@name="IDxcCodeCompleteResults.GetResultAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetResultAt([NativeTypeName("unsigned int")] uint index, IDxcCompletionResult** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint, IDxcCompletionResult**, int>)(lpVtbl[4]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), index, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetNumResults([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetResultAt([NativeTypeName("unsigned int")] uint index, IDxcCompletionResult** pResult);
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

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumResults;

        [NativeTypeName("HRESULT (unsigned int, IDxcCompletionResult **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcCompletionResult**, int> GetResultAt;
    }
}
