using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Core;
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
    public D3DX12_MESH_SHADER_PIPELINE_STATE_DESC psoDesc;
    public UniquePtr<ID3D12PipelineState> pso;
    public Key64<ShaderVariant> shaderVariant;

    public void Dispose()
    {
        pso.Dispose();
    }
}

internal unsafe class D3D12PipelineLibrary : IPipelineLibrary
{
    private readonly D3D12RenderDevice _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private UniquePtr<ID3D12PipelineLibrary1> _library;
    private UniquePtr<ID3D12RootSignature> _defaultRootSignature;

    private readonly Dictionary<Key128<GraphicsPipeline>, D3D12PipelineState> _pipelineCache;

    public ID3D12RootSignature* DefaultRootSignature => _defaultRootSignature.Get();

    public D3D12PipelineLibrary(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase)
    {
        _device = device;
        _resourceDatabase = resourceDatabase;

        _pipelineCache = new Dictionary<Key128<GraphicsPipeline>, D3D12PipelineState>();

        CreateDefaultRootSignature().ThrowIfFailed();
    }

    // TODO: Maybe we don't need 4 root signature. We can use bindless for global, per-view, and per-object buffers as well.
    private Result CreateDefaultRootSignature()
    {
        _defaultRootSignature = default;

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up viewGroup tables.
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER1[RootSignatureLayout.ROOT_PARAMETER_COUNT];

#if false
        rootParameters[0] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(RootSignatureLayout.GLOBAL_BUFFER_SLOT, 0), // b0
        };

        rootParameters[1] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(RootSignatureLayout.PER_VIEW_BUFFER_SLOT, 0), // b1
        };

        rootParameters[2] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(RootSignatureLayout.PER_OBJECT_BUFFER_SLOT, 0), // b2
        };

        rootParameters[3] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, 0), // b3
        };
#else
        rootParameters[0] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Constants = new D3D12_ROOT_CONSTANTS
            {
                ShaderRegister = 0, // b0
                RegisterSpace = 0,  // space0
                Num32BitValues = 4  // Global, View, Object, Material indices
            }
        };
#endif
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
        ThrowIfFailed(_device.NativeDevice.Get()->CreateRootSignature(0, pSignature.Get()->GetBufferPointer(), pSignature.Get()->GetBufferSize(),
            __uuidof(pRootSignature), (void**)&pRootSignature));

        _defaultRootSignature.Attach(pRootSignature);

        return Result.Success();
    }

    public void InitializeLibrary(string? filePath)
    {
        ID3D12PipelineLibrary1* pLibrary = default;

        if (File.Exists(filePath))
        {
            var fileBytes = File.ReadAllBytes(filePath);
            fixed (byte* pFileBytes = fileBytes)
            {
                ThrowIfFailed(_device.NativeDevice.Get()->CreatePipelineLibrary(pFileBytes, (nuint)fileBytes.Length, __uuidof(pLibrary), (void**)&pLibrary));
            }
        }
        else
        {
            ThrowIfFailed(_device.NativeDevice.Get()->CreatePipelineLibrary(null, 0, __uuidof(pLibrary), (void**)&pLibrary));
        }

        _library.Attach(pLibrary);
    }

    public void SaveLibraryToDisk(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Directory does not exist: {dir}");
        }

        var size = _library.Get()->GetSerializedSize();
        using var buffer = new UnsafeArray<byte>((int)size, Allocator.Persistent); // We use persistent Heap allocation instead of stack allocation to avoid stack overflow for large pipeline libraries.

        ThrowIfFailed(_library.Get()->Serialize(buffer.GetUnsafePtr(), size));

        using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(buffer.AsSpan());
    }

    private static Result<CBufferInfo> ValidateReflectionData(ShaderReflectionData reflectionData)
    {
        if (reflectionData.ResourcesBindings.Count != RootSignatureLayout.ROOT_PARAMETER_COUNT)
        {
            return Result.Failure($"Shader must use all {RootSignatureLayout.ROOT_PARAMETER_COUNT} constant buffer slots defined in the root signature.");
        }

        var rootConstant = reflectionData.ResourcesBindings[0];
        if (rootConstant.Type != ShaderInputType.ConstantBuffer)
        {
            return Result.Failure($"Root constant parameter must be a constant buffer.");
        }

        if (rootConstant.Size != sizeof(PushConstantsData))
        {
            return Result.Failure($"Root constant buffer size must be {sizeof(PushConstantsData)} bytes.");
        }

        var cbufferInfo = new CBufferInfo
        {
            Name = rootConstant.Name,
            RegisterSlot = rootConstant.BindPoint,
            RegisterSpace = rootConstant.Space,
            SizeInBytes = rootConstant.Size,
            Properties = rootConstant.Properties
        };

        return Result.Success(cbufferInfo);
    }

    private static D3D12_COMPARISON_FUNC ToD3DCompare(ZTest z) => z switch
    {
        ZTest.Disabled => D3D12_COMPARISON_FUNC_NEVER,
        ZTest.Less => D3D12_COMPARISON_FUNC_LESS,
        ZTest.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
        ZTest.Equal => D3D12_COMPARISON_FUNC_EQUAL,
        ZTest.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
        ZTest.Greater => D3D12_COMPARISON_FUNC_GREATER,
        ZTest.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
        ZTest.Always => D3D12_COMPARISON_FUNC_ALWAYS,
        _ => D3D12_COMPARISON_FUNC_LESS_EQUAL
    };

    private static D3D12_DEPTH_STENCIL_DESC BuildDepthStencil(ZTest ztest, ZWrite zwrite)
    {
        var depthEnabled = ztest != ZTest.Disabled;
        var writeEnabled = zwrite == ZWrite.On;
        var cmp = ToD3DCompare(ztest);
        return D3D12Utility.D3D12_DEPTH_STENCIL_DESC_CREATE(depthEnabled, writeEnabled, cmp);
    }

    public Result<Key128<GraphicsPipeline>> CompilePSO(ref readonly GraphicsPSODescriptor descriptor, ref readonly GraphicsCompiledResult compiled)
    {
        static Result<CBufferInfo> ValidatePassReflectionData(ref readonly GraphicsCompiledResult compiled)
        {
            var msr = ValidateReflectionData(compiled.msResult.reflectionData);
            if (msr.IsFailure)
            {
                return Result.Failure("Validation of mesh shader reflection data failed: " + msr.Message);
            }

            var psr = ValidateReflectionData(compiled.psResult.reflectionData);
            if (psr.IsFailure)
            {
                return Result.Failure("Validation of pixel shader reflection data failed: " + psr.Message);
            }

            if (msr.Value.Properties != null
                && msr.Value.SizeInBytes != psr.Value.SizeInBytes)
            {
                return Result.Failure("Mesh shader and pixel shader constant buffer layouts do not match.");
            }

            if (compiled.tsResult.IsCreated)
            {
                var tsr = ValidateReflectionData(compiled.tsResult.reflectionData);
                if (tsr.IsFailure)
                {
                    return Result.Failure("Validation of task shader reflection data failed: " + tsr.Message);
                }

                if (tsr.Value.Properties != null
                    && tsr.Value.SizeInBytes != psr.Value.SizeInBytes)
                {
                    return Result.Failure("Task shader and pixel shader constant buffer layouts do not match.");
                }
            }

            // ts and ms may not use per material cbuffer at all, so we return the psr value.
            return psr.Value;
        }

        if (descriptor.RtvFormats.Length > D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT)
        {
            return Result.Failure($"RTV format count exceeds the maximum supported render target count of {D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT}.");
        }

        var passPipelineKey = new PassPipelineHash(descriptor.RtvFormats, descriptor.DsvFormat);
        var pipelineKey = RHIUtility.CreateGraphicsPipelineKey(descriptor.VariantKey, descriptor.PipelineOption, passPipelineKey);

        if (!_pipelineCache.ContainsKey(pipelineKey))
        {
            var result = ValidatePassReflectionData(in compiled);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            var desc = new D3DX12_MESH_SHADER_PIPELINE_STATE_DESC
            {
                pRootSignature = _defaultRootSignature.Get(),
                MS = new D3D12_SHADER_BYTECODE(compiled.msResult.bytecode.GetUnsafePtr(), (nuint)compiled.msResult.bytecode.Count),
                PS = new D3D12_SHADER_BYTECODE(compiled.psResult.bytecode.GetUnsafePtr(), (nuint)compiled.psResult.bytecode.Count),
                PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                SampleMask = UINT32_MAX,
                SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                NumRenderTargets = (uint)descriptor.RtvFormats.Length,
                DSVFormat = descriptor.DsvFormat.ToDXGIFormat(),
                DepthStencilState = BuildDepthStencil(descriptor.PipelineOption.ZTest, descriptor.PipelineOption.ZWrite),
                NodeMask = 0,
                Flags = D3D12_PIPELINE_STATE_FLAG_NONE,

                BlendState = descriptor.PipelineOption.Blend switch
                {
                    Blend.Opaque => D3D12Utility.D3D12_BLEND_DESC_OPAQUE,
                    Blend.Alpha => D3D12Utility.D3D12_BLEND_DESC_ALPHA_BLEND,
                    Blend.Additive => D3D12Utility.D3D12_BLEND_DESC_ADDITIVE,
                    Blend.Multiply => D3D12Utility.D3D12_BLEND_DESC_MULTIPLY,
                    Blend.PremultipliedAlpha => D3D12Utility.D3D12_BLEND_DESC_PREMULTIPLIED,
                    _ => D3D12Utility.D3D12_BLEND_DESC_OPAQUE
                },
                RasterizerState = descriptor.PipelineOption.Cull switch
                {
                    Cull.Off => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE,
                    Cull.Front => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_CLOCKWISE,
                    Cull.Back => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_COUNTER_CLOCKWISE,
                    _ => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE
                },
            };

            if (compiled.tsResult.IsCreated)
            {
                desc.AS = new D3D12_SHADER_BYTECODE(compiled.tsResult.bytecode.GetUnsafePtr(), (nuint)compiled.tsResult.bytecode.Count);
            }

            for (var i = 0; i < descriptor.RtvFormats.Length; i++)
            {
                desc.RTVFormats[i] = descriptor.RtvFormats[i].ToDXGIFormat();
                desc.BlendState.RenderTarget[i].RenderTargetWriteMask = (byte)((int)descriptor.PipelineOption.ColorMask & 0x0F);
            }

            var meshStream = new CD3DX12_PIPELINE_MESH_STATE_STREAM(in desc);
            var streamDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
            {
                pPipelineStateSubobjectStream = &meshStream,
                SizeInBytes = (nuint)sizeof(CD3DX12_PIPELINE_MESH_STATE_STREAM)
            };

            ID3D12PipelineState* pPipelineState = default;

            var pKeyStr = stackalloc char[33]; // 32 for 128 bits key + 1 for null terminator
            var keySpan = new Span<char>(pKeyStr, 33);
            if (!pipelineKey.TryGetString(keySpan))
            {
                return Result.Failure("Failed to convert pipeline key to string.");
            }

            var hr = _library.Get()->LoadPipeline(pKeyStr, &streamDesc, __uuidof(pPipelineState), (void**)&pPipelineState);
            if (hr == E.E_INVALIDARG)
            {
                // Pipeline not found in the library, create a new one.
                ThrowIfFailed(_device.NativeDevice.Get()->CreatePipelineState(&streamDesc, __uuidof(pPipelineState), (void**)&pPipelineState));
                ThrowIfFailed(_library.Get()->StorePipeline(pKeyStr, pPipelineState));
            }
            else
            {
                ThrowIfFailed(hr);
            }

            D3D12PipelineState pso = default;
            pso.shaderVariant = descriptor.VariantKey;
            pso.psoDesc = desc;
            pso.pso.Attach(pPipelineState);

            _pipelineCache[pipelineKey] = pso;
        }

        return pipelineKey;
    }

    public bool HasPipeline(Key128<GraphicsPipeline> key)
    {
        return _pipelineCache.ContainsKey(key);
    }

    public Result<SharedPtr<ID3D12PipelineState>, ErrorStatus> GetGraphicsPSO(Key128<GraphicsPipeline> key)
    {
        if (_pipelineCache.TryGetValue(key, out var cacheEntry))
        {
            return cacheEntry.pso.Share();
        }

        return ErrorStatus.NotFound;
    }

    public void Dispose()
    {
        foreach (var kvp in _pipelineCache)
        {
            kvp.Value.Dispose();
        }

        _pipelineCache.Clear();

        _defaultRootSignature.Dispose();
        _library.Dispose();
    }
}
