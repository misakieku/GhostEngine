using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12WorkGraphProgram : D3D12Object<ID3D12StateObject>, IWorkGraphProgram
{
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private ID3D12StateObjectProperties1* _stateObjectProperties;
    private ID3D12WorkGraphProperties* _workGraphProperties;

    private D3D12_PROGRAM_IDENTIFIER _d3d12ProgramIdentifier;
    private ProgramIdentifier _programIdentifier;

    private Handle<GPUBuffer> _backingMemory;
    private ulong _backingMemoryAddress;
    private ulong _backingMemorySize;
    private uint _workGraphIndex;

    private bool _isInitialized;

    public ProgramIdentifier ProgramIdentifier => _programIdentifier;
    public Handle<GPUBuffer> BackingMemoryBuffer => _backingMemory;
    public ulong BackingMemoryAddress => _backingMemoryAddress;
    public ulong BackingMemorySize => _backingMemorySize;
    public bool IsInitialized => _isInitialized;

    internal D3D12WorkGraphProgram(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase, D3D12ResourceAllocator resourceAllocator, ID3D12RootSignature* globalRootSignature, ReadOnlySpan<byte> bytecode, string programName)
        : base(CreateStateObject(device, globalRootSignature, bytecode, programName))
    {
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;

        var pStateObject = pNativeObject;
        ID3D12StateObjectProperties1* pStateObjectProps;
        ThrowIfFailed(pStateObject->QueryInterface(__uuidof<ID3D12StateObjectProperties1>(), (void**)&pStateObjectProps));
        _stateObjectProperties = pStateObjectProps;

        ID3D12WorkGraphProperties* pWorkGraphProps;
        ThrowIfFailed(pStateObject->QueryInterface(__uuidof<ID3D12WorkGraphProperties>(), (void**)&pWorkGraphProps));
        _workGraphProperties = pWorkGraphProps;

        fixed (char* pProgramName = programName)
        {
            _d3d12ProgramIdentifier = pStateObjectProps->GetProgramIdentifier(pProgramName);
            fixed (void* pDst = &_programIdentifier)
            fixed (void* pSrc = &_d3d12ProgramIdentifier)
            {
                Unsafe.CopyBlock(pDst, pSrc, (uint)sizeof(D3D12_PROGRAM_IDENTIFIER));
            }

            _workGraphIndex = pWorkGraphProps->GetWorkGraphIndex(pProgramName);
            if (_workGraphIndex == uint.MaxValue)
            {
                Logger.Error($"[D3D12WorkGraphProgram] Failed to find work graph index for '{programName}'!");
            }

            D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS memReqs;
            pWorkGraphProps->GetWorkGraphMemoryRequirements(_workGraphIndex, &memReqs);

            _backingMemorySize = memReqs.MaxSizeInBytes;
            if (_backingMemorySize > 0)
            {
                var backingDesc = new BufferDesc
                {
                    Size = _backingMemorySize,
                    Stride = 4,
                    Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess,
                    HeapType = HeapType.Default
                };

                _backingMemory = _resourceAllocator.CreateBuffer(in backingDesc, $"{programName}_BackingMemory");
                var backingResource = _resourceDatabase.GetResource(_backingMemory.AsResource());
                _backingMemoryAddress = backingResource.Get()->GetGPUVirtualAddress();
            }
        }
    }

    private static ID3D12StateObject* CreateStateObject(D3D12RenderDevice device, ID3D12RootSignature* globalRootSignature, ReadOnlySpan<byte> bytecode, string programName)
    {
        var subobjects = stackalloc D3D12_STATE_SUBOBJECT[3];

        fixed (byte* pBytecode = bytecode)
        fixed (char* pProgramName = programName)
        {
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

            var rootSigDesc = new D3D12_GLOBAL_ROOT_SIGNATURE
            {
                pGlobalRootSignature = globalRootSignature
            };

            subobjects[1].Type = D3D12_STATE_SUBOBJECT_TYPE_GLOBAL_ROOT_SIGNATURE;
            subobjects[1].pDesc = &rootSigDesc;

            var workGraphDesc = new D3D12_WORK_GRAPH_DESC
            {
                ProgramName = pProgramName,
                Flags = D3D12_WORK_GRAPH_FLAG_INCLUDE_ALL_AVAILABLE_NODES
            };

            subobjects[2].Type = D3D12_STATE_SUBOBJECT_TYPE_WORK_GRAPH;
            subobjects[2].pDesc = &workGraphDesc;

            var stateObjectDesc = new D3D12_STATE_OBJECT_DESC
            {
                Type = D3D12_STATE_OBJECT_TYPE_EXECUTABLE,
                NumSubobjects = 3,
                pSubobjects = subobjects
            };

            var pDevice = device.NativeObject.Get();
            ID3D12StateObject* pStateObject;
            ThrowIfFailed(pDevice->CreateStateObject(&stateObjectDesc, __uuidof<ID3D12StateObject>(), (void**)&pStateObject));
            return pStateObject;
        }
    }

    public uint GetEntrypointIndex(string nodeName)
    {
        if (_workGraphProperties == null)
        {
            return 0;
        }

        fixed (char* pNodeName = nodeName)
        {
            var nodeId = new D3D12_NODE_ID
            {
                Name = pNodeName,
                ArrayIndex = 0
            };
            var index = _workGraphProperties->GetEntrypointIndex(_workGraphIndex, nodeId);
            if (index == uint.MaxValue)
            {
                Logger.Warning($"[D3D12WorkGraphProgram] Entrypoint '{nodeName}' was NOT found in work graph (Index {_workGraphIndex})!");
            }

            return index;
        }
    }

    public void MarkInitialized()
    {
        _isInitialized = true;
    }

    public void Bind(ICommandBuffer cmdBuffer)
    {
        var flags = _isInitialized ? SetWorkGraphFlags.None : SetWorkGraphFlags.Initialize;
        var setProgramDesc = SetProgramDesc.ForWorkGraph(
            _programIdentifier,
            _backingMemoryAddress,
            _backingMemorySize,
            flags);

        _isInitialized = true;
        cmdBuffer.SetProgram(in setProgramDesc);
    }

    public void Dispatch(ICommandBuffer cmdBuffer, scoped in DispatchGraphDesc desc)
    {
        cmdBuffer.DispatchGraph(in desc);
    }

    public void DispatchCPU<T>(ICommandBuffer cmdBuffer, uint entryPointIndex, ReadOnlySpan<T> records)
        where T : unmanaged
    {
        Bind(cmdBuffer);
        fixed (T* pRecords = records)
        {
            var dispatchDesc = DispatchGraphDesc.ForCPUInput(entryPointIndex, (uint)records.Length, pRecords, (ulong)sizeof(T));
            cmdBuffer.DispatchGraph(in dispatchDesc);
        }
    }

    public void DispatchGPU(ICommandBuffer cmdBuffer, ulong gpuVirtualAddress)
    {
        Bind(cmdBuffer);
        var dispatchDesc = DispatchGraphDesc.ForGPUInput(gpuVirtualAddress);
        cmdBuffer.DispatchGraph(in dispatchDesc);
    }

    protected override void Dispose(bool disposing)
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

        base.Dispose(disposing);
    }
}
