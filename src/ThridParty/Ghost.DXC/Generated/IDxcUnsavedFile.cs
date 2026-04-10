using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcUnsavedFile.xml' path='doc/member[@name="IDxcUnsavedFile"]/*' />
[Guid("8EC00F98-07D0-4E60-9D7C-5A50B5B0017F")]
[NativeTypeName("struct IDxcUnsavedFile : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcUnsavedFile : IDxcUnsavedFile.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcUnsavedFile);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, Guid*, void**, int>)(lpVtbl[0]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint>)(lpVtbl[1]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint>)(lpVtbl[2]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcUnsavedFile.xml' path='doc/member[@name="IDxcUnsavedFile.GetFileName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetFileName([NativeTypeName("LPSTR *")] sbyte** pFileName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, sbyte**, int>)(lpVtbl[3]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pFileName);
    }

    /// <include file='IDxcUnsavedFile.xml' path='doc/member[@name="IDxcUnsavedFile.GetContents"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetContents([NativeTypeName("LPSTR *")] sbyte** pContents)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, sbyte**, int>)(lpVtbl[4]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pContents);
    }

    /// <include file='IDxcUnsavedFile.xml' path='doc/member[@name="IDxcUnsavedFile.GetLength"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetLength([NativeTypeName("unsigned int *")] uint* pLength)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint*, int>)(lpVtbl[5]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pLength);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetFileName([NativeTypeName("LPSTR *")] sbyte** pFileName);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetContents([NativeTypeName("LPSTR *")] sbyte** pContents);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetLength([NativeTypeName("unsigned int *")] uint* pLength);
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

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetFileName;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetContents;

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetLength;
    }
}
