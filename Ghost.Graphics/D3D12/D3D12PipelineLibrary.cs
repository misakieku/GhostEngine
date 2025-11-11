using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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

    public void Dispose()
    {
        tsResult.Dispose();
        msResult.Dispose();
        psResult.Dispose();
    }
}

[SupportedOSPlatform(Win32Utility.OS_SUPPORTED_VERSION)]
internal struct D3D12PipelineState : IDisposable
{
    // NOTE: This is just a temporary cache for compiled shader code. We will implement a proper disk cache later.
    public D3D12GraphicsCompiledResult compileResult;
    public D3DX12_MESH_SHADER_PIPELINE_STATE_DESC psoDesc;
    public ComPtr<ID3D12PipelineState> pso;

    public void Dispose()
    {
        compileResult.Dispose();
        pso.Dispose();
    }
}

[SupportedOSPlatform(Win32Utility.OS_SUPPORTED_VERSION)]
internal unsafe class D3D12PipelineLibrary : IPipelineLibrary, IDisposable
{
    private const int _ROOT_PARAM_COUNT =
#if USE_TRADITIONAL_BINDLESS
    6
#else
    4
#endif
    ;

    private readonly D3D12RenderDevice _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private ComPtr<ID3D12PipelineLibrary1> _library;
    private ComPtr<ID3D12RootSignature> _defaultRootSignature;

    private readonly Dictionary<GraphicsPipelineKey, D3D12PipelineState> _pipelineCache;

    public ID3D12RootSignature* DefaultRootSignature => _defaultRootSignature.Get();

    public D3D12PipelineLibrary(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase)
    {
        _device = device;
        _resourceDatabase = resourceDatabase;

        _pipelineCache = new();

        CreateDefaultRootSignature();
    }

    private void CreateDefaultRootSignature()
    {
        _defaultRootSignature = default;

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up viewGroup tables.
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER1[_ROOT_PARAM_COUNT];
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
            NumParameters = _ROOT_PARAM_COUNT,
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

    public void LoadLibraryFromDisk(string? filePath)
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

    private static void ValidateReflectionData(ShaderReflectionData reflectionData)
    {
        if (reflectionData.ConstantBuffers.Count != _ROOT_PARAM_COUNT)
        {
            throw new InvalidOperationException($"Shader reflection data has {reflectionData.ConstantBuffers.Count} constant buffers, expected {_ROOT_PARAM_COUNT}");
        }

        if (reflectionData.OtherResources.Count != 0)
        {
            throw new NotSupportedException("Shader reflection data contains unsupported resource types. Only constant buffers are supported in the current root signature.");
        }

        // TODO: Validate Cbuffer sizes and bindings.
    }

    private static Result<D3D12GraphicsCompiledResult> CompileAndValidateFullPass(FullPassDescriptor descriptor)
    {
        static CompileResult CompileAndValidate(ref CompilerConfig config)
        {
            IDxcBlob* reflectionBlob = default;

            try
            {
                var result = D3D12ShaderCompiler.Compile(ref config, Allocator.Persistent, &reflectionBlob).GetValueOrThrow();

                if (reflectionBlob != null)
                {
                    var reflection = D3D12ShaderCompiler.PerformDXCReflection(reflectionBlob).GetValueOrThrow();
                    ValidateReflectionData(reflection);
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
                includes = descriptor.includes.AsSpan(),
                shaderPath = tsEntry.shader,
                entryPoint = tsEntry.entry,
                stage = ShaderStage.TaskShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            tsResult = CompileAndValidate(ref config);
        }

        CompileResult msResult;
        var msEntry = descriptor.meshShader;
        if (msEntry.IsCreated)
        {
            var config = new CompilerConfig
            {
                defines = descriptor.defines.AsSpan(),
                includes = descriptor.includes.AsSpan(),
                shaderPath = msEntry.shader,
                entryPoint = msEntry.entry,
                stage = ShaderStage.MeshShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            msResult = CompileAndValidate(ref config);
        }
        else
        {
            return Result<D3D12GraphicsCompiledResult>.Fail("Mesh shader expected.");
        }

        CompileResult psResult;
        var psEntry = descriptor.pixelShader;
        if (psEntry.IsCreated)
        {
            var config = new CompilerConfig
            {
                defines = descriptor.defines.AsSpan(),
                includes = descriptor.includes.AsSpan(),
                shaderPath = psEntry.shader,
                entryPoint = psEntry.entry,
                stage = ShaderStage.PixelShader,
                tier = CompilerTier.Tier0,
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
            };

            psResult = CompileAndValidate(ref config);
        }
        else
        {
            return Result<D3D12GraphicsCompiledResult>.Fail("Pixel shader expected.");
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

    private GraphicsPipelineKey CompilePSO(ref readonly GraphicsPSODescriptor descriptor, ref readonly D3D12GraphicsCompiledResult compiled)
    {
        var rtvCount = (uint)Math.Min(descriptor.rtvFormats.Length, D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT);

        var desc = new D3DX12_MESH_SHADER_PIPELINE_STATE_DESC
        {
            pRootSignature = _defaultRootSignature.Get(),
            MS = new D3D12_SHADER_BYTECODE(compiled.msResult.bytecode.GetUnsafePtr(), (nuint)compiled.msResult.bytecode.Count),
            PS = new D3D12_SHADER_BYTECODE(compiled.psResult.bytecode.GetUnsafePtr(), (nuint)compiled.psResult.bytecode.Count),
            PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
            SampleMask = UINT32_MAX,
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            NumRenderTargets = rtvCount,
            DSVFormat = descriptor.dsvFormat.ToDXGIFormat(),
            DepthStencilState = BuildDepthStencil(descriptor.zTest, descriptor.zWrite),
            NodeMask = 0,
            Flags = D3D12_PIPELINE_STATE_FLAG_NONE,

            BlendState = descriptor.blend switch
            {
                BlendOptions.Opaque => D3D12Utility.D3D12_BLEND_DESC_OPAQUE,
                BlendOptions.Alpha => D3D12Utility.D3D12_BLEND_DESC_ALPHA_BLEND,
                BlendOptions.Additive => D3D12Utility.D3D12_BLEND_DESC_ADDITIVE,
                BlendOptions.Multiply => D3D12Utility.D3D12_BLEND_DESC_MULTIPLY,
                BlendOptions.PremultipliedAlpha => D3D12Utility.D3D12_BLEND_DESC_PREMULTIPLIED,
                _ => D3D12Utility.D3D12_BLEND_DESC_OPAQUE
            },
            RasterizerState = descriptor.cull switch
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
            id = descriptor.passId,
            rtvCount = (uint)descriptor.rtvFormats.Length,
            dsvFormat = descriptor.dsvFormat,
        };

        for (var i = 0; i < rtvCount && i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
        {
            desc.RTVFormats[i] = descriptor.rtvFormats[i].ToDXGIFormat();
            desc.BlendState.RenderTarget[i].RenderTargetWriteMask = (byte)(descriptor.colorMask & 0x0F);
            hash.rtvFormats[i] = descriptor.rtvFormats[i];
        }

        var key = hash.GetKey();
        ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_pipelineCache, key, out var exists);
        if (!exists)
        {
            existing.compileResult = compiled;
            existing.psoDesc = desc;

            var streamDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
            {
                pPipelineStateSubobjectStream = &desc,
                SizeInBytes = (nuint)sizeof(D3DX12_MESH_SHADER_PIPELINE_STATE_DESC)
            };

            ID3D12PipelineState* pPipelineState = default;

            char* pKeyStr = stackalloc char[GraphicsPipelineKey.KEY_STRING_LENGTH];
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
        var key = default(GraphicsPipelineKey);
        switch (descriptor)
        {
            case FullPassDescriptor fullPass:
                var result = CompileAndValidateFullPass(fullPass).GetValueOrThrow();
                var psoDes = new GraphicsPSODescriptor
                {
                    passId = new(fullPass.Identifier),
                    zTest = fullPass.localPipeline.zTest,
                    zWrite = fullPass.localPipeline.zWrite,
                    cull = fullPass.localPipeline.cull,
                    blend = fullPass.localPipeline.blend,
                    colorMask = fullPass.localPipeline.colorMask,

                    rtvFormats = rtvs,
                    dsvFormat = dsv,
                };

                key = CompilePSO(in psoDes, in result);
                break;

            // Do we need to support other pass types?

            default:
                break;
        }

        return key;
    }

    public Result<Ptr<ID3D12PipelineState>> LoadGraphicsPSO(GraphicsPipelineKey key)
    {
        ref var cacheEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_pipelineCache, key);
        if (Unsafe.IsNullRef(ref cacheEntry))
        {
            return Result.Fail("Pipeline state not found in cache.");
        }

        return new Ptr<ID3D12PipelineState>(cacheEntry.pso.Get());
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
