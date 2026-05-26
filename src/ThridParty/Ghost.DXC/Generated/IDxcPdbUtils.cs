using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils"]/*' />
[Guid("E6C9647E-9D6A-4C3B-B94C-524B5A6C343D")]
[NativeTypeName("struct IDxcPdbUtils : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPdbUtils : IDxcPdbUtils.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPdbUtils);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint>)(lpVtbl[1]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint>)(lpVtbl[2]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.Load"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int Load(IDxcBlob* pPdbOrDxil)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pPdbOrDxil);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetSourceCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[4]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetSource"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetSourceName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceName([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[6]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetFlagCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[7]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetFlag"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetFlag([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[8]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetArgCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[9]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetArg"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int GetArg([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[10]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetArgPairCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[11]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetArgPair"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgPair([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pName, [NativeTypeName("BSTR *")] char** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, char**, int>)(lpVtbl[12]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pName, pValue);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetDefineCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint*, int>)(lpVtbl[13]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetDefine"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefine([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, uint, char**, int>)(lpVtbl[14]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), uIndex, pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetTargetProfile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetTargetProfile([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[15]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetEntryPoint"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int GetEntryPoint([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[16]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetMainFileName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetMainFileName([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[17]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetHash"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int GetHash(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob**, int>)(lpVtbl[18]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char**, int>)(lpVtbl[19]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.IsFullPDB"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("BOOL")]
    public int IsFullPDB()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, int>)(lpVtbl[20]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetFullPDB"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int GetFullPDB(IDxcBlob** ppFullPDB)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcBlob**, int>)(lpVtbl[21]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppFullPDB);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.GetVersionInfo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int GetVersionInfo(IDxcVersionInfo** ppVersionInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcVersionInfo**, int>)(lpVtbl[22]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppVersionInfo);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.SetCompiler"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    [return: NativeTypeName("HRESULT")]
    public int SetCompiler(IDxcCompiler3* pCompiler)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcCompiler3*, int>)(lpVtbl[23]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pCompiler);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.CompileForFullPDB"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int CompileForFullPDB(IDxcResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, IDxcResult**, int>)(lpVtbl[24]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.OverrideArgs"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    [return: NativeTypeName("HRESULT")]
    public int OverrideArgs(DxcArgPair* pArgPairs, [NativeTypeName("UINT32")] uint uNumArgPairs)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, DxcArgPair*, uint, int>)(lpVtbl[25]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pArgPairs, uNumArgPairs);
    }

    /// <include file='IDxcPdbUtils.xml' path='doc/member[@name="IDxcPdbUtils.OverrideRootSignature"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    [return: NativeTypeName("HRESULT")]
    public int OverrideRootSignature([NativeTypeName("const WCHAR *")] char* pRootSignature)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils*, char*, int>)(lpVtbl[26]))((IDxcPdbUtils*)Unsafe.AsPointer(ref this), pRootSignature);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int Load(IDxcBlob* pPdbOrDxil);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetSourceName([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetFlag([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetArgCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int GetArg([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetArgPair([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pName, [NativeTypeName("BSTR *")] char** pValue);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int GetDefine([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetTargetProfile([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int GetEntryPoint([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int GetMainFileName([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int GetHash(IDxcBlob** ppResult);

        [VtblIndex(19)]
        [return: NativeTypeName("HRESULT")]
        int GetName([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(20)]
        [return: NativeTypeName("BOOL")]
        int IsFullPDB();

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int GetFullPDB(IDxcBlob** ppFullPDB);

        [VtblIndex(22)]
        [return: NativeTypeName("HRESULT")]
        int GetVersionInfo(IDxcVersionInfo** ppVersionInfo);

        [VtblIndex(23)]
        [return: NativeTypeName("HRESULT")]
        int SetCompiler(IDxcCompiler3* pCompiler);

        [VtblIndex(24)]
        [return: NativeTypeName("HRESULT")]
        int CompileForFullPDB(IDxcResult** ppResult);

        [VtblIndex(25)]
        [return: NativeTypeName("HRESULT")]
        int OverrideArgs(DxcArgPair* pArgPairs, [NativeTypeName("UINT32")] uint uNumArgPairs);

        [VtblIndex(26)]
        [return: NativeTypeName("HRESULT")]
        int OverrideRootSignature([NativeTypeName("const WCHAR *")] char* pRootSignature);
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

        [NativeTypeName("HRESULT (IDxcBlob *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, int> Load;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetSourceCount;

        [NativeTypeName("HRESULT (UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobEncoding**, int> GetSource;

        [NativeTypeName("HRESULT (UINT32, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetSourceName;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetFlagCount;

        [NativeTypeName("HRESULT (UINT32, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetFlag;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetArgCount;

        [NativeTypeName("HRESULT (UINT32, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetArg;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetArgPairCount;

        [NativeTypeName("HRESULT (UINT32, BSTR *, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, char**, int> GetArgPair;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetDefineCount;

        [NativeTypeName("HRESULT (UINT32, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetDefine;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetTargetProfile;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetEntryPoint;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetMainFileName;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetHash;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetName;

        [NativeTypeName("BOOL () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int> IsFullPDB;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetFullPDB;

        [NativeTypeName("HRESULT (IDxcVersionInfo **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcVersionInfo**, int> GetVersionInfo;

        [NativeTypeName("HRESULT (IDxcCompiler3 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCompiler3*, int> SetCompiler;

        [NativeTypeName("HRESULT (IDxcResult **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcResult**, int> CompileForFullPDB;

        [NativeTypeName("HRESULT (DxcArgPair *, UINT32) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcArgPair*, uint, int> OverrideArgs;

        [NativeTypeName("HRESULT (const WCHAR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, int> OverrideRootSignature;
    }
}
