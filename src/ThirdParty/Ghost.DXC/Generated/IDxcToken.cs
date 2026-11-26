using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("7F90B9FF-A275-4932-97D8-3CFD234482A2")]
[NativeTypeName("struct IDxcToken : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcToken : IDxcToken.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcToken);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, Guid*, void**, int>)(lpVtbl[0]))((IDxcToken*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, uint>)(lpVtbl[1]))((IDxcToken*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, uint>)(lpVtbl[2]))((IDxcToken*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetKind(DxcTokenKind* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, DxcTokenKind*, int>)(lpVtbl[3]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetLocation(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetExtent(IDxcSourceRange** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, IDxcSourceRange**, int>)(lpVtbl[5]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, sbyte**, int>)(lpVtbl[6]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetKind(DxcTokenKind* pValue);

        [VtblIndex(4)]
        HRESULT GetLocation(IDxcSourceLocation** pValue);

        [VtblIndex(5)]
        HRESULT GetExtent(IDxcSourceRange** pValue);

        [VtblIndex(6)]
        HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pValue);
    }
}
