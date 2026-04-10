using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.DXC;

public unsafe partial struct STATSTG
{
    // LPOLESTR - pointer to a null-terminated Unicode string (allocated by the COM method)
    public char* pwcsName;

    // DWORD
    public uint type;

    // ULARGE_INTEGER (64-bit unsigned integer)
    public ulong cbSize;

    public FILETIME mtime;
    public FILETIME ctime;
    public FILETIME atime;

    // DWORDs
    public uint grfMode;
    public uint grfLocksSupported;

    // CLSID
    public Guid clsid;

    // DWORDs
    public uint grfStateBits;
    public uint reserved;
}

// Corresponds to the Win32 FILETIME structure
public partial struct FILETIME
{
    public uint dwLowDateTime;
    public uint dwHighDateTime;
}

[Guid("0000000C-0000-0000-C000-000000000046")]
[NativeTypeName("struct IStream : ISequentialStream")]
[NativeInheritance("ISequentialStream")]
public unsafe partial struct IStream
{
    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, Guid*, void**, int>)(lpVtbl[0]))((IStream*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, uint>)(lpVtbl[1]))((IStream*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, uint>)(lpVtbl[2]))((IStream*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="ISequentialStream.Read" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public int Read(void* pv, [NativeTypeName("ULONG")] uint cb, [NativeTypeName("ULONG *")] uint* pcbRead)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, void*, uint, uint*, int>)(lpVtbl[3]))((IStream*)Unsafe.AsPointer(ref this), pv, cb, pcbRead);
    }

    /// <inheritdoc cref="ISequentialStream.Write" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public int Write([NativeTypeName("const void *")] void* pv, [NativeTypeName("ULONG")] uint cb, [NativeTypeName("ULONG *")] uint* pcbWritten)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, void*, uint, uint*, int>)(lpVtbl[4]))((IStream*)Unsafe.AsPointer(ref this), pv, cb, pcbWritten);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.Seek"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public int Seek(long dlibMove, [NativeTypeName("DWORD")] uint dwOrigin, ulong* plibNewPosition)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, long, uint, ulong*, int>)(lpVtbl[5]))((IStream*)Unsafe.AsPointer(ref this), dlibMove, dwOrigin, plibNewPosition);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.SetSize"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public int SetSize(ulong libNewSize)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, ulong, int>)(lpVtbl[6]))((IStream*)Unsafe.AsPointer(ref this), libNewSize);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.CopyTo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public int CopyTo(IStream* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, IStream*, ulong, ulong*, ulong*, int>)(lpVtbl[7]))((IStream*)Unsafe.AsPointer(ref this), pstm, cb, pcbRead, pcbWritten);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.Commit"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public int Commit([NativeTypeName("DWORD")] uint grfCommitFlags)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, uint, int>)(lpVtbl[8]))((IStream*)Unsafe.AsPointer(ref this), grfCommitFlags);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.Revert"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public int Revert()
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, int>)(lpVtbl[9]))((IStream*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.LockRegion"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public int LockRegion(ulong libOffset, ulong cb, [NativeTypeName("DWORD")] uint dwLockType)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, ulong, ulong, uint, int>)(lpVtbl[10]))((IStream*)Unsafe.AsPointer(ref this), libOffset, cb, dwLockType);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.UnlockRegion"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public int UnlockRegion(ulong libOffset, ulong cb, [NativeTypeName("DWORD")] uint dwLockType)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, ulong, ulong, uint, int>)(lpVtbl[11]))((IStream*)Unsafe.AsPointer(ref this), libOffset, cb, dwLockType);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.Stat"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public int Stat(STATSTG* pstatstg, [NativeTypeName("DWORD")] uint grfStatFlag)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, STATSTG*, uint, int>)(lpVtbl[12]))((IStream*)Unsafe.AsPointer(ref this), pstatstg, grfStatFlag);
    }

    /// <include file='IStream.xml' path='doc/member[@name="IStream.Clone"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public int Clone(IStream** ppstm)
    {
        return ((delegate* unmanaged[MemberFunction]<IStream*, IStream**, int>)(lpVtbl[13]))((IStream*)Unsafe.AsPointer(ref this), ppstm);
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged
    {
        [NativeTypeName("int (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("int (void *, ULONG, ULONG *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint*, int> Read;

        [NativeTypeName("int (const void *, ULONG, ULONG *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, uint, uint*, int> Write;

        [NativeTypeName("int (long, DWORD, ulong *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, long, uint, ulong*, int> Seek;

        [NativeTypeName("int (ulong) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong, int> SetSize;

        [NativeTypeName("int (IStream *, ulong, ulong *, ulong *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IStream*, ulong, ulong*, ulong*, int> CopyTo;

        [NativeTypeName("int (DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, int> Commit;

        [NativeTypeName("int () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int> Revert;

        [NativeTypeName("int (ulong, ulong, DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong, ulong, uint, int> LockRegion;

        [NativeTypeName("int (ulong, ulong, DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong, ulong, uint, int> UnlockRegion;

        [NativeTypeName("int (STATSTG *, DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, STATSTG*, uint, int> Stat;

        [NativeTypeName("int (IStream **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IStream**, int> Clone;
    }
}