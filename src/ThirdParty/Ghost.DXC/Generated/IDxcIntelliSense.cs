using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("B1F99513-46D6-4112-8169-DD0D6053F17D")]
[NativeTypeName("struct IDxcIntelliSense : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcIntelliSense : IDxcIntelliSense.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcIntelliSense);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, Guid*, void**, int>)(lpVtbl[0]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, uint>)(lpVtbl[1]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, uint>)(lpVtbl[2]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT CreateIndex(IDxcIndex** index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcIndex**, int>)(lpVtbl[3]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetNullLocation(IDxcSourceLocation** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), location);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetNullRange(IDxcSourceRange** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceRange**, int>)(lpVtbl[5]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), location);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetRange(IDxcSourceLocation* start, IDxcSourceLocation* end, IDxcSourceRange** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceLocation*, IDxcSourceLocation*, IDxcSourceRange**, int>)(lpVtbl[6]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), start, end, location);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDefaultDiagnosticDisplayOptions(DxcDiagnosticDisplayOptions* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, DxcDiagnosticDisplayOptions*, int>)(lpVtbl[7]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetDefaultEditingTUOptions(DxcTranslationUnitFlags* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, DxcTranslationUnitFlags*, int>)(lpVtbl[8]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT CreateUnsavedFile([NativeTypeName("LPCSTR")] sbyte* fileName, [NativeTypeName("LPCSTR")] sbyte* contents, [NativeTypeName("unsigned int")] uint contentLength, IDxcUnsavedFile** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, sbyte*, sbyte*, uint, IDxcUnsavedFile**, int>)(lpVtbl[9]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), fileName, contents, contentLength, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT CreateIndex(IDxcIndex** index);

        [VtblIndex(4)]
        HRESULT GetNullLocation(IDxcSourceLocation** location);

        [VtblIndex(5)]
        HRESULT GetNullRange(IDxcSourceRange** location);

        [VtblIndex(6)]
        HRESULT GetRange(IDxcSourceLocation* start, IDxcSourceLocation* end, IDxcSourceRange** location);

        [VtblIndex(7)]
        HRESULT GetDefaultDiagnosticDisplayOptions(DxcDiagnosticDisplayOptions* pValue);

        [VtblIndex(8)]
        HRESULT GetDefaultEditingTUOptions(DxcTranslationUnitFlags* pValue);

        [VtblIndex(9)]
        HRESULT CreateUnsavedFile([NativeTypeName("LPCSTR")] sbyte* fileName, [NativeTypeName("LPCSTR")] sbyte* contents, [NativeTypeName("unsigned int")] uint contentLength, IDxcUnsavedFile** pResult);
    }
}
