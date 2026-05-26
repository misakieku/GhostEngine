using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary"]/*' />
[Guid("E5204DC7-D18C-4C3C-BDFB-851673980FE7")]
[NativeTypeName("struct IDxcLibrary : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcLibrary : IDxcLibrary.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcLibrary);

    public void** lpVtbl;

    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsUtf16(IDxcBlob* pBlob, IDxcBlobEncoding** pBlobEncoding)
    {
        return this.GetBlobAsWide(pBlob, pBlobEncoding);
    }

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, Guid*, void**, int>)(lpVtbl[0]))((IDxcLibrary*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, uint>)(lpVtbl[1]))((IDxcLibrary*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, uint>)(lpVtbl[2]))((IDxcLibrary*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.SetMalloc"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetMalloc(IMalloc* pMalloc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IMalloc*, int>)(lpVtbl[3]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pMalloc);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateBlobFromBlob"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobFromBlob(IDxcBlob* pBlob, [NativeTypeName("UINT32")] uint offset, [NativeTypeName("UINT32")] uint length, IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IDxcBlob*, uint, uint, IDxcBlob**, int>)(lpVtbl[4]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pBlob, offset, length, ppResult);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateBlobFromFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobFromFile([NativeTypeName("LPCWSTR")] char* pFileName, [NativeTypeName("UINT32 *")] uint* codePage, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, char*, uint*, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pFileName, codePage, pBlobEncoding);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateBlobWithEncodingFromPinned"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobWithEncodingFromPinned([NativeTypeName("LPCVOID")] void* pText, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, void*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[6]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pText, size, codePage, pBlobEncoding);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateBlobWithEncodingOnHeapCopy"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobWithEncodingOnHeapCopy([NativeTypeName("LPCVOID")] void* pText, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, void*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[7]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pText, size, codePage, pBlobEncoding);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateBlobWithEncodingOnMalloc"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int CreateBlobWithEncodingOnMalloc([NativeTypeName("LPCVOID")] void* pText, IMalloc* pIMalloc, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, void*, IMalloc*, uint, uint, IDxcBlobEncoding**, int>)(lpVtbl[8]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pText, pIMalloc, size, codePage, pBlobEncoding);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateIncludeHandler"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int CreateIncludeHandler(IDxcIncludeHandler** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IDxcIncludeHandler**, int>)(lpVtbl[9]))((IDxcLibrary*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.CreateStreamFromBlobReadOnly"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int CreateStreamFromBlobReadOnly(IDxcBlob* pBlob, IStream** ppStream)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IDxcBlob*, IStream**, int>)(lpVtbl[10]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pBlob, ppStream);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.GetBlobAsUtf8"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsUtf8(IDxcBlob* pBlob, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IDxcBlob*, IDxcBlobEncoding**, int>)(lpVtbl[11]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pBlob, pBlobEncoding);
    }

    /// <include file='IDxcLibrary.xml' path='doc/member[@name="IDxcLibrary.GetBlobAsWide"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetBlobAsWide(IDxcBlob* pBlob, IDxcBlobEncoding** pBlobEncoding)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcLibrary*, IDxcBlob*, IDxcBlobEncoding**, int>)(lpVtbl[12]))((IDxcLibrary*)Unsafe.AsPointer(ref this), pBlob, pBlobEncoding);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetMalloc(IMalloc* pMalloc);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobFromBlob(IDxcBlob* pBlob, [NativeTypeName("UINT32")] uint offset, [NativeTypeName("UINT32")] uint length, IDxcBlob** ppResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobFromFile([NativeTypeName("LPCWSTR")] char* pFileName, [NativeTypeName("UINT32 *")] uint* codePage, IDxcBlobEncoding** pBlobEncoding);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobWithEncodingFromPinned([NativeTypeName("LPCVOID")] void* pText, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobWithEncodingOnHeapCopy([NativeTypeName("LPCVOID")] void* pText, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int CreateBlobWithEncodingOnMalloc([NativeTypeName("LPCVOID")] void* pText, IMalloc* pIMalloc, [NativeTypeName("UINT32")] uint size, [NativeTypeName("UINT32")] uint codePage, IDxcBlobEncoding** pBlobEncoding);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int CreateIncludeHandler(IDxcIncludeHandler** ppResult);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int CreateStreamFromBlobReadOnly(IDxcBlob* pBlob, IStream** ppStream);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetBlobAsUtf8(IDxcBlob* pBlob, IDxcBlobEncoding** pBlobEncoding);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetBlobAsWide(IDxcBlob* pBlob, IDxcBlobEncoding** pBlobEncoding);
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

        [NativeTypeName("HRESULT (IMalloc *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IMalloc*, int> SetMalloc;

        [NativeTypeName("HRESULT (IDxcBlob *, UINT32, UINT32, IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, uint, uint, IDxcBlob**, int> CreateBlobFromBlob;

        [NativeTypeName("HRESULT (LPCWSTR, UINT32 *, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, uint*, IDxcBlobEncoding**, int> CreateBlobFromFile;

        [NativeTypeName("HRESULT (LPCVOID, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint, IDxcBlobEncoding**, int> CreateBlobWithEncodingFromPinned;

        [NativeTypeName("HRESULT (LPCVOID, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint, IDxcBlobEncoding**, int> CreateBlobWithEncodingOnHeapCopy;

        [NativeTypeName("HRESULT (LPCVOID, IMalloc *, UINT32, UINT32, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, IMalloc*, uint, uint, IDxcBlobEncoding**, int> CreateBlobWithEncodingOnMalloc;

        [NativeTypeName("HRESULT (IDxcIncludeHandler **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcIncludeHandler**, int> CreateIncludeHandler;

        [NativeTypeName("HRESULT (IDxcBlob *, IStream **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IStream**, int> CreateStreamFromBlobReadOnly;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlobEncoding**, int> GetBlobAsUtf8;

        [NativeTypeName("HRESULT (IDxcBlob *, IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, IDxcBlobEncoding**, int> GetBlobAsWide;
    }
}
