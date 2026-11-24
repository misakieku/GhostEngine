using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public unsafe partial struct ID3DInclude : ID3DInclude.Interface
{
    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT Open([NativeTypeName("D3D_INCLUDE_TYPE")] _D3D_INCLUDE_TYPE IncludeType, [NativeTypeName("LPCSTR")] sbyte* pFileName, [NativeTypeName("LPCVOID")] void* pParentData, [NativeTypeName("LPCVOID *")] void** ppData, uint* pBytes)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DInclude*, _D3D_INCLUDE_TYPE, sbyte*, void*, void**, uint*, int>)(lpVtbl[0]))((ID3DInclude*)Unsafe.AsPointer(ref this), IncludeType, pFileName, pParentData, ppData, pBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    public HRESULT Close([NativeTypeName("LPCVOID")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3DInclude*, void*, int>)(lpVtbl[1]))((ID3DInclude*)Unsafe.AsPointer(ref this), pData);
    }

    public interface Interface
    {
        [VtblIndex(0)]
        HRESULT Open([NativeTypeName("D3D_INCLUDE_TYPE")] _D3D_INCLUDE_TYPE IncludeType, [NativeTypeName("LPCSTR")] sbyte* pFileName, [NativeTypeName("LPCVOID")] void* pParentData, [NativeTypeName("LPCVOID *")] void** ppData, uint* pBytes);

        [VtblIndex(1)]
        HRESULT Close([NativeTypeName("LPCVOID")] void* pData);
    }
}
