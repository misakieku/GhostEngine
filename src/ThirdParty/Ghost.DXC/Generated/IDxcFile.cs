using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("BB2FCA9E-1478-47BA-B08C-2C502ADA4895")]
[NativeTypeName("struct IDxcFile : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcFile : IDxcFile.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcFile);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, Guid*, void**, int>)(lpVtbl[0]))((IDxcFile*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, uint>)(lpVtbl[1]))((IDxcFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, uint>)(lpVtbl[2]))((IDxcFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetName([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, sbyte**, int>)(lpVtbl[3]))((IDxcFile*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT IsEqualTo(IDxcFile* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, IDxcFile*, int*, int>)(lpVtbl[4]))((IDxcFile*)Unsafe.AsPointer(ref this), other, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetName([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        HRESULT IsEqualTo(IDxcFile* other, [NativeTypeName("BOOL *")] int* pResult);
    }
}
