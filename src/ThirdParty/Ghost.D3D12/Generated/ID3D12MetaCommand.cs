using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("DBB84C27-36CE-4FC9-B801-F048C46AC570")]
[NativeTypeName("struct ID3D12MetaCommand : ID3D12Pageable")]
public unsafe partial struct ID3D12MetaCommand : ID3D12MetaCommand.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12MetaCommand);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, uint>)(lpVtbl[1]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, uint>)(lpVtbl[2]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, char*, int>)(lpVtbl[6]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("UINT64")]
    public ulong GetRequiredParameterResourceSize(D3D12_META_COMMAND_PARAMETER_STAGE Stage, uint ParameterIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12MetaCommand*, D3D12_META_COMMAND_PARAMETER_STAGE, uint, ulong>)(lpVtbl[8]))((ID3D12MetaCommand*)Unsafe.AsPointer(ref this), Stage, ParameterIndex);
    }

    public interface Interface : ID3D12Pageable.Interface
    {
        [VtblIndex(8)]
        [return: NativeTypeName("UINT64")]
        ulong GetRequiredParameterResourceSize(D3D12_META_COMMAND_PARAMETER_STAGE Stage, uint ParameterIndex);
    }
}
