using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("458E1FD1-B1B2-4750-A6E1-9C10F03BED92")]
[NativeTypeName("struct IDxcValidator2 : IDxcValidator")]
[NativeInheritance("IDxcValidator")]
public unsafe partial struct IDxcValidator2 : IDxcValidator2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcValidator2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcValidator2*, Guid*, void**, int>)(lpVtbl[0]))((IDxcValidator2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcValidator2*, uint>)(lpVtbl[1]))((IDxcValidator2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcValidator2*, uint>)(lpVtbl[2]))((IDxcValidator2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT Validate(IDxcBlob* pShader, [NativeTypeName("UINT32")] uint Flags, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcValidator2*, IDxcBlob*, uint, IDxcOperationResult**, int>)(lpVtbl[3]))((IDxcValidator2*)Unsafe.AsPointer(ref this), pShader, Flags, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT ValidateWithDebug(IDxcBlob* pShader, [NativeTypeName("UINT32")] uint Flags, DxcBuffer* pOptDebugBitcode, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcValidator2*, IDxcBlob*, uint, DxcBuffer*, IDxcOperationResult**, int>)(lpVtbl[4]))((IDxcValidator2*)Unsafe.AsPointer(ref this), pShader, Flags, pOptDebugBitcode, ppResult);
    }

    public interface Interface : IDxcValidator.Interface
    {
        [VtblIndex(4)]
        HRESULT ValidateWithDebug(IDxcBlob* pShader, [NativeTypeName("UINT32")] uint Flags, DxcBuffer* pOptDebugBitcode, IDxcOperationResult** ppResult);
    }
}
