using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("F1359B36-A53F-4E81-B514-B6B84122A13F")]
[NativeTypeName("struct IDxcSourceRange : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcSourceRange : IDxcSourceRange.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcSourceRange);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, Guid*, void**, int>)(lpVtbl[0]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint>)(lpVtbl[1]))((IDxcSourceRange*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint>)(lpVtbl[2]))((IDxcSourceRange*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT IsNull([NativeTypeName("BOOL *")] int* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, int*, int>)(lpVtbl[3]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetStart(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetEnd(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetOffsets([NativeTypeName("unsigned int *")] uint* startOffset, [NativeTypeName("unsigned int *")] uint* endOffset)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceRange*, uint*, uint*, int>)(lpVtbl[6]))((IDxcSourceRange*)Unsafe.AsPointer(ref this), startOffset, endOffset);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT IsNull([NativeTypeName("BOOL *")] int* pValue);

        [VtblIndex(4)]
        HRESULT GetStart(IDxcSourceLocation** pValue);

        [VtblIndex(5)]
        HRESULT GetEnd(IDxcSourceLocation** pValue);

        [VtblIndex(6)]
        HRESULT GetOffsets([NativeTypeName("unsigned int *")] uint* startOffset, [NativeTypeName("unsigned int *")] uint* endOffset);
    }
}
