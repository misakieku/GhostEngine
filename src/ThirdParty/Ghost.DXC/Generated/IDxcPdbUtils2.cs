using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2"]/*' />
[Guid("4315D938-F369-4F93-95A2-252017CC3807")]
[NativeTypeName("struct IDxcPdbUtils2 : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPdbUtils2 : IDxcPdbUtils2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPdbUtils2);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint>)(lpVtbl[1]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint>)(lpVtbl[2]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.Load"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int Load(IDxcBlob* pPdbOrDxil)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pPdbOrDxil);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetSourceCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[4]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetSource"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetSourceName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceName([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[6]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetLibraryPDBCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetLibraryPDBCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[7]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetLibraryPDB"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetLibraryPDB([NativeTypeName("UINT32")] uint uIndex, IDxcPdbUtils2** ppOutPdbUtils, IDxcBlobWide** ppLibraryName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcPdbUtils2**, IDxcBlobWide**, int>)(lpVtbl[8]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppOutPdbUtils, ppLibraryName);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetFlagCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[9]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetFlag"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int GetFlag([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[10]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetArgCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[11]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetArg"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetArg([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[12]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetArgPairCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[13]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetArgPair"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgPair([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppName, IDxcBlobWide** ppValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, IDxcBlobWide**, int>)(lpVtbl[14]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppName, ppValue);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetDefineCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[15]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetDefine"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefine([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[16]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetTargetProfile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetTargetProfile(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[17]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetEntryPoint"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int GetEntryPoint(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[18]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetMainFileName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int GetMainFileName(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[19]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetHash"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("HRESULT")]
    public int GetHash(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[20]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int GetName(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[21]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetVersionInfo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int GetVersionInfo(IDxcVersionInfo** ppVersionInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcVersionInfo**, int>)(lpVtbl[22]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppVersionInfo);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetCustomToolchainID"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    [return: NativeTypeName("HRESULT")]
    public int GetCustomToolchainID([NativeTypeName("UINT32 *")] uint* pID)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[23]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pID);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetCustomToolchainData"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int GetCustomToolchainData(IDxcBlob** ppBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[24]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppBlob);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.GetWholeDxil"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    [return: NativeTypeName("HRESULT")]
    public int GetWholeDxil(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[25]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.IsFullPDB"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    [return: NativeTypeName("BOOL")]
    public int IsFullPDB()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, int>)(lpVtbl[26]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPdbUtils2.xml' path='doc/member[@name="IDxcPdbUtils2.IsPDBRef"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    [return: NativeTypeName("BOOL")]
    public int IsPDBRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, int>)(lpVtbl[27]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
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
        int GetSourceName([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetLibraryPDBCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetLibraryPDB([NativeTypeName("UINT32")] uint uIndex, IDxcPdbUtils2** ppOutPdbUtils, IDxcBlobWide** ppLibraryName);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int GetFlag([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetArgCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetArg([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int GetArgPair([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppName, IDxcBlobWide** ppValue);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int GetDefine([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int GetTargetProfile(IDxcBlobWide** ppResult);

        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int GetEntryPoint(IDxcBlobWide** ppResult);

        [VtblIndex(19)]
        [return: NativeTypeName("HRESULT")]
        int GetMainFileName(IDxcBlobWide** ppResult);

        [VtblIndex(20)]
        [return: NativeTypeName("HRESULT")]
        int GetHash(IDxcBlob** ppResult);

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int GetName(IDxcBlobWide** ppResult);

        [VtblIndex(22)]
        [return: NativeTypeName("HRESULT")]
        int GetVersionInfo(IDxcVersionInfo** ppVersionInfo);

        [VtblIndex(23)]
        [return: NativeTypeName("HRESULT")]
        int GetCustomToolchainID([NativeTypeName("UINT32 *")] uint* pID);

        [VtblIndex(24)]
        [return: NativeTypeName("HRESULT")]
        int GetCustomToolchainData(IDxcBlob** ppBlob);

        [VtblIndex(25)]
        [return: NativeTypeName("HRESULT")]
        int GetWholeDxil(IDxcBlob** ppResult);

        [VtblIndex(26)]
        [return: NativeTypeName("BOOL")]
        int IsFullPDB();

        [VtblIndex(27)]
        [return: NativeTypeName("BOOL")]
        int IsPDBRef();
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

        [NativeTypeName("HRESULT (UINT32, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobWide**, int> GetSourceName;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetLibraryPDBCount;

        [NativeTypeName("HRESULT (UINT32, IDxcPdbUtils2 **, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPdbUtils2**, IDxcBlobWide**, int> GetLibraryPDB;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetFlagCount;

        [NativeTypeName("HRESULT (UINT32, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobWide**, int> GetFlag;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetArgCount;

        [NativeTypeName("HRESULT (UINT32, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobWide**, int> GetArg;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetArgPairCount;

        [NativeTypeName("HRESULT (UINT32, IDxcBlobWide **, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobWide**, IDxcBlobWide**, int> GetArgPair;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetDefineCount;

        [NativeTypeName("HRESULT (UINT32, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlobWide**, int> GetDefine;

        [NativeTypeName("HRESULT (IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlobWide**, int> GetTargetProfile;

        [NativeTypeName("HRESULT (IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlobWide**, int> GetEntryPoint;

        [NativeTypeName("HRESULT (IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlobWide**, int> GetMainFileName;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetHash;

        [NativeTypeName("HRESULT (IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlobWide**, int> GetName;

        [NativeTypeName("HRESULT (IDxcVersionInfo **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcVersionInfo**, int> GetVersionInfo;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetCustomToolchainID;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetCustomToolchainData;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetWholeDxil;

        [NativeTypeName("BOOL () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int> IsFullPDB;

        [NativeTypeName("BOOL () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int> IsPDBRef;
    }
}
