using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcCompletionResult.xml' path='doc/member[@name="IDxcCompletionResult"]/*' />
[Guid("943C0588-22D0-4784-86FC-701F802AC2B6")]
[NativeTypeName("struct IDxcCompletionResult : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCompletionResult : IDxcCompletionResult.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompletionResult);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, uint>)(lpVtbl[1]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, uint>)(lpVtbl[2]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcCompletionResult.xml' path='doc/member[@name="IDxcCompletionResult.GetCursorKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetCursorKind(DxcCursorKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, DxcCursorKind*, int>)(lpVtbl[3]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCompletionResult.xml' path='doc/member[@name="IDxcCompletionResult.GetCompletionString"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetCompletionString(IDxcCompletionString** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, IDxcCompletionString**, int>)(lpVtbl[4]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetCursorKind(DxcCursorKind* pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetCompletionString(IDxcCompletionString** pResult);
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

        [NativeTypeName("HRESULT (DxcCursorKind *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcCursorKind*, int> GetCursorKind;

        [NativeTypeName("HRESULT (IDxcCompletionString **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCompletionString**, int> GetCompletionString;
    }
}
