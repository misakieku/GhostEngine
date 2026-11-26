using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("2EC912FD-B144-4A15-AD0D-1C5439C81E46")]
[NativeTypeName("struct IDxcType : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcType : IDxcType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcType);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, uint>)(lpVtbl[1]))((IDxcType*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, uint>)(lpVtbl[2]))((IDxcType*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, sbyte**, int>)(lpVtbl[3]))((IDxcType*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT IsEqualTo(IDxcType* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, IDxcType*, int*, int>)(lpVtbl[4]))((IDxcType*)Unsafe.AsPointer(ref this), other, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetKind(DxcTypeKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, DxcTypeKind*, int>)(lpVtbl[5]))((IDxcType*)Unsafe.AsPointer(ref this), pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        HRESULT IsEqualTo(IDxcType* other, [NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(5)]
        HRESULT GetKind(DxcTypeKind* pResult);
    }
}
