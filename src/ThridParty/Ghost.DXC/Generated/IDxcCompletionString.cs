using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcCompletionString.xml' path='doc/member[@name="IDxcCompletionString"]/*' />
[Guid("06B51E0F-A605-4C69-A110-CD6E14B58EEC")]
[NativeTypeName("struct IDxcCompletionString : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCompletionString : IDxcCompletionString.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCompletionString);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint>)(lpVtbl[1]))((IDxcCompletionString*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint>)(lpVtbl[2]))((IDxcCompletionString*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcCompletionString.xml' path='doc/member[@name="IDxcCompletionString.GetNumCompletionChunks"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumCompletionChunks([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint*, int>)(lpVtbl[3]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCompletionString.xml' path='doc/member[@name="IDxcCompletionString.GetCompletionChunkKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetCompletionChunkKind([NativeTypeName("unsigned int")] uint chunkNumber, DxcCompletionChunkKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint, DxcCompletionChunkKind*, int>)(lpVtbl[4]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), chunkNumber, pResult);
    }

    /// <include file='IDxcCompletionString.xml' path='doc/member[@name="IDxcCompletionString.GetCompletionChunkText"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetCompletionChunkText([NativeTypeName("unsigned int")] uint chunkNumber, [NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCompletionString*, uint, sbyte**, int>)(lpVtbl[5]))((IDxcCompletionString*)Unsafe.AsPointer(ref this), chunkNumber, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetNumCompletionChunks([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetCompletionChunkKind([NativeTypeName("unsigned int")] uint chunkNumber, DxcCompletionChunkKind* pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetCompletionChunkText([NativeTypeName("unsigned int")] uint chunkNumber, [NativeTypeName("LPSTR *")] sbyte** pResult);
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

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumCompletionChunks;

        [NativeTypeName("HRESULT (unsigned int, DxcCompletionChunkKind *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, DxcCompletionChunkKind*, int> GetCompletionChunkKind;

        [NativeTypeName("HRESULT (unsigned int, LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, sbyte**, int> GetCompletionChunkText;
    }
}
