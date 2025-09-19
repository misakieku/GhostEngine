using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.D3D12;

internal class D3D12ShaderPipeline : IShaderPipeline, IDisposable
{
    public ComPtr<ID3D12RootSignature> rootSignature;
    public ComPtr<ID3D12PipelineState> pipelineState;
    public ComPtr<ID3D12DescriptorHeap> samplerHeap;
    public D3D12ShaderCompiler.CompileResult vsResult;
    public D3D12ShaderCompiler.CompileResult psResult;
    public D3D12ShaderCompiler.CompileResult csResult;

    public PipelineType Type
    {
        get; init;
    }

    public void Dispose()
    {
        rootSignature.Dispose();
        pipelineState.Dispose();
        samplerHeap.Dispose();
        vsResult.Dispose();
        psResult.Dispose();
        csResult.Dispose();
    }
}

internal unsafe class D3D12PipelineStateController : IPipelineStateController, IDisposable
{
    private readonly ID3D12Device14* _device;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private readonly Dictionary<Identifier<Shader>, D3D12ShaderPipeline> _shaderPipelines;

    public D3D12PipelineStateController(D3D12RenderDevice device, D3D12ResourceDatabase resourceDatabase)
    {
        _device = device.NativeDevice;
        _resourceDatabase = resourceDatabase;

        _shaderPipelines = new();
    }

    public void CompileShader(Identifier<Shader> id, string shaderPath)
    {
        var vsResult = D3D12ShaderCompiler.Compile(shaderPath, D3D12ShaderCompiler.ShaderStage.VertexShader, D3D12ShaderCompiler.CompilerVersion.SM_6_6);
        var psResult = D3D12ShaderCompiler.Compile(shaderPath, D3D12ShaderCompiler.ShaderStage.PixelShader, D3D12ShaderCompiler.CompilerVersion.SM_6_6);

        var shader = _resourceDatabase.GetShader(id);

        D3D12ShaderCompiler.PerformDXCReflection(shader, vsResult.reflection.Get());
        D3D12ShaderCompiler.PerformDXCReflection(shader, psResult.reflection.Get());

        var shaderPipeline = new D3D12ShaderPipeline
        {
            Type = PipelineType.Graphics,
            vsResult = vsResult,
            psResult = psResult
        };

        _shaderPipelines[id] = shaderPipeline;
    }

    private void CreateRootSignature(Shader shader, D3D12ShaderPipeline shaderPipeline)
    {
        // Calculate total root parameters: CBVs + Regular texture descriptor table + Sampler table
        var totalRootParams = shader.ConstantBuffers.Count + (shader.RegularTextures.Count > 0 ? 1 : 0) + 1; // +1 for sampler
        var rootParameters = new RootParameter1[totalRootParams];

        var parameterIndex = 0;

        // Add CBV root parameters
        foreach (var cbufferInfo in shader.ConstantBuffers)
        {
            rootParameters[parameterIndex++] = new RootParameter1
            {
                ParameterType = RootParameterType.Cbv,
                ShaderVisibility = ShaderVisibility.All,
                Descriptor = new RootDescriptor1(cbufferInfo.RegisterSlot, 0),
            };
        }

        // Add regular texture descriptor table if we have regular textures
        if (shader.RegularTextures.Count > 0)
        {
            var textureRanges = new DescriptorRange1[1];
            textureRanges[0] = new DescriptorRange1
            {
                RangeType = DescriptorRangeType.Srv,
                NumDescriptors = (uint)shader.RegularTextures.Count,
                BaseShaderRegister = 0, // Start from t0
                RegisterSpace = 0,
                Flags = DescriptorRangeFlags.None,
                OffsetInDescriptorsFromTableStart = 0
            };

            fixed (DescriptorRange1* textureRangesPtr = textureRanges)
            {
                rootParameters[parameterIndex++] = new RootParameter1
                {
                    ParameterType = RootParameterType.DescriptorTable,
                    ShaderVisibility = ShaderVisibility.All,
                    DescriptorTable = new RootDescriptorTable1(1, textureRangesPtr)
                };
            }
        }

        // Sampler descriptor table (still needed for samplers)
        var samplerRanges = new DescriptorRange1[1];
        samplerRanges[0] = new DescriptorRange1
        {
            RangeType = DescriptorRangeType.Sampler,
            NumDescriptors = 1,
            BaseShaderRegister = 0, // s0
            RegisterSpace = 0,
            Flags = DescriptorRangeFlags.None,
            OffsetInDescriptorsFromTableStart = 0
        };

        fixed (DescriptorRange1* samplerRangesPtr = samplerRanges)
        {
            rootParameters[parameterIndex] = new RootParameter1
            {
                ParameterType = RootParameterType.DescriptorTable,
                ShaderVisibility = ShaderVisibility.All,
                DescriptorTable = new RootDescriptorTable1(1, samplerRangesPtr)
            };
        }

        // Create root signature with the modern flag
        fixed (RootParameter1* rootParamsPtr = rootParameters)
        {
            var rootSignatureDesc = new RootSignatureDescription1
            {
                NumParameters = (uint)rootParameters.Length,
                pParameters = rootParamsPtr,
                NumStaticSamplers = 0,
                pStaticSamplers = null,
                // Key difference: Use the modern flag for direct heap indexing
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout |
                       RootSignatureFlags.CbvSrvUavHeapDirectlyIndexed
            };

            var versionedDesc = new VersionedRootSignatureDescription
            {
                Version = RootSignatureVersion.V1_1,
                Desc_1_1 = rootSignatureDesc
            };

            using ComPtr<ID3DBlob> signature = default;
            using ComPtr<ID3DBlob> error = default;

            D3D12SerializeVersionedRootSignature(&versionedDesc, signature.GetAddressOf(), error.GetAddressOf());

            _device->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), shaderPipeline.rootSignature.GetVoidAddressOf());
        }
    }

    private void CreatePipelineStateObject(D3D12ShaderPipeline shaderPipeline)
    {
        var psoDesc = new GraphicsPipelineStateDescription
        {
            pRootSignature = shaderPipeline.rootSignature.Get(),
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

        _device->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), shaderPipeline.pipelineState.GetVoidAddressOf());
    }

    private void CreateSamplerHeap(D3D12ShaderPipeline shaderPipeline)
    {
        // Create sampler heap
        var samplerHeapDesc = new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.Sampler,
            NumDescriptors = 1,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        _device->CreateDescriptorHeap(&samplerHeapDesc, __uuidof<ID3D12DescriptorHeap>(), shaderPipeline.samplerHeap.GetVoidAddressOf());

        // Create default sampler
        var samplerDesc = new SamplerDescription
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        // Set border color manually
        samplerDesc.BorderColor[0] = 0;
        samplerDesc.BorderColor[1] = 0;
        samplerDesc.BorderColor[2] = 0;
        samplerDesc.BorderColor[3] = 0;

        var samplerHandle = shaderPipeline.samplerHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        _device->CreateSampler(&samplerDesc, samplerHandle);
    }

    // TODO: Pipeline variants (keywords)
    // TODO: Disk caching
    // TODO: Async compilation
    public void PreCookPipelineState()
    {
        foreach (var kvp in _shaderPipelines)
        {
            var shader = _resourceDatabase.GetShader(kvp.Key);

            CreateRootSignature(shader, kvp.Value);
            CreatePipelineStateObject(kvp.Value);
            CreateSamplerHeap(kvp.Value);
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