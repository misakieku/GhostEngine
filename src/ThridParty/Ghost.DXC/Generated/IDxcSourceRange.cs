using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcSourceRange.xml' path='doc/member[@name="IDxcSourceRange"]/*' />
[Guid("F1359B36-A53F-4E81-B514-B6B84122A13F")]
[NativeTypeName("struct IDxcSourceRange : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcSourceRange : IDxcSourceRange.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcSourceRange);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, Guid*, void**, int>)(lpVtbl[0]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint>)(lpVtbl[1]))((IDxcSourceRange*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint>)(lpVtbl[2]))((IDxcSourceRange*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcSourceRange.xml' path='doc/member[@name="IDxcSourceRange.IsNull"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int IsNull([NativeTypeName("BOOL *")] int* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, int*, int>)(lpVtbl[3]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcSourceRange.xml' path='doc/member[@name="IDxcSourceRange.GetStart"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetStart(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcSourceRange.xml' path='doc/member[@name="IDxcSourceRange.GetEnd"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetEnd(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcSourceRange.xml' path='doc/member[@name="IDxcSourceRange.GetOffsets"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetOffsets([NativeTypeName("unsigned int *")] uint* startOffset, [NativeTypeName("unsigned int *")] uint* endOffset)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint*, uint*, int>)(lpVtbl[6]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), startOffset, endOffset);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int IsNull([NativeTypeName("BOOL *")] int* pValue);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetStart(IDxcSourceLocation** pValue);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetEnd(IDxcSourceLocation** pValue);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetOffsets([NativeTypeName("unsigned int *")] uint* startOffset, [NativeTypeName("unsigned int *")] uint* endOffset);
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

        [NativeTypeName("HRESULT (BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> IsNull;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetStart;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetEnd;

        [NativeTypeName("HRESULT (unsigned int *, unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, uint*, int> GetOffsets;
    }
}
