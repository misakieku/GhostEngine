using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("06B51E0F-A605-4C69-A110-CD6E14B58EEC")]
[NativeTypeName("struct IDxcCompletionString : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCompletionString : IDxcCompletionString.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompletionString);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint>)(lpVtbl[1]))((IDxcCompletionString*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint>)(lpVtbl[2]))((IDxcCompletionString*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetNumCompletionChunks([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint*, int>)(lpVtbl[3]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetCompletionChunkKind([NativeTypeName("unsigned int")] uint chunkNumber, DxcCompletionChunkKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint, DxcCompletionChunkKind*, int>)(lpVtbl[4]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), chunkNumber, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetCompletionChunkText([NativeTypeName("unsigned int")] uint chunkNumber, [NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint, sbyte**, int>)(lpVtbl[5]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), chunkNumber, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetNumCompletionChunks([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(4)]
        HRESULT GetCompletionChunkKind([NativeTypeName("unsigned int")] uint chunkNumber, DxcCompletionChunkKind* pResult);

        [VtblIndex(5)]
        HRESULT GetCompletionChunkText([NativeTypeName("unsigned int")] uint chunkNumber, [NativeTypeName("LPSTR *")] sbyte** pResult);
    }
}
