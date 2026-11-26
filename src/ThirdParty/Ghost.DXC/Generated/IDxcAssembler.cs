using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("091F7A26-1C1F-4948-904B-E6E3A8A771D5")]
[NativeTypeName("struct IDxcAssembler : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcAssembler : IDxcAssembler.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcAssembler);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcAssembler*, Guid*, void**, int>)(lpVtbl[0]))((IDxcAssembler*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcAssembler*, uint>)(lpVtbl[1]))((IDxcAssembler*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcAssembler*, uint>)(lpVtbl[2]))((IDxcAssembler*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT AssembleToContainer(IDxcBlob* pShader, IDxcOperationResult** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcAssembler*, IDxcBlob*, IDxcOperationResult**, int>)(lpVtbl[3]))((IDxcAssembler*)Unsafe.AsPointer(ref this), pShader, ppResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT AssembleToContainer(IDxcBlob* pShader, IDxcOperationResult** ppResult);
    }
}
