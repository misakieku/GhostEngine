using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("065ACF71-F863-4B89-82F4-02E4D5886757")]
[NativeTypeName("struct ID3D12WorkGraphProperties : IUnknown")]
public unsafe partial struct ID3D12WorkGraphProperties : ID3D12WorkGraphProperties.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12WorkGraphProperties);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint>)(lpVtbl[1]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint>)(lpVtbl[2]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public uint GetNumWorkGraphs()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint>)(lpVtbl[3]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("LPCWSTR")]
    public char* GetProgramName(uint WorkGraphIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, char*>)(lpVtbl[4]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public uint GetWorkGraphIndex([NativeTypeName("LPCWSTR")] char* pProgramName)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, char*, uint>)(lpVtbl[5]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), pProgramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public uint GetNumNodes(uint WorkGraphIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint>)(lpVtbl[6]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public D3D12_NODE_ID GetNodeID(uint WorkGraphIndex, uint NodeIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint, D3D12_NODE_ID>)(lpVtbl[7]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, NodeIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public uint GetNodeIndex(uint WorkGraphIndex, D3D12_NODE_ID NodeID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, D3D12_NODE_ID, uint>)(lpVtbl[8]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, NodeID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public uint GetNodeLocalRootArgumentsTableIndex(uint WorkGraphIndex, uint NodeIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint, uint>)(lpVtbl[9]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, NodeIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public uint GetNumEntrypoints(uint WorkGraphIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint>)(lpVtbl[10]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public D3D12_NODE_ID GetEntrypointID(uint WorkGraphIndex, uint EntrypointIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint, D3D12_NODE_ID>)(lpVtbl[11]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, EntrypointIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public uint GetEntrypointIndex(uint WorkGraphIndex, D3D12_NODE_ID NodeID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, D3D12_NODE_ID, uint>)(lpVtbl[12]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, NodeID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public uint GetEntrypointRecordSizeInBytes(uint WorkGraphIndex, uint EntrypointIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint, uint>)(lpVtbl[13]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, EntrypointIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public void GetWorkGraphMemoryRequirements(uint WorkGraphIndex, D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS* pWorkGraphMemoryRequirements)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS*, void>)(lpVtbl[14]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, pWorkGraphMemoryRequirements);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public uint GetEntrypointRecordAlignmentInBytes(uint WorkGraphIndex, uint EntrypointIndex)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12WorkGraphProperties*, uint, uint, uint>)(lpVtbl[15]))((ID3D12WorkGraphProperties*)Unsafe.AsPointer(ref this), WorkGraphIndex, EntrypointIndex);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        uint GetNumWorkGraphs();

        [VtblIndex(4)]
        [return: NativeTypeName("LPCWSTR")]
        char* GetProgramName(uint WorkGraphIndex);

        [VtblIndex(5)]
        uint GetWorkGraphIndex([NativeTypeName("LPCWSTR")] char* pProgramName);

        [VtblIndex(6)]
        uint GetNumNodes(uint WorkGraphIndex);

        [VtblIndex(7)]
        D3D12_NODE_ID GetNodeID(uint WorkGraphIndex, uint NodeIndex);

        [VtblIndex(8)]
        uint GetNodeIndex(uint WorkGraphIndex, D3D12_NODE_ID NodeID);

        [VtblIndex(9)]
        uint GetNodeLocalRootArgumentsTableIndex(uint WorkGraphIndex, uint NodeIndex);

        [VtblIndex(10)]
        uint GetNumEntrypoints(uint WorkGraphIndex);

        [VtblIndex(11)]
        D3D12_NODE_ID GetEntrypointID(uint WorkGraphIndex, uint EntrypointIndex);

        [VtblIndex(12)]
        uint GetEntrypointIndex(uint WorkGraphIndex, D3D12_NODE_ID NodeID);

        [VtblIndex(13)]
        uint GetEntrypointRecordSizeInBytes(uint WorkGraphIndex, uint EntrypointIndex);

        [VtblIndex(14)]
        void GetWorkGraphMemoryRequirements(uint WorkGraphIndex, D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS* pWorkGraphMemoryRequirements);

        [VtblIndex(15)]
        uint GetEntrypointRecordAlignmentInBytes(uint WorkGraphIndex, uint EntrypointIndex);
    }
}
