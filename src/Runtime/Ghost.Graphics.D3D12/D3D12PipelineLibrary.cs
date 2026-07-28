using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

internal struct D3D12PipelineState : IDisposable
{
    public UniquePtr<ID3D12PipelineState> pso;
    public Key64<ShaderVariant> shaderVariant;
    public ulong contentHash;

    public void Dispose()
    {
        pso.Dispose();
    }
}

internal unsafe class D3D12PipelineLibrary : D3D12Object<ID3D12PipelineLibrary1>, IPipelineLibrary
{
    private readonly D3D12RenderDevice _device;

    private UniquePtr<ID3D12RootSignature> _defaultRootSignature;

    private UnsafeHashMap<UInt128, D3D12PipelineState> _pipelineCache;

    private struct StalePipeline
    {
        public UInt128 pipelineKey;
        public D3D12PipelineState pso;
        public ulong frameAdded;
    }

    private UnsafeList<StalePipeline> _stalePipelines;
    private ulong _currentCpuFrame;
    private ulong _completedGpuFrame;

    public ID3D12RootSignature* DefaultRootSignature => _defaultRootSignature.Get();

    private static ID3D12PipelineLibrary1* CreateLibrary(D3D12RenderDevice device, string? filePath)
    {
        ID3D12PipelineLibrary1* pLibrary = default;

        if (File.Exists(filePath))
        {
            var fileBytes = File.ReadAllBytes(filePath);
            fixed (byte* pFileBytes = fileBytes)
            {
                ThrowIfFailed(device.NativeObject.Get()->CreatePipelineLibrary(pFileBytes, (nuint)fileBytes.Length, __uuidof(pLibrary), (void**)&pLibrary));
            }
        }
        else
        {
            ThrowIfFailed(device.NativeObject.Get()->CreatePipelineLibrary(null, 0, __uuidof(pLibrary), (void**)&pLibrary));
        }

        return pLibrary;
    }

    public D3D12PipelineLibrary(D3D12RenderDevice device)
        : base(CreateLibrary(device, null)) // TODO: we need to path to load the existing library from disk.
    {
        _device = device;

        _pipelineCache = new UnsafeHashMap<UInt128, D3D12PipelineState>(32, AllocationHandle.Persistent);
        _stalePipelines = new UnsafeList<StalePipeline>(16, AllocationHandle.Persistent);

        CreateDefaultRootSignature().ThrowIfFailed();
    }

    private Result CreateDefaultRootSignature()
    {
        _defaultRootSignature = default;

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up viewGroup tables.
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER1[RootSignatureLayout.ROOT_PARAMETER_COUNT];

        rootParameters[0] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Constants = new D3D12_ROOT_CONSTANTS
            {
                ShaderRegister = 0, // b0
                RegisterSpace = 0,  // space0
                Num32BitValues = PushConstantsData.NUM_32BITS_VALUE // 3
            }
        };

        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC1
        {
            NumParameters = RootSignatureLayout.ROOT_PARAMETER_COUNT,
            pParameters = rootParameters,
            NumStaticSamplers = 0,
            pStaticSamplers = null,
            Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT
                | D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
                | D3D12_ROOT_SIGNATURE_FLAG_SAMPLER_HEAP_DIRECTLY_INDEXED
        };

        var versionedDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC
        {
            Version = D3D_ROOT_SIGNATURE_VERSION_1_1,
            Desc_1_1 = rootSignatureDesc
        };

        using ComPtr<ID3DBlob> pSignature = default;
        using ComPtr<ID3DBlob> pError = default;

        var serializeResult = D3D12SerializeVersionedRootSignature(&versionedDesc, pSignature.GetAddressOf(), pError.GetAddressOf());
        if (serializeResult.FAILED)
        {
            var errorMsg = pError.Get() != null ? Marshal.PtrToStringUTF8((nint)pError.Get()->GetBufferPointer()) : "Unknown error";
            return Result.Failure($"Failed to serialize default root signature: {errorMsg}");
        }

        ID3D12RootSignature* pRootSignature = default;
        ThrowIfFailed(_device.NativeObject.Get()->CreateRootSignature(0, pSignature.Get()->GetBufferPointer(), pSignature.Get()->GetBufferSize(),
            __uuidof(pRootSignature), (void**)&pRootSignature));

        _defaultRootSignature.Attach(pRootSignature);

        return Result.Success();
    }

    public void SaveLibraryToDisk(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Directory does not exist: {dir}");
        }

        var size = pNativeObject->GetSerializedSize();
        using var buffer = new UnsafeArray<byte>((int)size, AllocationHandle.Persistent); // We use persistent Heap allocation instead of stack allocation to avoid stack overflow for large pipeline libraries.

        ThrowIfFailed(pNativeObject->Serialize(buffer.GetUnsafePtr(), size));

        using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(buffer.AsSpan());
    }

    private static D3D12_DEPTH_STENCIL_DESC BuildDepthStencil(ZTest ztest, ZWrite zwrite)
    {
        var depthEnabled = ztest != ZTest.Disabled;
        var writeEnabled = zwrite == ZWrite.On;
        var cmp = ztest.ToD3DCompare();
        return D3D12Utility.D3D12_DEPTH_STENCIL_DESC_CREATE(depthEnabled, writeEnabled, cmp);
    }

    private Result CreatePSO(ulong contentHash, Key64<ShaderVariant> shaderVariantKey, UInt128 pipelineKey, D3D12_PIPELINE_STATE_STREAM_DESC* pStreamDesc)
    {
        ID3D12PipelineState* pPipelineState = default;

        var pKeyStr = stackalloc char[33]; // 32 for 128 bits key + 1 for null terminator
        var keySpan = new Span<char>(pKeyStr, 33);
        if (!RHIUtility.TryGetStringFromHash(pipelineKey, keySpan))
        {
            return Result.Failure("Failed to convert pipeline key to string.");
        }

        var hr = pNativeObject->LoadPipeline(pKeyStr, pStreamDesc, __uuidof(pPipelineState), (void**)&pPipelineState);

        if (hr == E.E_INVALIDARG)
        {
            // Pipeline not found in the library, create a new one.
            ThrowIfFailed(_device.NativeObject.Get()->CreatePipelineState(pStreamDesc, __uuidof(pPipelineState), (void**)&pPipelineState));
            ThrowIfFailed(pNativeObject->StorePipeline(pKeyStr, pPipelineState));
        }
        else
        {
            ThrowIfFailed(hr);
        }

        D3D12PipelineState pso = default;
        pso.shaderVariant = shaderVariantKey;
        pso.contentHash = contentHash;
        pso.pso.Attach(pPipelineState);

        _pipelineCache[pipelineKey] = pso;
        return Result.Success();
    }

    public Result<Key128<PipelineState>> CreateGraphicsPipeline(scoped in GraphicsPSODesc desc)
    {
        AssertNotDisposed();

        if (desc.RtvFormats.Length > D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT)
        {
            return Result.Failure($"RTV format count exceeds the maximum supported render target count of {D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT}.");
        }

        var passAttachmentKey = new PassAttachmentHash(desc.RtvFormats, desc.DsvFormat);
        var pipelineKey = RHIUtility.CreateGraphicsPipelineKey(desc.CompiledHash, desc.PipelineOption, passAttachmentKey);

        if (!_pipelineCache.ContainsKey(pipelineKey))
        {
            fixed (byte* pASByteCode = desc.AsCode, pMSByteCode = desc.MsCode, pPSByteCode = desc.PsCode)
            {
                var msPipelinedesc = new D3DX12_MESH_SHADER_PIPELINE_STATE_DESC
                {
                    pRootSignature = _defaultRootSignature.Get(),
                    MS = new D3D12_SHADER_BYTECODE(pMSByteCode, (nuint)desc.MsCode.Length),
                    PS = new D3D12_SHADER_BYTECODE(pPSByteCode, (nuint)desc.PsCode.Length),
                    PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                    SampleMask = UINT32_MAX,
                    SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                    NumRenderTargets = (uint)desc.RtvFormats.Length,
                    DSVFormat = desc.DsvFormat.ToDXGIFormat(),
                    DepthStencilState = BuildDepthStencil(desc.PipelineOption.ZTest, desc.PipelineOption.ZWrite),
                    NodeMask = 0,
                    Flags = D3D12_PIPELINE_STATE_FLAG_NONE,

                    BlendState = desc.PipelineOption.Blend switch
                    {
                        Blend.Opaque => D3D12Utility.D3D12_BLEND_DESC_OPAQUE,
                        Blend.Alpha => D3D12Utility.D3D12_BLEND_DESC_ALPHA_BLEND,
                        Blend.Additive => D3D12Utility.D3D12_BLEND_DESC_ADDITIVE,
                        Blend.Multiply => D3D12Utility.D3D12_BLEND_DESC_MULTIPLY,
                        Blend.PremultipliedAlpha => D3D12Utility.D3D12_BLEND_DESC_PREMULTIPLIED,
                        _ => D3D12Utility.D3D12_BLEND_DESC_OPAQUE
                    },
                    RasterizerState = desc.PipelineOption.Cull switch
                    {
                        Cull.Off => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE,
                        Cull.Front => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_CLOCKWISE,
                        Cull.Back => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_COUNTER_CLOCKWISE,
                        _ => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE
                    },
                };

                if (desc.AsCode.Length != 0)
                {
                    msPipelinedesc.AS = new D3D12_SHADER_BYTECODE(pASByteCode, (nuint)desc.AsCode.Length);
                }

                for (var i = 0; i < desc.RtvFormats.Length; i++)
                {
                    msPipelinedesc.RTVFormats[i] = desc.RtvFormats[i].ToDXGIFormat();
                    msPipelinedesc.BlendState.RenderTarget[i].RenderTargetWriteMask = (byte)((int)desc.PipelineOption.ColorMask & 0x0F);
                }

                var meshStream = new CD3DX12_PIPELINE_MESH_STATE_STREAM(in msPipelinedesc);
                var streamDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
                {
                    pPipelineStateSubobjectStream = &meshStream,
                    SizeInBytes = (nuint)sizeof(CD3DX12_PIPELINE_MESH_STATE_STREAM)
                };

                var result = CreatePSO(desc.CompiledHash, desc.VariantKey, pipelineKey, &streamDesc);
                if (result.IsFailure)
                {
                    return result;
                }
            }
        }

        return pipelineKey;
    }

    public Result<Key128<PipelineState>> CreateComputePipeline(scoped in ComputePSODesc desc)
    {
        AssertNotDisposed();

        var pipelineKey = RHIUtility.CreateComputePipelineKey(desc.CompiledHash);
        if (!_pipelineCache.ContainsKey(pipelineKey))
        {
            fixed (byte* pCSByteCode = desc.CsCode)
            {
                var byteCode = new D3D12_SHADER_BYTECODE(pCSByteCode, (nuint)desc.CsCode.Length);
                var csPipelineDesc = new CD3DX12_PIPELINE_STATE_STREAM_CS(in byteCode);

                var streamDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
                {
                    pPipelineStateSubobjectStream = &csPipelineDesc,
                    SizeInBytes = (nuint)sizeof(CD3DX12_PIPELINE_STATE_STREAM_CS)
                };

                var result = CreatePSO(desc.CompiledHash, desc.VariantKey, pipelineKey, &streamDesc);
                if (result.IsFailure)
                {
                    return result;
                }
            }
        }

        return pipelineKey;
    }

    public bool HasPipelineStateObject(UInt128 key)
    {
        AssertNotDisposed();
        return _pipelineCache.ContainsKey(key);
    }

    public Result<SharedPtr<ID3D12PipelineState>, Error> GetPipelineStateObject(UInt128 key)
    {
        AssertNotDisposed();
        if (_pipelineCache.TryGetValue(key, out var cacheEntry))
        {
            return cacheEntry.pso.Share();
        }

        return Error.NotFound;
    }

    public void BeginFrame(ulong cpuFrame)
    {
        _currentCpuFrame = cpuFrame;
    }

    public void EndFrame(ulong gpuFrame)
    {
        _completedGpuFrame = gpuFrame;

        // Process stale pipelines and dispose them if they are no longer in flight
        for (var i = _stalePipelines.Count - 1; i >= 0; i--)
        {
            var stale = _stalePipelines[i];
            if (_completedGpuFrame >= stale.frameAdded)
            {
                stale.pso.Dispose();
                _stalePipelines.RemoveAtSwapBack(i);
            }
        }
    }

    public void EvictStalePipelines(ulong oldContentHash)
    {
        // Find all pipelines with matching oldContentHash
        using var keysToRemove = new UnsafeList<UInt128>(8, AllocationHandle.Temp);

        foreach (var kvp in _pipelineCache)
        {
            if (kvp.Value.contentHash == oldContentHash)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (_pipelineCache.TryGetValue(key, out var pso))
            {
                _stalePipelines.Add(new StalePipeline
                {
                    pipelineKey = key,
                    pso = pso,
                    frameAdded = _currentCpuFrame
                });
                _pipelineCache.Remove(key);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var kvp in _pipelineCache)
        {
            kvp.Value.Dispose();
        }

        _pipelineCache.Dispose();
        _stalePipelines.Dispose();
        _defaultRootSignature.Dispose();
    }
}
