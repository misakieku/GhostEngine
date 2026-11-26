using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("A005A9D9-B8BB-4594-B5C9-0E633BEC4D37")]
[NativeTypeName("struct IDxcCompiler2 : IDxcCompiler")]
[NativeInheritance("IDxcCompiler")]
public unsafe partial struct IDxcCompiler2 : IDxcCompiler2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompiler2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompiler2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, uint>)(lpVtbl[1]))((IDxcCompiler2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, uint>)(lpVtbl[2]))((IDxcCompiler2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT Compile(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, IDxcBlob*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int>)(lpVtbl[3]))((IDxcCompiler2*)Unsafe.AsPointer(ref this), pSource, pSourceName, pEntryPoint, pTargetProfile, pArguments, argCount, pDefines, defineCount, pIncludeHandler, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT Preprocess(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, IDxcBlob*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int>)(lpVtbl[4]))((IDxcCompiler2*)Unsafe.AsPointer(ref this), pSource, pSourceName, pArguments, argCount, pDefines, defineCount, pIncludeHandler, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT Disassemble(IDxcBlob* pSource, IDxcBlobEncoding** ppDisassembly)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, IDxcBlob*, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcCompiler2*)Unsafe.AsPointer(ref this), pSource, ppDisassembly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT CompileWithDebug(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult, [NativeTypeName("LPWSTR *")] char** ppDebugBlobName, IDxcBlob** ppDebugBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler2*, IDxcBlob*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, char**, IDxcBlob**, int>)(lpVtbl[6]))((IDxcCompiler2*)Unsafe.AsPointer(ref this), pSource, pSourceName, pEntryPoint, pTargetProfile, pArguments, argCount, pDefines, defineCount, pIncludeHandler, ppResult, ppDebugBlobName, ppDebugBlob);
    }

    public interface Interface : IDxcCompiler.Interface
    {
        [VtblIndex(6)]
        HRESULT CompileWithDebug(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult, [NativeTypeName("LPWSTR *")] char** ppDebugBlobName, IDxcBlob** ppDebugBlob);
    }
}
