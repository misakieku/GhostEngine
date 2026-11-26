using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("D5E82917-F0F1-44CF-AE5E-CE222DD0B884")]
[NativeTypeName("struct ID3D12StateObjectProperties2 : ID3D12StateObjectProperties1")]
public unsafe partial struct ID3D12StateObjectProperties2 : ID3D12StateObjectProperties2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectProperties2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, uint>)(lpVtbl[1]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, uint>)(lpVtbl[2]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void* GetShaderIdentifier([NativeTypeName("LPCWSTR")] char* pExportName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, char*, void*>)(lpVtbl[3]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), pExportName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("UINT64")]
    public ulong GetShaderStackSize([NativeTypeName("LPCWSTR")] char* pExportName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, char*, ulong>)(lpVtbl[4]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), pExportName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("UINT64")]
    public ulong GetPipelineStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, ulong>)(lpVtbl[5]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void SetPipelineStackSize([NativeTypeName("UINT64")] ulong PipelineStackSizeInBytes)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, ulong, void>)(lpVtbl[6]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), PipelineStackSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public D3D12_PROGRAM_IDENTIFIER GetProgramIdentifier([NativeTypeName("LPCWSTR")] char* pProgramName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, char*, D3D12_PROGRAM_IDENTIFIER>)(lpVtbl[7]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), pProgramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetGlobalRootSignatureForProgram([NativeTypeName("LPCWSTR")] char* pProgramName, [NativeTypeName("const IID &")] Guid* riid, void** ppvRootSignature)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, char*, Guid*, void**, int>)(lpVtbl[8]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), pProgramName, riid, ppvRootSignature);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT GetGlobalRootSignatureForShader([NativeTypeName("LPCWSTR")] char* pExportName, [NativeTypeName("const IID &")] Guid* riid, void** ppvRootSignature)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectProperties2*, char*, Guid*, void**, int>)(lpVtbl[9]))((ID3D12StateObjectProperties2*)Unsafe.AsPointer(ref this), pExportName, riid, ppvRootSignature);
    }

    public interface Interface : ID3D12StateObjectProperties1.Interface
    {
        [VtblIndex(8)]
        HRESULT GetGlobalRootSignatureForProgram([NativeTypeName("LPCWSTR")] char* pProgramName, [NativeTypeName("const IID &")] Guid* riid, void** ppvRootSignature);

        [VtblIndex(9)]
        HRESULT GetGlobalRootSignatureForShader([NativeTypeName("LPCWSTR")] char* pExportName, [NativeTypeName("const IID &")] Guid* riid, void** ppvRootSignature);
    }
}
