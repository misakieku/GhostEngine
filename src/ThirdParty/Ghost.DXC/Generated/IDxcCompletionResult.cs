using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("943C0588-22D0-4784-86FC-701F802AC2B6")]
[NativeTypeName("struct IDxcCompletionResult : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCompletionResult : IDxcCompletionResult.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompletionResult);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, uint>)(lpVtbl[1]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, uint>)(lpVtbl[2]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetCursorKind(DxcCursorKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, DxcCursorKind*, int>)(lpVtbl[3]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetCompletionString(IDxcCompletionString** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionResult*, IDxcCompletionString**, int>)(lpVtbl[4]))((IDxcCompletionResult*)Unsafe.AsPointer(ref this), pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetCursorKind(DxcCursorKind* pResult);

        [VtblIndex(4)]
        HRESULT GetCompletionString(IDxcCompletionString** pResult);
    }
}
