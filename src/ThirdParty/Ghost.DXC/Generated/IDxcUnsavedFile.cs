using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("8EC00F98-07D0-4E60-9D7C-5A50B5B0017F")]
[NativeTypeName("struct IDxcUnsavedFile : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcUnsavedFile : IDxcUnsavedFile.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcUnsavedFile);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, Guid*, void**, int>)(lpVtbl[0]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint>)(lpVtbl[1]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint>)(lpVtbl[2]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetFileName([NativeTypeName("LPSTR *")] sbyte** pFileName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, sbyte**, int>)(lpVtbl[3]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pFileName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetContents([NativeTypeName("LPSTR *")] sbyte** pContents)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, sbyte**, int>)(lpVtbl[4]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pContents);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetLength([NativeTypeName("unsigned int *")] uint* pLength)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcUnsavedFile*, uint*, int>)(lpVtbl[5]))((IDxcUnsavedFile*)Unsafe.AsPointer(ref this), pLength);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetFileName([NativeTypeName("LPSTR *")] sbyte** pFileName);

        [VtblIndex(4)]
        HRESULT GetContents([NativeTypeName("LPSTR *")] sbyte** pContents);

        [VtblIndex(5)]
        HRESULT GetLength([NativeTypeName("unsigned int *")] uint* pLength);
    }
}
