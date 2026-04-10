using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic"]/*' />
[Guid("4F76B234-3659-4D33-99B0-3B0DB994B564")]
[NativeTypeName("struct IDxcDiagnostic : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcDiagnostic : IDxcDiagnostic.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcDiagnostic);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, Guid*, void**, int>)(lpVtbl[0]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint>)(lpVtbl[1]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint>)(lpVtbl[2]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.FormatDiagnostic"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int FormatDiagnostic(DxcDiagnosticDisplayOptions options, [NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, DxcDiagnosticDisplayOptions, sbyte**, int>)(lpVtbl[3]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), options, pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetSeverity"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSeverity(DxcDiagnosticSeverity* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, DxcDiagnosticSeverity*, int>)(lpVtbl[4]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetLocation(IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetSpelling"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, sbyte**, int>)(lpVtbl[6]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetCategoryText"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetCategoryText([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, sbyte**, int>)(lpVtbl[7]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetNumRanges"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumRanges([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint*, int>)(lpVtbl[8]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetRangeAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetRangeAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint, IDxcSourceRange**, int>)(lpVtbl[9]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), index, pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetNumFixIts"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumFixIts([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint*, int>)(lpVtbl[10]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcDiagnostic.xml' path='doc/member[@name="IDxcDiagnostic.GetFixItAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetFixItAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pReplacementRange, [NativeTypeName("LPSTR *")] sbyte** pText)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint, IDxcSourceRange**, sbyte**, int>)(lpVtbl[11]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), index, pReplacementRange, pText);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int FormatDiagnostic(DxcDiagnosticDisplayOptions options, [NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetSeverity(DxcDiagnosticSeverity* pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetLocation(IDxcSourceLocation** pResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetCategoryText([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetNumRanges([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetRangeAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pResult);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int GetNumFixIts([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetFixItAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pReplacementRange, [NativeTypeName("LPSTR *")] sbyte** pText);
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

        [NativeTypeName("HRESULT (DxcDiagnosticDisplayOptions, LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcDiagnosticDisplayOptions, sbyte**, int> FormatDiagnostic;

        [NativeTypeName("HRESULT (DxcDiagnosticSeverity *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcDiagnosticSeverity*, int> GetSeverity;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetLocation;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetSpelling;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetCategoryText;

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumRanges;

        [NativeTypeName("HRESULT (unsigned int, IDxcSourceRange **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcSourceRange**, int> GetRangeAt;

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumFixIts;

        [NativeTypeName("HRESULT (unsigned int, IDxcSourceRange **, LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcSourceRange**, sbyte**, int> GetFixItAt;
    }
}
