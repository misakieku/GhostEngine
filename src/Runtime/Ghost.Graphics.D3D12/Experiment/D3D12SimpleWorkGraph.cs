using Ghost.Core;
using Ghost.Graphics.RHI;
using TerraFX.Interop.DirectX;
using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12.Experiment;

internal unsafe class D3D12SimpleWorkGraph : IDisposable
{
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private ID3D12StateObject* _stateObject;
    private ID3D12StateObjectProperties1* _stateObjectProperties;
    private ID3D12WorkGraphProperties* _workGraphProperties;

    // Contains Opaque ID data used by the Runtime to identify the WG
    private D3D12_PROGRAM_IDENTIFIER _programIdentifier;

    // Auto-allocated Backing Memory for WG Queues and Nodes state
    private Handle<GPUBuffer> _backingMemory;
    private D3D12_GPU_VIRTUAL_ADDRESS_RANGE _backingMemoryRange;

    // First time dispatching a Work Graph requires INITIALIZE flag
    private bool _isInitialized;

    public D3D12SimpleWorkGraph(
        D3D12RenderDevice device,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        ID3D12RootSignature* globalRootSignature,
        ReadOnlySpan<byte> bytecode,
        string programName)
    {
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;

        var subobjects = stackalloc D3D12_STATE_SUBOBJECT[3];

        fixed (byte* pBytecode = bytecode)
        fixed (char* pProgramName = programName)
        {
            // 1. DXIL Library Subobject
            var dxilLibDesc = new D3D12_DXIL_LIBRARY_DESC
            {
                DXILLibrary = new D3D12_SHADER_BYTECODE
                {
                    pShaderBytecode = pBytecode,
                    BytecodeLength = (nuint)bytecode.Length
                },
                NumExports = 0,
                pExports = null
            };

            subobjects[0].Type = D3D12_STATE_SUBOBJECT_TYPE_DXIL_LIBRARY;
            subobjects[0].pDesc = &dxilLibDesc;

            // 2. Global Root Signature Subobject
            // This is shared among all Nodes. Perfect for passing your bindless Descriptor Heaps 
            // and root constants (which hold your Buffer IDs and Sampler Indices).
            var rootSigDesc = new D3D12_GLOBAL_ROOT_SIGNATURE
            {
                pGlobalRootSignature = globalRootSignature
            };

            subobjects[1].Type = D3D12_STATE_SUBOBJECT_TYPE_GLOBAL_ROOT_SIGNATURE;
            subobjects[1].pDesc = &rootSigDesc;

            // 3. Work Graph Properties Subobject
            var workGraphDesc = new D3D12_WORK_GRAPH_DESC
            {
                ProgramName = pProgramName,
                Flags = D3D12_WORK_GRAPH_FLAG_INCLUDE_ALL_AVAILABLE_NODES
            };

            subobjects[2].Type = D3D12_STATE_SUBOBJECT_TYPE_WORK_GRAPH;
            subobjects[2].pDesc = &workGraphDesc;

            // Tie them to a state object descriptor
            var stateObjectDesc = new D3D12_STATE_OBJECT_DESC
            {
                Type = D3D12_STATE_OBJECT_TYPE_EXECUTABLE,
                NumSubobjects = 3,
                pSubobjects = subobjects
            };

            // Build State Object!
            var pDevice = device.NativeObject.Get();
            ID3D12StateObject* pStateObject;
            ThrowIfFailed(pDevice->CreateStateObject(&stateObjectDesc, __uuidof<ID3D12StateObject>(), (void**)&pStateObject));
            _stateObject = pStateObject;

            ID3D12StateObjectProperties1* pStateObjectProps;
            ThrowIfFailed(pStateObject->QueryInterface(__uuidof<ID3D12StateObjectProperties1>(), (void**)&pStateObjectProps));
            _stateObjectProperties = pStateObjectProps;

            ID3D12WorkGraphProperties* pWorkGraphProps;
            ThrowIfFailed(pStateObject->QueryInterface(__uuidof<ID3D12WorkGraphProperties>(), (void**)&pWorkGraphProps));
            _workGraphProperties = pWorkGraphProps;

            // Extract Program Identifier - we'll pass this via CommandList later.
            _programIdentifier = pStateObjectProps->GetProgramIdentifier(pProgramName);
            var workGraphIndex = pWorkGraphProps->GetWorkGraphIndex(pProgramName);

            // Compute Backing Memory needed
            D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS memReqs;
            pWorkGraphProps->GetWorkGraphMemoryRequirements(workGraphIndex, &memReqs);

            // Allocate Backing Memory Buffer using your custom allocator!
            var backingMemoryDesc = new BufferDesc
            {
                Size = memReqs.MaxSizeInBytes,
                Usage = BufferUsage.UnorderedAccess, // Backing memory MUST be an Unordered Access resource.
                HeapType = HeapType.Default
            };

            _backingMemory = _resourceAllocator.CreateBuffer(in backingMemoryDesc, $"{programName}_BackingMemory");
            var backingBufferResource = _resourceDatabase.GetResource(_backingMemory.AsResource());

            _backingMemoryRange = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE
            {
                StartAddress = backingBufferResource.Get()->GetGPUVirtualAddress(),
                SizeInBytes = memReqs.MaxSizeInBytes
            };
        }
    }

    // You invoke this from outside passing records via CPU array to test immediately.
    public void Dispatch<TRecord>(D3D12CommandBuffer cmdBuffer, uint entryPointIndex, ReadOnlySpan<TRecord> records)
        where TRecord : unmanaged
    {
        ID3D12GraphicsCommandList10* pCmdList = default;
        ThrowIfFailed(cmdBuffer.NativeObject.Get()->QueryInterface(__uuidof(pCmdList), (void**)&pCmdList));

        try
        {
            // 1. Prepare Program descriptors
            var setProgramDesc = new D3D12_SET_PROGRAM_DESC
            {
                Type = D3D12_PROGRAM_TYPE_WORK_GRAPH,
                WorkGraph = new D3D12_SET_WORK_GRAPH_DESC
                {
                    ProgramIdentifier = _programIdentifier,
                    Flags = _isInitialized ? D3D12_SET_WORK_GRAPH_FLAG_NONE : D3D12_SET_WORK_GRAPH_FLAG_INITIALIZE,
                    BackingMemory = _backingMemoryRange,
                    NodeLocalRootArgumentsTable = default // Ignored since we are pure bindless setup!
                }
            };

            _isInitialized = true;

            pCmdList->SetProgram(&setProgramDesc);

            // 2. Execute! Map CPU inputs natively directly.
            fixed (TRecord* pRecords = records)
            {
                var cpuInput = new D3D12_NODE_CPU_INPUT
                {
                    EntrypointIndex = entryPointIndex,
                    NumRecords = (uint)records.Length,
                    pRecords = pRecords,
                    RecordStrideInBytes = (uint)sizeof(TRecord)
                };

                var dispatchDesc = new D3D12_DISPATCH_GRAPH_DESC
                {
                    Mode = D3D12_DISPATCH_MODE_NODE_CPU_INPUT,
                    NodeCPUInput = cpuInput
                };

                pCmdList->DispatchGraph(&dispatchDesc);
            }
        }
        finally
        {
            pCmdList->Release();
        }
    }

    public void Dispose()
    {
        if (_backingMemory.IsValid)
        {
            _resourceDatabase.ReleaseResourceImmediately(_backingMemory.AsResource());
            _backingMemory = Handle<GPUBuffer>.Invalid;
        }

        if (_workGraphProperties != null)
        {
            _workGraphProperties->Release();
            _workGraphProperties = null;
        }

        if (_stateObjectProperties != null)
        {
            _stateObjectProperties->Release();
            _stateObjectProperties = null;
        }

        if (_stateObject != null)
        {
            _stateObject->Release();
            _stateObject = null;
        }
    }
}
