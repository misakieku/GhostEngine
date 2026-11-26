using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("1E06466A-FD8B-45F3-A78F-8A3F76EBB552")]
[NativeTypeName("struct IDxcCodeCompleteResults : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCodeCompleteResults : IDxcCodeCompleteResults.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCodeCompleteResults);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint>)(lpVtbl[1]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint>)(lpVtbl[2]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetNumResults([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint*, int>)(lpVtbl[3]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetResultAt([NativeTypeName("unsigned int")] uint index, IDxcCompletionResult** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCodeCompleteResults*, uint, IDxcCompletionResult**, int>)(lpVtbl[4]))((IDxcCodeCompleteResults*)Unsafe.AsPointer(ref this), index, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetNumResults([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(4)]
        HRESULT GetResultAt([NativeTypeName("unsigned int")] uint index, IDxcCompletionResult** pResult);
    }
}
