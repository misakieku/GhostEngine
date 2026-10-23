using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils"]/*' />
[Guid("4605C4CB-2019-492A-ADA4-65F20BB7D67F")]
[NativeTypeName("struct IDxcUtils : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcUtils : IDxcUtils.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcUtils);

    public void** lpVtbl;

    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsUtf16(IDxcBlob* pBlob, IDxcBlobWide** ppBlobEncoding)
    {
        return this.GetBlobAsWide(pBlob, ppBlobEncoding);
    }

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, Guid*, void**, int>)(lpVtbl[0]))((IDxcUtils*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, uint>)(lpVtbl[1]))((IDxcUtils*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, uint>)(lpVtbl[2]))((IDxcUtils*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateBlobFromBlob"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobFromBlob(IDxcBlob* pBlob, [NativeTypeName("UINT32")] uint offset, [NativeTypeName("UINT32")] uint length, IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcBlob*, uint, uint, IDxcBlob**, int>)(lpVtbl[3]))((IDxcUtils*)Unsafe.AsPointer(ref this), pBlob, offset, length, ppResult);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateBlobFromPinned"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobFromPinned([NativeTypeName("LPCVOID")] void* pData, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, void*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[4]))((IDxcUtils*)Unsafe.AsPointer(ref this), pData, size, codePage, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.MoveToBlob"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int MoveToBlob([NativeTypeName("LPCVOID")] void* pData, IMalloc* pIMalloc, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, void*, IMalloc*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcUtils*)Unsafe.AsPointer(ref this), pData, pIMalloc, size, codePage, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateBlob"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlob([NativeTypeName("LPCVOID")] void* pData, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, void*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[6]))((IDxcUtils*)Unsafe.AsPointer(ref this), pData, size, codePage, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.LoadFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int LoadFile([NativeTypeName("LPCWSTR")] char* pFileName, [NativeTypeName("UINT32 *")] uint* pCodePage, IDxcBlobEncoding** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, char*, uint*, IDxcBlobEncoding**, int>)(lpVtbl[7]))((IDxcUtils*)Unsafe.AsPointer(ref this), pFileName, pCodePage, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateReadOnlyStreamFromBlob"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int CreateReadOnlyStreamFromBlob(IDxcBlob* pBlob, IStream** ppStream)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcBlob*, IStream**, int>)(lpVtbl[8]))((IDxcUtils*)Unsafe.AsPointer(ref this), pBlob, ppStream);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateDefaultIncludeHandler"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int CreateDefaultIncludeHandler(IDxcIncludeHandler** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcIncludeHandler**, int>)(lpVtbl[9]))((IDxcUtils*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.GetBlobAsUtf8"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsUtf8(IDxcBlob* pBlob, IDxcBlobUtf8** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcBlob*, IDxcBlobUtf8**, int>)(lpVtbl[10]))((IDxcUtils*)Unsafe.AsPointer(ref this), pBlob, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.GetBlobAsWide"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsWide(IDxcBlob* pBlob, IDxcBlobWide** ppBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcBlob*, IDxcBlobWide**, int>)(lpVtbl[11]))((IDxcUtils*)Unsafe.AsPointer(ref this), pBlob, ppBlobEncoding);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.GetDxilContainerPart"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetDxilContainerPart([NativeTypeName("const DxcBuffer *")] DxcBuffer* pShader, [NativeTypeName("UINT32")] uint DxcPart, void** ppPartData, [NativeTypeName("UINT32 *")] uint* pPartSizeInBytes)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, DxcBuffer*, uint, void**, uint*, int>)(lpVtbl[12]))((IDxcUtils*)Unsafe.AsPointer(ref this), pShader, DxcPart, ppPartData, pPartSizeInBytes);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.CreateReflection"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int CreateReflection([NativeTypeName("const DxcBuffer *")] DxcBuffer* pData, [NativeTypeName("const IID &")] Guid* iid, void** ppvReflection)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, DxcBuffer*, Guid*, void**, int>)(lpVtbl[13]))((IDxcUtils*)Unsafe.AsPointer(ref this), pData, iid, ppvReflection);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.BuildArguments"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int BuildArguments([NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcCompilerArgs** ppArgs)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcCompilerArgs**, int>)(lpVtbl[14]))((IDxcUtils*)Unsafe.AsPointer(ref this), pSourceName, pEntryPoint, pTargetProfile, pArguments, argCount, pDefines, defineCount, ppArgs);
    }

    /// <include file='IDxcUtils.xml' path='doc/member[@name="IDxcUtils.GetPDBContents"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetPDBContents(IDxcBlob* pPDBBlob, IDxcBlob** ppHash, IDxcBlob** ppContainer)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUtils*, IDxcBlob*, IDxcBlob**, IDxcBlob**, int>)(lpVtbl[15]))((IDxcUtils*)Unsafe.AsPointer(ref this), pPDBBlob, ppHash, ppContainer);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobFromBlob(IDxcBlob* pBlob, [NativeTypeName("UINT32")] uint offset, [NativeTypeName("UINT32")] uint length, IDxcBlob** ppResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobFromPinned([NativeTypeName("LPCVOID")] void* pData, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int MoveToBlob([NativeTypeName("LPCVOID")] void* pData, IMalloc* pIMalloc, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlob([NativeTypeName("LPCVOID")] void* pData, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** ppBlobEncoding);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int LoadFile([NativeTypeName("LPCWSTR")] char* pFileName, [NativeTypeName("UINT32 *")] uint* pCodePage, IDxcBlobEncoding** ppBlobEncoding);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int CreateReadOnlyStreamFromBlob(IDxcBlob* pBlob, IStream** ppStream);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int CreateDefaultIncludeHandler(IDxcIncludeHandler** ppResult);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int GetBlobAsUtf8(IDxcBlob* pBlob, IDxcBlobUtf8** ppBlobEncoding);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetBlobAsWide(IDxcBlob* pBlob, IDxcBlobWide** ppBlobEncoding);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetDxilContainerPart([NativeTypeName("const DxcBuffer *")] DxcBuffer* pShader, [NativeTypeName("UINT32")] uint DxcPart, void** ppPartData, [NativeTypeName("UINT32 *")] uint* pPartSizeInBytes);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int CreateReflection([NativeTypeName("const DxcBuffer *")] DxcBuffer* pData, [NativeTypeName("const IID &")] Guid* iid, void** ppvReflection);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int BuildArguments([NativeTypeName("LPCWSTR")] char* pSourceName, [NativeTypeName("LPCWSTR")] char* pEntryPoint, [NativeTypeName("LPCWSTR")] char* pTargetProfile, [NativeTypeName("LPCWSTR *")] char** pArguments, [NativeTypeName("UINT32")] uint argCount, [NativeTypeName("const DxcDefine *")] DxcDefine* pDefines, [NativeTypeName("UINT32")] uint defineCount, IDxcCompilerArgs** ppArgs);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetPDBContents(IDxcBlob* pPDBBlob, IDxcBlob** ppHash, IDxcBlob** ppContainer);
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

        [NativeTypeName("HRESULT (IDxcBlob *, UINT32, UINT32, IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, uint, uint, IDxcBlob**, int> CreateBlobFromBlob;

        [NativeTypeName("HRESULT (LPCVOID, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint, IDxcBlobEncoding**, int> CreateBlobFromPinned;

        [NativeTypeName("HRESULT (LPCVOID, IMalloc *, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, IMalloc*, uint, uint, IDxcBlobEncoding**, int> MoveToBlob;

        [NativeTypeName("HRESULT (LPCVOID, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint, IDxcBlobEncoding**, int> CreateBlob;

        [NativeTypeName("HRESULT (LPCWSTR, UINT32 *, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, uint*, IDxcBlobEncoding**, int> LoadFile;

        [NativeTypeName("HRESULT (IDxcBlob *, IStream **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IStream**, int> CreateReadOnlyStreamFromBlob;

        [NativeTypeName("HRESULT (IDxcIncludeHandler **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcIncludeHandler**, int> CreateDefaultIncludeHandler;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlobUtf8 **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlobUtf8**, int> GetBlobAsUtf8;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlobWide**, int> GetBlobAsWide;

        [NativeTypeName("HRESULT (const DxcBuffer *, UINT32, void **, UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcBuffer*, uint, void**, uint*, int> GetDxilContainerPart;

        [NativeTypeName("HRESULT (const DxcBuffer *, const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcBuffer*, Guid*, void**, int> CreateReflection;

        [NativeTypeName("HRESULT (LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR *, UINT32, const DxcDefine *, UINT32, IDxcCompilerArgs **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, char*, char*, char**, uint, DxcDefine*, uint, IDxcCompilerArgs**, int> BuildArguments;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlob **, IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlob**, IDxcBlob**, int> GetPDBContents;
    }
}
