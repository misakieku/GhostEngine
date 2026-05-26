using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcCompiler.xml' path='doc/member[@name="IDxcCompiler"]/*' />
[Guid("8C210BF3-011F-4422-8D70-6F9ACB8DB617")]
[NativeTypeName("struct IDxcCompiler : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCompiler : IDxcCompiler.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompiler);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompiler*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, uint>)(lpVtbl[1]))((IDxcCompiler*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, uint>)(lpVtbl[2]))((IDxcCompiler*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcCompiler.xml' path='doc/member[@name="IDxcCompiler.Compile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int Compile(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, IDxcBlob*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int>)(lpVtbl[3]))((IDxcCompiler*)Unsafe.AsPointer(ref this), pSource, pSourceName, pEntryPoint, pTargetProfile, pArguments, argCount, pDefines, defineCount, pIncludeHandler, ppResult);
    }

    /// <include file='IDxcCompiler.xml' path='doc/member[@name="IDxcCompiler.Preprocess"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int Preprocess(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, IDxcBlob*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int>)(lpVtbl[4]))((IDxcCompiler*)Unsafe.AsPointer(ref this), pSource, pSourceName, pArguments, argCount, pDefines, defineCount, pIncludeHandler, ppResult);
    }

    /// <include file='IDxcCompiler.xml' path='doc/member[@name="IDxcCompiler.Disassemble"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int Disassemble(IDxcBlob* pSource, IDxcBlobEncoding** ppDisassembly)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompiler*, IDxcBlob*, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcCompiler*)Unsafe.AsPointer(ref this), pSource, ppDisassembly);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int Compile(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int Preprocess(IDxcBlob* pSource, [NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcIncludeHandler* pIncludeHandler, IDxcOperationResult** ppResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int Disassemble(IDxcBlob* pSource, IDxcBlobEncoding** ppDisassembly);
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

        [NativeTypeName("HRESULT (IDxcBlob *, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR *, UINT32, const DxcDefine *, UINT32, IDxcIncludeHandler *, IDxcOperationResult **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int> Compile;

        [NativeTypeName("HRESULT (IDxcBlob *, LPCWSTR, LPCWSTR *, UINT32, const DxcDefine *, UINT32, IDxcIncludeHandler *, IDxcOperationResult **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, char*, char**, uint, DxcDefine*, uint, IDxcIncludeHandler*, IDxcOperationResult**, int> Preprocess;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlobEncoding**, int> Disassemble;
    }
}
