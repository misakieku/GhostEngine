using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("4F76B234-3659-4D33-99B0-3B0DB994B564")]
[NativeTypeName("struct IDxcDiagnostic : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcDiagnostic : IDxcDiagnostic.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcDiagnostic);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, Guid*, void**, int>)(lpVtbl[0]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint>)(lpVtbl[1]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint>)(lpVtbl[2]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT FormatDiagnostic(DxcDiagnosticDisplayOptions options, [NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, DxcDiagnosticDisplayOptions, sbyte**, int>)(lpVtbl[3]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), options, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetSeverity(DxcDiagnosticSeverity* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, DxcDiagnosticSeverity*, int>)(lpVtbl[4]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetLocation(IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, sbyte**, int>)(lpVtbl[6]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetCategoryText([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, sbyte**, int>)(lpVtbl[7]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetNumRanges([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint*, int>)(lpVtbl[8]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT GetRangeAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint, IDxcSourceRange**, int>)(lpVtbl[9]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), index, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT GetNumFixIts([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint*, int>)(lpVtbl[10]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public HRESULT GetFixItAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pReplacementRange, [NativeTypeName("LPSTR *")] sbyte** pText)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcDiagnostic*, uint, IDxcSourceRange**, sbyte**, int>)(lpVtbl[11]))((IDxcDiagnostic*)Unsafe.AsPointer(ref this), index, pReplacementRange, pText);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT FormatDiagnostic(DxcDiagnosticDisplayOptions options, [NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        HRESULT GetSeverity(DxcDiagnosticSeverity* pResult);

        [VtblIndex(5)]
        HRESULT GetLocation(IDxcSourceLocation** pResult);

        [VtblIndex(6)]
        HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(7)]
        HRESULT GetCategoryText([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(8)]
        HRESULT GetNumRanges([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(9)]
        HRESULT GetRangeAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pResult);

        [VtblIndex(10)]
        HRESULT GetNumFixIts([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(11)]
        HRESULT GetFixItAt([NativeTypeName("unsigned int")] uint index, IDxcSourceRange** pReplacementRange, [NativeTypeName("LPSTR *")] sbyte** pText);
    }
}
