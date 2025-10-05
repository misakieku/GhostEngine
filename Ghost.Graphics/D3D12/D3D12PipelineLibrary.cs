#undef USE_TRANDITIONAL_BINDLESS

using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct D3D12GraphicsPipelineStream
{
    public PipelineStateSubObjectType rootSignatureType;
    public ID3D12RootSignature* pRootSignature;

    public PipelineStateSubObjectType vsType;
    public ShaderBytecode vs;

    public PipelineStateSubObjectType psType;
    public ShaderBytecode ps;

    public PipelineStateSubObjectType rasterizerType;
    public RasterizerDescription rasterizer;

    public PipelineStateSubObjectType blendType;
    public BlendDescription blend;

    public PipelineStateSubObjectType depthStencilType;
    public DepthStencilDescription depthStencil;

    public PipelineStateSubObjectType topologyType;
    public PrimitiveTopologyType primitiveTopology;

    public PipelineStateSubObjectType rtvFormatType;
    public RtFormatArray rtvFormats;

    public PipelineStateSubObjectType dsvFormatType;
    public Format dsvFormat;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct ComputePipelineStream
{
    public PipelineStateSubObjectType rootSignatureType;
    public ID3D12RootSignature* pRootSignature;

    public PipelineStateSubObjectType csType;
    public ShaderBytecode cs;
}

internal unsafe class D3D12PipelineLibrary : IPipelineLibrary, IDisposable
{
    private readonly D3D12RenderDevice _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private ComPtr<ID3D12PipelineLibrary1> _library;
    private ComPtr<ID3D12RootSignature> _defaultRootSignature;

    private readonly Dictionary<Identifier<Shader>, D3D12ShaderPipeline> _shaderPipelines;

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
#if USE_TRANDITIONAL_BINDLESS
            6
#else
            4
#endif
            ;

        _defaultRootSignature = default;

        // The layout of the root signature is:
        // - Global buffer (b0)
        // - Per-view buffer (b1)
        // - Per-object buffer (b2)
        // - Per-instance buffer (b3)
        // - Descriptor table for bindless textures (t0)
        // - Descriptor table for bindless samplers (s0)

        // NOTE: Since we are targeting SM 6.6, we can use ResourceDescriptorHeap and SamplerDescriptorHeap directly without needing to set up descriptor tables.

        var rootParameters = stackalloc RootParameter1[rootParamCount];
        rootParameters[0] = new RootParameter1
        {
            ParameterType = RootParameterType.Cbv,
            ShaderVisibility = ShaderVisibility.All,
            Descriptor = new RootDescriptor1(0, 0), // b0
        };

        rootParameters[1] = new RootParameter1
        {
            ParameterType = RootParameterType.Cbv,
            ShaderVisibility = ShaderVisibility.All,
            Descriptor = new RootDescriptor1(1, 0), // b1
        };

        rootParameters[2] = new RootParameter1
        {
            ParameterType = RootParameterType.Cbv,
            ShaderVisibility = ShaderVisibility.All,
            Descriptor = new RootDescriptor1(2, 0), // b2
        };

        rootParameters[3] = new RootParameter1
        {
            ParameterType = RootParameterType.Cbv,
            ShaderVisibility = ShaderVisibility.All,
            Descriptor = new RootDescriptor1(3, 0), // b3
        };

#if USE_TRANDITIONAL_BINDLESS
        // Descriptor table for bindless textures
        var srvRange = new DescriptorRange1(
            DescriptorRangeType.Srv,
            ~0u,
            0,
            0,
            DescriptorRangeFlags.DataVolatile);

        rootParameters[4] = new RootParameter1
        {
            ParameterType = RootParameterType.DescriptorTable,
            ShaderVisibility = ShaderVisibility.All,
            DescriptorTable = new RootDescriptorTable1(1, &srvRange)
        };

        // Descriptor table for bindless samplers
        var sampRange = new DescriptorRange1(
            DescriptorRangeType.Sampler,
            ~0u, 
            0,
            0,
            DescriptorRangeFlags.DataVolatile);

        rootParameters[5] = new RootParameter1
        {
            ParameterType = RootParameterType.DescriptorTable,
            ShaderVisibility = ShaderVisibility.All,
            DescriptorTable = new RootDescriptorTable1(1, &sampRange)
        };
#endif

        var rootSignatureDesc = new RootSignatureDescription1
        {
            NumParameters = rootParamCount,
            pParameters = rootParameters,
            NumStaticSamplers = 0,
            pStaticSamplers = null,
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
#if !USE_TRANDITIONAL_BINDLESS
                | RootSignatureFlags.CbvSrvUavHeapDirectlyIndexed
                | RootSignatureFlags.SamplerHeapDirectlyIndexed
#endif
        };

        var versionedDesc = new VersionedRootSignatureDescription
        {
            Version = RootSignatureVersion.V1_1,
            Desc_1_1 = rootSignatureDesc
        };

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        var serializeResult = D3D12SerializeVersionedRootSignature(&versionedDesc, signature.GetAddressOf(), error.GetAddressOf());
        if (serializeResult.Failure)
        {
            var errorMsg = error.Get() != null ? Marshal.PtrToStringAnsi((nint)error.Get()->GetBufferPointer()) : "Unknown error";
            throw new InvalidOperationException($"Failed to serialize default root signature: {errorMsg}");
        }

        _device.NativeDevice->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(),
            __uuidof<ID3D12RootSignature>(), _defaultRootSignature.GetVoidAddressOf()).ThrowIfFailed();
    }

    public void StorePipeline(string psoIdentifier, ID3D12PipelineState* pso)
    {
        _library.Get()->StorePipeline(psoIdentifier.AsSpan().GetPointer(), pso);
    }

    public void* LoadGraphicsPipeline(string psoIdentifier)
    {
        if (_library.Get()->LoadGraphicsPipeline(psoIdentifier.AsSpan().GetPointer(), __uuidof<ID3D12PipelineState>(), out var pso).Failure)
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
        var psoDesc = new GraphicsPipelineStateDescription
        {
            pRootSignature = _defaultRootSignature.Get(),
            VS = new ShaderBytecode(shaderPipeline.vsResult.bytecode.GetUnsafePtr(), (nuint)shaderPipeline.vsResult.bytecode.Count),
            PS = new ShaderBytecode(shaderPipeline.psResult.bytecode.GetUnsafePtr(), (nuint)shaderPipeline.vsResult.bytecode.Count),
            InputLayout = D3D12PipelineResource.InputLayoutDescription,
            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.Default,
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            NumRenderTargets = 1,
            SampleDesc = new SampleDescription(1, 0),
            DSVFormat = Format.Unknown,
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