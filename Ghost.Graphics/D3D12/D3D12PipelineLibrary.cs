using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Utilities;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

internal struct D3D12GraphicsCompiledResult : IDisposable
{
    public CompileResult tsResult;
    public CompileResult msResult;
    public CompileResult psResult;
    public CBufferInfo cbufferInfo;

    public void Dispose()
    {
        tsResult.Dispose();
        msResult.Dispose();
        psResult.Dispose();
    }
}

internal struct D3D12PipelineState : IDisposable
{
    public D3DX12_MESH_SHADER_PIPELINE_STATE_DESC psoDesc;
    public ComPtr<ID3D12PipelineState> pso;
    public ShaderPassKey shaderPass;

    public void Dispose()
    {
        pso.Dispose();
    }
}

internal unsafe class D3D12PipelineLibrary : IPipelineLibrary, IDisposable
{
    private readonly D3D12RenderDevice _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private ComPtr<ID3D12PipelineLibrary1> _library;
    private ComPtr<ID3D12RootSignature> _defaultRootSignature;

    private readonly Dictionary<GraphicsPipelineKey, D3D12PipelineState> _pipelineCache;
    // NOTE: This is just a temporary cache for compiled shader code. We will implement a proper disk cache later.
    private readonly Dictionary<ShaderPassKey, D3D12GraphicsCompiledResult> _compiledResults;

    public ID3D12RootSignature* DefaultRootSignature => _defaultRootSignature.Get();

    public D3D12PipelineLibrary(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase)
    {
        _device = device;
        _resourceDatabase = resourceDatabase;

        _pipelineCache = new();
        _compiledResults = new();

        CreateDefaultRootSignature();
    }

    private void CreateDefaultRootSignature()
    {
        _defaultRootSignature = default;

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up viewGroup tables.
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER1[RootSignatureLayout.ROOT_PARAMETER_COUNT];
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

#if USE_TRADITIONAL_BINDLESS
        // Descriptor table for bindless textures
        var srvRange = new D3D12_DESCRIPTOR_RANGE1(
            D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
            ~0u,
            0,
            0,
            D3D12_DESCRIPTOR_RANGE_FLAGS_DATA_VOLATILE);

        rootParameters[4] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE1(1, &srvRange)
        };

        // Descriptor table for bindless samplers
        var sampRange = new D3D12_DESCRIPTOR_RANGE1(
            D3D12_DESCRIPTOR_RANGE_TYPE_SAMPLER,
            ~0u,
            0,
            0,
            D3D12_DESCRIPTOR_RANGE_FLAGS_DATA_VOLATILE);

        rootParameters[5] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE1(1, &sampRange)
        };
#endif

        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC1
        {
            NumParameters = RootSignatureLayout.ROOT_PARAMETER_COUNT,
            pParameters = rootParameters,
            NumStaticSamplers = 0,
            pStaticSamplers = null,
            Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT
#if !USE_TRADITIONAL_BINDLESS
                | D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
                | D3D12_ROOT_SIGNATURE_FLAG_SAMPLER_HEAP_DIRECTLY_INDEXED
#endif
        };

        var versionedDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC
        {
            Version = D3D_ROOT_SIGNATURE_VERSION_1_1,
            Desc_1_1 = rootSignatureDesc
        };

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        var serializeResult = D3D12SerializeVersionedRootSignature(&versionedDesc, signature.GetAddressOf(), error.GetAddressOf());
        if (serializeResult.FAILED)
        {
            var errorMsg = error.Get() != null ? Marshal.PtrToStringUTF8((nint)error.Get()->GetBufferPointer()) : "Unknown error";
            throw new InvalidOperationException($"Failed to serialize default root signature: {errorMsg}");
        }

        ID3D12RootSignature* pRootSignature = default;
        ThrowIfFailed(_device.NativeDevice->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(),
            __uuidof(pRootSignature), (void**)&pRootSignature));

        _defaultRootSignature.Attach(pRootSignature);
    }

    public void InitializeLibrary(string? filePath)
    {
        ID3D12PipelineLibrary1* pLibrary = default;

        if (File.Exists(filePath))
        {
            var fileBytes = File.ReadAllBytes(filePath!);
            fixed (byte* pFileBytes = fileBytes)
            {
                ThrowIfFailed(_device.NativeDevice->CreatePipelineLibrary(pFileBytes, (nuint)fileBytes.Length, __uuidof(pLibrary), (void**)&pLibrary));
            }
        }
        else
        {
            ThrowIfFailed(_device.NativeDevice->CreatePipelineLibrary(null, 0, __uuidof(pLibrary), (void**)&pLibrary));
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
        using var buffer = new UnsafeArray<byte>((int)size, Allocator.Persistent); // We use persistent heap allocation instead of stack allocation to avoid stack overflow for large pipeline libraries.

        ThrowIfFailed(_library.Get()->Serialize(buffer.GetUnsafePtr(), size));

        using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(buffer.AsSpan());
    }

    private static Result<CBufferInfo> ValidateReflectionData(FullPassDescriptor descriptor, ShaderReflectionData reflectionData)
    {
        CBufferInfo cbufferInfo = default;

        foreach (var info in reflectionData.ResourcesBindings)
        {
            if (info.BindPoint > 3)
            {
                return Result.Fail($"Resource binding point {info.BindPoint} is out of range. Only binding points 0-3 are supported in the current root signature.");
            }

            if (info.Type != D3D_SHADER_INPUT_TYPE.D3D_SIT_CBUFFER)
            {
                return Result.Fail($"Resource binding type {info.Type} is not supported. Only constant buffers are supported in the current root signature.");
            }

            if (info.BindPoint == RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT)
            {
                cbufferInfo = new CBufferInfo
                {
                    Name = info.Name,
                    RegisterSlot = info.BindPoint,
                    RegisterSpace = info.Space,
                    SizeInBytes = info.Size,
                    Properties = info.Properties ?? Array.Empty<CBufferPropertyInfo>(),
                };

                return Result.Success(cbufferInfo);
            }
        }

        return Result.Fail("Per-material constant buffer not found in shader reflection data.");

        // TODO: Validate Cbuffer sizes and bindings.
    }

    private static D3D12GraphicsCompiledResult CompileAndValidateFullPass(FullPassDescriptor descriptor)
    {
        static CompileResult CompileAndValidate(ref CompilerConfig config, FullPassDescriptor descriptor)
        {
            IDxcBlob* reflectionBlob = default;
            CBufferInfo cbufferInfo = default;

            try
            {
                // TODO: This does not include generated code. This will cause a root signature mismatch.
                var result = D3D12ShaderCompiler.Compile(ref config, Allocator.Persistent, (void**)&reflectionBlob).GetValueOrThrow();
                if (reflectionBlob != null)
                {
                    var reflection = D3D12ShaderCompiler.PerformDXCReflection(reflectionBlob).GetValueOrThrow();
                    cbufferInfo = ValidateReflectionData(descriptor, reflection).GetValueOrThrow();
                }

                return result;
            }
            finally
            {
                if (reflectionBlob != null)
                {
                    reflectionBlob->Release();
                }
            }
        }

        CompileResult tsResult = default;
        var tsEntry = descriptor.taskShader;
        if (tsEntry.IsCreated)
        {
            var config = new CompilerConfig
            {
                defines = descriptor.defines.AsSpan(),
                include = descriptor.generatedCodePath,
                shaderPath = tsEntry.shader,
                entryPoint = tsEntry.entry,
                stage = ShaderStage.TaskShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            tsResult = CompileAndValidate(ref config, descriptor);
        }

        CompileResult msResult;
        var msEntry = descriptor.meshShader;
        if (msEntry.IsCreated)
        {
            var config = new CompilerConfig
            {
                defines = descriptor.defines.AsSpan(),
                include = descriptor.generatedCodePath,
                shaderPath = msEntry.shader,
                entryPoint = msEntry.entry,
                stage = ShaderStage.MeshShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            msResult = CompileAndValidate(ref config, descriptor);
        }
        else
        {
            throw new InvalidOperationException("Mesh shader expected.");
        }

        CompileResult psResult;
        var psEntry = descriptor.pixelShader;
        if (psEntry.IsCreated)
        {
            var config = new CompilerConfig
            {
                defines = descriptor.defines.AsSpan(),
                include = descriptor.generatedCodePath,
                shaderPath = psEntry.shader,
                entryPoint = psEntry.entry,
                stage = ShaderStage.PixelShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            psResult = CompileAndValidate(ref config, descriptor);
        }
        else
        {
            throw new InvalidOperationException("Pixel shader expected.");
        }

        return new D3D12GraphicsCompiledResult
        {
            tsResult = tsResult,
            msResult = msResult,
            psResult = psResult
        };
    }

    private static D3D12_COMPARISON_FUNC ToD3DCompare(ZTestOptions z) => z switch
    {
        ZTestOptions.Disabled => D3D12_COMPARISON_FUNC_ALWAYS,
        ZTestOptions.Less => D3D12_COMPARISON_FUNC_LESS,
        ZTestOptions.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
        ZTestOptions.Equal => D3D12_COMPARISON_FUNC_EQUAL,
        ZTestOptions.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
        ZTestOptions.Greater => D3D12_COMPARISON_FUNC_GREATER,
        ZTestOptions.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
        ZTestOptions.Always => D3D12_COMPARISON_FUNC_ALWAYS,
        _ => D3D12_COMPARISON_FUNC_LESS_EQUAL
    };

    private static D3D12_DEPTH_STENCIL_DESC BuildDepthStencil(ZTestOptions ztest, ZWriteOptions zwrite)
    {
        var depthEnabled = ztest != ZTestOptions.Disabled;
        var writeEnabled = zwrite == ZWriteOptions.On;
        var cmp = ToD3DCompare(ztest);
        return D3D12Utility.D3D12_DEPTH_STENCIL_DESC_CREATE(depthEnabled, writeEnabled, cmp);
    }

    private bool TryGetCompiledCache(ShaderPassKey passKey, out D3D12GraphicsCompiledResult compiled)
    {
        return _compiledResults.TryGetValue(passKey, out compiled);
    }

    private GraphicsPipelineKey CompilePSO(ref readonly GraphicsPSODescriptor descriptor, ref readonly D3D12GraphicsCompiledResult compiled)
    {
        var rtvCount = (uint)Math.Min(descriptor.RtvFormats.Length, D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT);

        var desc = new D3DX12_MESH_SHADER_PIPELINE_STATE_DESC
        {
            pRootSignature = _defaultRootSignature.Get(),
            MS = new D3D12_SHADER_BYTECODE(compiled.msResult.bytecode.GetUnsafePtr(), (nuint)compiled.msResult.bytecode.Count),
            PS = new D3D12_SHADER_BYTECODE(compiled.psResult.bytecode.GetUnsafePtr(), (nuint)compiled.psResult.bytecode.Count),
            PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
            SampleMask = UINT32_MAX,
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            NumRenderTargets = rtvCount,
            DSVFormat = descriptor.DsvFormat.ToDXGIFormat(),
            DepthStencilState = BuildDepthStencil(descriptor.ZTest, descriptor.ZWrite),
            NodeMask = 0,
            Flags = D3D12_PIPELINE_STATE_FLAG_NONE,

            BlendState = descriptor.Blend switch
            {
                BlendOptions.Opaque => D3D12Utility.D3D12_BLEND_DESC_OPAQUE,
                BlendOptions.Alpha => D3D12Utility.D3D12_BLEND_DESC_ALPHA_BLEND,
                BlendOptions.Additive => D3D12Utility.D3D12_BLEND_DESC_ADDITIVE,
                BlendOptions.Multiply => D3D12Utility.D3D12_BLEND_DESC_MULTIPLY,
                BlendOptions.PremultipliedAlpha => D3D12Utility.D3D12_BLEND_DESC_PREMULTIPLIED,
                _ => D3D12Utility.D3D12_BLEND_DESC_OPAQUE
            },
            RasterizerState = descriptor.Cull switch
            {
                CullOptions.Off => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE,
                CullOptions.Front => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_CLOCKWISE,
                CullOptions.Back => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_COUNTER_CLOCKWISE,
                _ => D3D12Utility.D3D12_RASTERIZER_DESC_CULL_NONE
            },
        };

        if (compiled.tsResult.IsCreated)
        {
            desc.AS = new D3D12_SHADER_BYTECODE(compiled.tsResult.bytecode.GetUnsafePtr(), (nuint)compiled.tsResult.bytecode.Count);
        }

        var hash = new GraphicsPipelineHash
        {
            Id = descriptor.PassId,
            RtvCount = (uint)descriptor.RtvFormats.Length,
            DsvFormat = descriptor.DsvFormat,
        };

        for (var i = 0; i < rtvCount && i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
        {
            desc.RTVFormats[i] = descriptor.RtvFormats[i].ToDXGIFormat();
            desc.BlendState.RenderTarget[i].RenderTargetWriteMask = (byte)(descriptor.ColorMask & 0x0F);
            hash.RtvFormats[i] = descriptor.RtvFormats[i];
        }

        var key = hash.GetKey();
        ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_pipelineCache, key, out var exists);
        if (!exists)
        {
            existing.psoDesc = desc;

            var meshStream = new CD3DX12_PIPELINE_MESH_STATE_STREAM(in desc);
            var streamDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
            {
                pPipelineStateSubobjectStream = &meshStream,
                SizeInBytes = (nuint)sizeof(CD3DX12_PIPELINE_MESH_STATE_STREAM)
            };

            ID3D12PipelineState* pPipelineState = default;

            var pKeyStr = stackalloc char[GraphicsPipelineKey.KEY_STRING_LENGTH];
            var keySpan = new Span<char>(pKeyStr, GraphicsPipelineKey.KEY_STRING_LENGTH);
            key.GetString(keySpan).ThrowIfFailed();

            var hr = _library.Get()->LoadPipeline(pKeyStr, &streamDesc, __uuidof(pPipelineState), (void**)&pPipelineState);
            if (hr == E.E_INVALIDARG)
            {
                // Pipeline not found in the library, create a new one.
                ThrowIfFailed(_device.NativeDevice->CreatePipelineState(&streamDesc, __uuidof(pPipelineState), (void**)&pPipelineState));
                ThrowIfFailed(_library.Get()->StorePipeline(pKeyStr, pPipelineState));
            }
            else
            {
                ThrowIfFailed(hr);
            }

            existing.pso.Attach(pPipelineState);
        }

        return key;
    }

    public GraphicsPipelineKey CompilePassPSO(IPassDescriptor descriptor, ReadOnlySpan<TextureFormat> rtvs, TextureFormat dsv)
    {
        GraphicsPipelineKey key = default;

        var passKey = new ShaderPassKey(descriptor.Identifier);
        var hasCompiledCache = TryGetCompiledCache(passKey, out var compiled);

        switch (descriptor)
        {
            case FullPassDescriptor fullPass:
                if (!hasCompiledCache)
                {
                    compiled = CompileAndValidateFullPass(fullPass);
                }

                var psoDes = new GraphicsPSODescriptor
                {
                    PassId = new ShaderPassKey(fullPass.Identifier),
                    ZTest = fullPass.localPipeline.zTest,
                    ZWrite = fullPass.localPipeline.zWrite,
                    Cull = fullPass.localPipeline.cull,
                    Blend = fullPass.localPipeline.blend,
                    ColorMask = fullPass.localPipeline.colorMask,

                    RtvFormats = rtvs,
                    DsvFormat = dsv,
                };

                key = CompilePSO(in psoDes, in compiled);
                break;

            // Do we need to support other pass types?
            case FallbackPassDescriptor:
                if (!hasCompiledCache)
                {
                    throw new ArgumentException("FallbackPassDescriptor is not supported for PSO compilation. There may be some inheritance dependency issues.");
                }

                break;

            default:
                break;
        }

        return key;
    }

    public Result<Ptr<ID3D12PipelineState>> GetGraphicsPSO(GraphicsPipelineKey key)
    {
        if (_pipelineCache.TryGetValue(key, out var cacheEntry))
        {
            return new Ptr<ID3D12PipelineState>(cacheEntry.pso.Get());
        }

        return Result.Fail("Pipeline state not found in cache.");
    }

    public Result<CBufferInfo> GetCBufferInfo(ShaderPassKey key)
    {
        if (_compiledResults.TryGetValue(key, out var compiled))
        {
            return compiled.cbufferInfo;
        }

        return Result.Fail("Compiled shader not found in cache.");
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
