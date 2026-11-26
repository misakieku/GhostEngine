using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("E6C9647E-9D6A-4C3B-B94C-524B5A6C343D")]
[NativeTypeName("struct IDxcPdbUtils : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPdbUtils : IDxcPdbUtils.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPdbUtils);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint>)(lpVtbl[1]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint>)(lpVtbl[2]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT Load(IDxcBlob* pPdbOrDxil)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pPdbOrDxil);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[4]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetSourceName([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[6]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[7]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetFlag([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[8]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT GetArgCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[9]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT GetArg([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[10]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public HRESULT GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[11]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT GetArgPair([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pName, [NativeTypeName("BSTR *")] char** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, char**, int>)(lpVtbl[12]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pName, pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[13]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public HRESULT GetDefine([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[14]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT GetTargetProfile([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[15]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT GetEntryPoint([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[16]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public HRESULT GetMainFileName([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[17]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public HRESULT GetHash(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob**, int>)(lpVtbl[18]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public HRESULT GetName([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[19]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("BOOL")]
    public int IsFullPDB()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, int>)(lpVtbl[20]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public HRESULT GetFullPDB(IDxcBlob** ppFullPDB)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob**, int>)(lpVtbl[21]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppFullPDB);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public HRESULT GetVersionInfo(IDxcVersionInfo** ppVersionInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcVersionInfo**, int>)(lpVtbl[22]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppVersionInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public HRESULT SetCompiler(IDxcCompiler3* pCompiler)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcCompiler3*, int>)(lpVtbl[23]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCompiler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    public HRESULT CompileForFullPDB(IDxcResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcResult**, int>)(lpVtbl[24]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public HRESULT OverrideArgs(DxcArgPair* pArgPairs, [NativeTypeName("UINT32")] uint uNumArgPairs)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, DxcArgPair*, uint, int>)(lpVtbl[25]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pArgPairs, uNumArgPairs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    public HRESULT OverrideRootSignature([NativeTypeName("const WCHAR *")] char* pRootSignature)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char*, int>)(lpVtbl[26]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pRootSignature);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT Load(IDxcBlob* pPdbOrDxil);

        [VtblIndex(4)]
        HRESULT GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(5)]
        HRESULT GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult);

        [VtblIndex(6)]
        HRESULT GetSourceName([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(7)]
        HRESULT GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(8)]
        HRESULT GetFlag([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(9)]
        HRESULT GetArgCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(10)]
        HRESULT GetArg([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(11)]
        HRESULT GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(12)]
        HRESULT GetArgPair([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pName, [NativeTypeName("BSTR *")] char** pValue);

        [VtblIndex(13)]
        HRESULT GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(14)]
        HRESULT GetDefine([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(15)]
        HRESULT GetTargetProfile([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(16)]
        HRESULT GetEntryPoint([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(17)]
        HRESULT GetMainFileName([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(18)]
        HRESULT GetHash(IDxcBlob** ppResult);

        [VtblIndex(19)]
        HRESULT GetName([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(20)]
        [return: NativeTypeName("BOOL")]
        int IsFullPDB();

        [VtblIndex(21)]
        HRESULT GetFullPDB(IDxcBlob** ppFullPDB);

        [VtblIndex(22)]
        HRESULT GetVersionInfo(IDxcVersionInfo** ppVersionInfo);

        [VtblIndex(23)]
        HRESULT SetCompiler(IDxcCompiler3* pCompiler);

        [VtblIndex(24)]
        HRESULT CompileForFullPDB(IDxcResult** ppResult);

        [VtblIndex(25)]
        HRESULT OverrideArgs(DxcArgPair* pArgPairs, [NativeTypeName("UINT32")] uint uNumArgPairs);

        [VtblIndex(26)]
        HRESULT OverrideRootSignature([NativeTypeName("const WCHAR *")] char* pRootSignature);
    }
}
