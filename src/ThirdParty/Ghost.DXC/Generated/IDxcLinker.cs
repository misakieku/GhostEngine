using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("F1B5BE2A-62DD-4327-A1C2-42AC1E1E78E6")]
[NativeTypeName("struct IDxcLinker : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcLinker : IDxcLinker.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcLinker);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, Guid*, void**, int>)(lpVtbl[0]))((IDxcLinker*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, uint>)(lpVtbl[1]))((IDxcLinker*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, uint>)(lpVtbl[2]))((IDxcLinker*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT RegisterLibrary([NativeTypeName("LPCWSTR")] char* pLibName, IDxcBlob* pLib)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, char*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcLinker*)Unsafe.AsPointer(ref this), pLibName, pLib);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT Link([NativeTypeName("LPCWSTR")] char* pEntryName, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("const LPCWSTR *")] char** pLibNames, [NativeTypeName("UINT32")] uint libCount, [NativeTypeName("const LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLinker*, char*, char*, char**, uint, char**, uint, IDxcOperationResult**, int>)(lpVtbl[4]))((IDxcLinker*)Unsafe.AsPointer(ref this), pEntryName, pTargetProfile, pLibNames, libCount, pArguments, argCount, ppResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT RegisterLibrary([NativeTypeName("LPCWSTR")] char* pLibName, IDxcBlob* pLib);

        [VtblIndex(4)]
        HRESULT Link([NativeTypeName("LPCWSTR")] char* pEntryName, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("const LPCWSTR *")] char** pLibNames, [NativeTypeName("UINT32")] uint libCount, [NativeTypeName("const LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, IDxcOperationResult** ppResult);
    }
}
