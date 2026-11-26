using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("4315D938-F369-4F93-95A2-252017CC3807")]
[NativeTypeName("struct IDxcPdbUtils2 : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPdbUtils2 : IDxcPdbUtils2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPdbUtils2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint>)(lpVtbl[1]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint>)(lpVtbl[2]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT Load(IDxcBlob* pPdbOrDxil)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pPdbOrDxil);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[4]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetSourceName([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[6]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetLibraryPDBCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[7]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetLibraryPDB([NativeTypeName("UINT32")] uint uIndex, IDxcPdbUtils2** ppOutPdbUtils, IDxcBlobWide** ppLibraryName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcPdbUtils2**, IDxcBlobWide**, int>)(lpVtbl[8]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppOutPdbUtils, ppLibraryName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[9]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT GetFlag([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[10]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public HRESULT GetArgCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[11]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT GetArg([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[12]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[13]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public HRESULT GetArgPair([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppName, IDxcBlobWide** ppValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, IDxcBlobWide**, int>)(lpVtbl[14]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppName, ppValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[15]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT GetDefine([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint, IDxcBlobWide**, int>)(lpVtbl[16]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), uIndex, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public HRESULT GetTargetProfile(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[17]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public HRESULT GetEntryPoint(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[18]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public HRESULT GetMainFileName(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[19]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public HRESULT GetHash(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[20]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public HRESULT GetName(IDxcBlobWide** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlobWide**, int>)(lpVtbl[21]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public HRESULT GetVersionInfo(IDxcVersionInfo** ppVersionInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcVersionInfo**, int>)(lpVtbl[22]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppVersionInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public HRESULT GetCustomToolchainID([NativeTypeName("UINT32 *")] uint* pID)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, uint*, int>)(lpVtbl[23]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), pID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    public HRESULT GetCustomToolchainData(IDxcBlob** ppBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[24]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppBlob);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public HRESULT GetWholeDxil(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, IDxcBlob**, int>)(lpVtbl[25]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this), ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    [return: NativeTypeName("BOOL")]
    public int IsFullPDB()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPdbUtils2*, int>)(lpVtbl[26]))((IDxcPdbUtils2*)Unsafe.AsPointer(ref this));
    }

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
        HRESULT Load(IDxcBlob* pPdbOrDxil);

        [VtblIndex(4)]
        HRESULT GetSourceCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(5)]
        HRESULT GetSource([NativeTypeName("UINT32")] uint uIndex, IDxcBlobEncoding** ppResult);

        [VtblIndex(6)]
        HRESULT GetSourceName([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(7)]
        HRESULT GetLibraryPDBCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(8)]
        HRESULT GetLibraryPDB([NativeTypeName("UINT32")] uint uIndex, IDxcPdbUtils2** ppOutPdbUtils, IDxcBlobWide** ppLibraryName);

        [VtblIndex(9)]
        HRESULT GetFlagCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(10)]
        HRESULT GetFlag([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(11)]
        HRESULT GetArgCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(12)]
        HRESULT GetArg([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(13)]
        HRESULT GetArgPairCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(14)]
        HRESULT GetArgPair([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppName, IDxcBlobWide** ppValue);

        [VtblIndex(15)]
        HRESULT GetDefineCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(16)]
        HRESULT GetDefine([NativeTypeName("UINT32")] uint uIndex, IDxcBlobWide** ppResult);

        [VtblIndex(17)]
        HRESULT GetTargetProfile(IDxcBlobWide** ppResult);

        [VtblIndex(18)]
        HRESULT GetEntryPoint(IDxcBlobWide** ppResult);

        [VtblIndex(19)]
        HRESULT GetMainFileName(IDxcBlobWide** ppResult);

        [VtblIndex(20)]
        HRESULT GetHash(IDxcBlob** ppResult);

        [VtblIndex(21)]
        HRESULT GetName(IDxcBlobWide** ppResult);

        [VtblIndex(22)]
        HRESULT GetVersionInfo(IDxcVersionInfo** ppVersionInfo);

        [VtblIndex(23)]
        HRESULT GetCustomToolchainID([NativeTypeName("UINT32 *")] uint* pID);

        [VtblIndex(24)]
        HRESULT GetCustomToolchainData(IDxcBlob** ppBlob);

        [VtblIndex(25)]
        HRESULT GetWholeDxil(IDxcBlob** ppResult);

        [VtblIndex(26)]
        [return: NativeTypeName("BOOL")]
        int IsFullPDB();

        [VtblIndex(27)]
        [return: NativeTypeName("BOOL")]
        int IsPDBRef();
    }
}
