#undef USE_TRADITIONAL_BINDLESS

using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using Misaki.HighPerformance.LowLevel.Utilities;

using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

// TODO: Fixed root signature and use bindless samplers and textures.
// This can dramatically reduce the number of root parameters needed and improve performance.
internal class D3D12ShaderPipeline : IShaderPipeline, IDisposable
{
    public ComPtr<ID3D12PipelineState> pipelineState;
    public D3D12ShaderCompiler.CompileResult vsResult;
    public D3D12ShaderCompiler.CompileResult psResult;
    public D3D12ShaderCompiler.CompileResult csResult;

    public PipelineType Type
    {
        get; init;
    }

    public void Dispose()
    {
        pipelineState.Dispose();
        vsResult.Dispose();
        psResult.Dispose();
        csResult.Dispose();
    }
}

internal unsafe class D3D12PipelineLibrary : IPipelineLibrary, IDisposable
{
    private readonly D3D12RenderDevice _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private ComPtr<ID3D12PipelineLibrary1> _library;
    private ComPtr<ID3D12RootSignature> _defaultRootSignature;

    private readonly Dictionary<Identifier<Shader>, D3D12ShaderPipeline> _shaderPipelines;

    public ID3D12RootSignature* DefaultRootSignature => _defaultRootSignature.Get();

    public D3D12PipelineLibrary(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase, string? cachePath)
    {
        _device = device;
        _resourceDatabase = resourceDatabase;

        _shaderPipelines = new();

        InitializePipelineLibrary(cachePath);
        CreateDefaultRootSignature();
    }

    private void InitializePipelineLibrary(string? cachePath)
    {
        if (!File.Exists(cachePath))
        {
            _device.NativeDevice->CreatePipelineLibrary(null, 0, __uuidof<ID3D12PipelineLibrary1>(), _library.GetVoidAddressOf()).ThrowIfFailed();
        }

        var fileBytes = File.ReadAllBytes(cachePath!);
        fixed (byte* pFileBytes = fileBytes)
        {
            _device.NativeDevice->CreatePipelineLibrary(pFileBytes, (nuint)fileBytes.Length, __uuidof<ID3D12PipelineLibrary1>(), _library.GetVoidAddressOf()).ThrowIfFailed();
        }
    }

    private void CreateDefaultRootSignature()
    {
        const int rootParamCount =
#if USE_TRADITIONAL_BINDLESS
            6
#else
            4
#endif
            ;

        _defaultRootSignature = default;

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up descriptor tables.
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER1[rootParamCount];
        rootParameters[0] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(0, 0), // b0
        };

        rootParameters[1] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(1, 0), // b1
        };

        rootParameters[2] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(2, 0), // b2
        };

        rootParameters[3] = new D3D12_ROOT_PARAMETER1
        {
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            Descriptor = new D3D12_ROOT_DESCRIPTOR1(3, 0), // b3
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
            NumParameters = rootParamCount,
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
            var errorMsg = error.Get() != null ? Marshal.PtrToStringAnsi((nint)error.Get()->GetBufferPointer()) : "Unknown error";
            throw new InvalidOperationException($"Failed to serialize default root signature: {errorMsg}");
        }

        _device.NativeDevice->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(),
            __uuidof<ID3D12RootSignature>(), _defaultRootSignature.GetVoidAddressOf()).ThrowIfFailed();
    }

    public void StorePipeline(string psoIdentifier, ID3D12PipelineState* pso)
    {
        _library.Get()->StorePipeline(psoIdentifier.AsSpan().GetUnsafePtr(), pso);
    }

    public void* LoadGraphicsPipeline(string psoIdentifier)
    {
        if (_library.Get()->LoadGraphicsPipeline(psoIdentifier.AsSpan().GetUnsafePtr(), __uuidof<ID3D12PipelineState>(), out var pso).Failure)
        {
            return null;
        }

        return pso;
    }

    public void CompileShader(Identifier<Shader> id, string shaderPath)
    {
        var vsResult = D3D12ShaderCompiler.Compile(shaderPath, D3D12ShaderCompiler.ShaderStage.VertexShader, D3D12ShaderCompiler.CompilerVersion.SM_6_6);
        var psResult = D3D12ShaderCompiler.Compile(shaderPath, D3D12ShaderCompiler.ShaderStage.PixelShader, D3D12ShaderCompiler.CompilerVersion.SM_6_6);

        ref var shader = ref _resourceDatabase.GetShaderReference(id);

        D3D12ShaderCompiler.PerformDXCReflection(ref shader, vsResult.reflection.Get());
        D3D12ShaderCompiler.PerformDXCReflection(ref shader, psResult.reflection.Get());

        var shaderPipeline = new D3D12ShaderPipeline
        {
            Type = PipelineType.Graphics,
            vsResult = vsResult,
            psResult = psResult
        };

        _shaderPipelines[id] = shaderPipeline;
    }

    // Create PSO from SDL (Shader Definition Language) file
    private void CreatePipelineStateObject(D3D12ShaderPipeline shaderPipeline)
    {
        var psoDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC
        {
            pRootSignature = _defaultRootSignature.Get(),
            VS = new D3D12_SHADER_BYTECODE(shaderPipeline.vsResult.bytecode.GetUnsafePtr(), (nuint)shaderPipeline.vsResult.bytecode.Count),
            PS = new D3D12_SHADER_BYTECODE(shaderPipeline.psResult.bytecode.GetUnsafePtr(), (nuint)shaderPipeline.psResult.bytecode.Count),
            InputLayout = D3D12PipelineResource.InputLayoutDescription,
            RasterizerState = D3D12_RASTERIZER_DESC.CULL_NONE,
            BlendState = D3D12_BLEND_DESC.OPAQUE,
            DepthStencilState = D3D12_DEPTH_STENCIL_DESC.DEFAULT,
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
            NumRenderTargets = 1,
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            DSVFormat = DXGI_FORMAT_UNKNOWN,
        };

        psoDesc.RTVFormats[0] = D3D12PipelineResource.SWAP_CHAIN_BACK_BUFFER_FORMAT;

        _device.NativeDevice->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), shaderPipeline.pipelineState.GetVoidAddressOf());
    }

    // TODO: Pipeline variants (keywords)
    // TODO: Disk caching
    // TODO: Async compilation
    public void PreCookPipelineState()
    {
        foreach (var kvp in _shaderPipelines)
        {
            ref var shader = ref _resourceDatabase.GetShaderReference(kvp.Key);

            CreatePipelineStateObject(kvp.Value);

            kvp.Value.vsResult.Dispose();
            kvp.Value.psResult.Dispose();
            kvp.Value.csResult.Dispose();
        }
    }

    public IShaderPipeline GetShaderPipeline(Identifier<Shader> id)
    {
        if (_shaderPipelines.TryGetValue(id, out var pipeline))
        {
            return pipeline;
        }

        throw new KeyNotFoundException($"Shader pipeline not found for shader ID: {id}");
    }

    public void Dispose()
    {
        foreach (var kvp in _shaderPipelines)
        {
            kvp.Value.Dispose();
        }
    }
}