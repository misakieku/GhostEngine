using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Dxc;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Shading;

internal readonly struct TextureInfo
{
    public required string Name
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }

    public uint RootParameterIndex
    {
        get; init;
    }
}

internal readonly struct PropertyInfo
{
    public required string Name
    {
        get; init;
    }

    public uint CBufferIndex
    {
        get; init;
    }

    public uint ByteOffset
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }
}

internal readonly struct CBufferInfo
{
    public required string Name
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }
}

/// <summary>
/// Bindless shader implementation using SM 6.6 with ResourceDescriptorHeap
/// and D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
/// Enhanced to support both bindless and regular texture binding for hybrid materials
/// </summary>
public unsafe class Shader : IDisposable
{
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12DescriptorHeap> _samplerHeap;

    private readonly byte[] _vertexShaderBytecode;
    private readonly byte[] _pixelShaderBytecode;

    private readonly List<CBufferInfo> _constantBuffers = new();
    private readonly List<PropertyInfo> _properties = new();
    private readonly Dictionary<string, int> _propertyNameToIdMap = new();
    private readonly List<TextureInfo> _regularTextures = new(); // Add regular texture support

    private bool _disposed;

    internal ConstPtr<ID3D12PipelineState> PipelineState => new(_pipelineState.Get());
    internal ConstPtr<ID3D12RootSignature> RootSignature => new(_rootSignature.Get());
    internal ConstPtr<ID3D12DescriptorHeap> SamplerHeap => new(_samplerHeap.Get());

    internal IReadOnlyList<CBufferInfo> ConstantBuffers => _constantBuffers;
    internal IReadOnlyList<PropertyInfo> Properties => _properties;
    internal IReadOnlyList<TextureInfo> RegularTextures => _regularTextures; // Expose regular textures

    public Shader(string shaderCode)
    {
        var (vsBytecode, vsReflection) = CompileShaderDXC(shaderCode, "VSMain", "vs_6_6");
        var (psBytecode, psReflection) = CompileShaderDXC(shaderCode, "PSMain", "ps_6_6");

        _vertexShaderBytecode = vsBytecode;
        _pixelShaderBytecode = psBytecode;

        PerformDXCReflection(vsReflection);
        PerformDXCReflection(psReflection);

        CreateBindlessRootSignature();
        CreatePipelineState();
        CreateSamplerHeap();
    }

    private (byte[] bytecode, ComPtr<IDxcBlob> reflection) CompileShaderDXC(string source, string entryPoint, string profile)
    {
        using ComPtr<IDxcCompiler3> compiler = default;
        using ComPtr<IDxcUtils> utils = default;

        // Create DXC compiler and utils
        DxcCreateInstance(CLSID_DxcCompiler, __uuidof<IDxcCompiler3>(), compiler.GetVoidAddressOf());
        DxcCreateInstance(CLSID_DxcUtils, __uuidof<IDxcUtils>(), utils.GetVoidAddressOf());

        // Create source blob
        using ComPtr<IDxcBlobEncoding> sourceBlob = default;
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(source);
        fixed (byte* sourceBytesPtr = sourceBytes)
        {
            utils.Get()->CreateBlob(sourceBytesPtr, (uint)sourceBytes.Length, DXC_CP_UTF8, sourceBlob.GetAddressOf());
        }

        // Prepare compilation arguments - NOTE: NO -Qstrip_reflect to keep reflection data
        var argsArray = new string[]
        {
            "-T", profile,           // Target profile (vs_6_6, ps_6_6)
            "-E", entryPoint,        // Entry point
            "-HV", "2021",           // HLSL version 2021 (required for SM 6.6)
            "-enable-16bit-types",   // Enable 16-bit types
            "-O3",                   // Optimization level
            "-Qstrip_debug"          // Strip debug info but KEEP reflection
        };

        // Convert to wide strings (DXC expects LPCWSTR)
        var wideArgs = new nuint[argsArray.Length];
        var argPointers = new IntPtr[argsArray.Length];

        for (var i = 0; i < argsArray.Length; i++)
        {
            argPointers[i] = Marshal.StringToHGlobalUni(argsArray[i]);
            wideArgs[i] = (nuint)argPointers[i];
        }

        try
        {
            // Compile shader
            using ComPtr<IDxcResult> result = default;
            fixed (nuint* argsPtr = wideArgs)
            {
                var buffer = new DxcBuffer
                {
                    Ptr = sourceBlob.Get()->GetBufferPointer(),
                    Size = sourceBlob.Get()->GetBufferSize(),
                    Encoding = DXC_CP_UTF8
                };

                compiler.Get()->Compile(&buffer, (char**)argsPtr, (uint)argsArray.Length, null, __uuidof<IDxcResult>(), result.GetVoidAddressOf());
            }

            // Check compilation result
            HResult hrStatus;
            result.Get()->GetStatus(&hrStatus);
            if (hrStatus.Failure)
            {
                // Get error messages
                using ComPtr<IDxcBlobEncoding> errorBlob = default;
                result.Get()->GetErrorBuffer(errorBlob.GetAddressOf());

                if (errorBlob.Get() != null)
                {
                    var errorMessage = Marshal.PtrToStringUni((IntPtr)errorBlob.Get()->GetBufferPointer());
                    throw new Exception($"DXC shader compilation failed: {errorMessage}");
                }
                else
                {
                    throw new Exception("DXC shader compilation failed with unknown error");
                }
            }

            // Get compiled bytecode
            using ComPtr<IDxcBlob> bytecodeBlob = default;
            result.Get()->GetResult(bytecodeBlob.GetAddressOf());

            if (bytecodeBlob.Get() == null)
            {
                throw new Exception("DXC compilation succeeded but no bytecode was produced");
            }

            // Get reflection data using DXC API
            using ComPtr<IDxcBlob> reflectionBlob = default;
            result.Get()->GetOutput(DxcOutKind.Reflection, __uuidof<IDxcBlob>(), reflectionBlob.GetVoidAddressOf(), null);

            if (reflectionBlob.Get() == null)
            {
                throw new Exception("DXC compilation succeeded but no reflection data was produced");
            }

            // Copy bytecode to managed array
            var bytecodeSize = (int)bytecodeBlob.Get()->GetBufferSize();
            var bytecode = new byte[bytecodeSize];

            fixed (byte* bytecodePtr = bytecode)
            {
                Buffer.MemoryCopy(bytecodeBlob.Get()->GetBufferPointer(), bytecodePtr, bytecodeSize, bytecodeSize);
            }

            // Return both bytecode and reflection blob (move ownership)
            return (bytecode, reflectionBlob.Move());
        }
        finally
        {
            // Free allocated wide strings
            for (var i = 0; i < argPointers.Length; i++)
            {
                Marshal.FreeHGlobal(argPointers[i]);
            }
        }
    }

    private void CreateBindlessRootSignature()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Calculate total root parameters: CBVs + Regular texture descriptor table + Sampler table
        var totalRootParams = _constantBuffers.Count + (_regularTextures.Count > 0 ? 1 : 0) + 1; // +1 for sampler
        var rootParameters = new RootParameter1[totalRootParams];

        var parameterIndex = 0;

        // Add CBV root parameters
        foreach (var cbufferInfo in _constantBuffers)
        {
            rootParameters[parameterIndex++] = new RootParameter1
            {
                ParameterType = RootParameterType.Cbv,
                ShaderVisibility = ShaderVisibility.All,
                Descriptor = new RootDescriptor1(cbufferInfo.RegisterSlot, 0),
            };
        }

        // Add regular texture descriptor table if we have regular textures
        if (_regularTextures.Count > 0)
        {
            var textureRanges = new DescriptorRange1[1];
            textureRanges[0] = new DescriptorRange1
            {
                RangeType = DescriptorRangeType.Srv,
                NumDescriptors = (uint)_regularTextures.Count,
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

            device->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
        }
    }

    private void CreatePipelineState()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        fixed (byte* vsPtr = _vertexShaderBytecode)
        fixed (byte* psPtr = _pixelShaderBytecode)
        {
            var psoDesc = new GraphicsPipelineStateDescription
            {
                pRootSignature = _rootSignature.Get(),
                VS = new ShaderBytecode(vsPtr, (nuint)_vertexShaderBytecode.Length),
                PS = new ShaderBytecode(psPtr, (nuint)_pixelShaderBytecode.Length),
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

            device->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), _pipelineState.GetVoidAddressOf());
        }
    }

    private void CreateSamplerHeap()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Create sampler heap
        var samplerHeapDesc = new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.Sampler,
            NumDescriptors = 1,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        device->CreateDescriptorHeap(&samplerHeapDesc, __uuidof<ID3D12DescriptorHeap>(), _samplerHeap.GetVoidAddressOf());

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

        var samplerHandle = _samplerHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        device->CreateSampler(&samplerDesc, samplerHandle);
    }

    private unsafe void PerformDXCReflection(ComPtr<IDxcBlob> reflectionBlob)
    {
        // Create DXC utils to parse reflection data
        using ComPtr<IDxcUtils> utils = default;
        DxcCreateInstance(CLSID_DxcUtils, __uuidof<IDxcUtils>(), utils.GetVoidAddressOf());

        // Create reflection interface from blob
        var reflectionData = new DxcBuffer
        {
            Ptr = reflectionBlob.Get()->GetBufferPointer(),
            Size = reflectionBlob.Get()->GetBufferSize(),
            Encoding = DXC_CP_ACP
        };

        using ComPtr<ID3D12ShaderReflection> reflection = default;
        utils.Get()->CreateReflection(&reflectionData, __uuidof<ID3D12ShaderReflection>(), reflection.GetVoidAddressOf());

        if (reflection.Get() == null)
        {
            throw new Exception("Failed to create shader reflection from DXC output");
        }

        ShaderDescription shaderDesc;
        reflection.Get()->GetDesc(&shaderDesc);

        var cbufferRegistry = _constantBuffers.ToDictionary(cb => cb.Name);
        var textureRegistry = _regularTextures.ToDictionary(t => t.Name);

        for (uint i = 0; i < shaderDesc.BoundResources; i++)
        {
            ShaderInputBindDescription bindDesc;
            reflection.Get()->GetResourceBindingDesc(i, &bindDesc);

            if (bindDesc.Type == ShaderInputType.ConstantBuffer)
            {
                var cbufferName = Marshal.PtrToStringAnsi((IntPtr)bindDesc.Name);
                if (cbufferName == null || cbufferRegistry.ContainsKey(cbufferName))
                {
                    continue;
                }

                var cbuffer = reflection.Get()->GetConstantBufferByName(bindDesc.Name);
                ShaderBufferDescription cbufferDesc;
                cbuffer->GetDesc(&cbufferDesc);

                var cbufferInfo = new CBufferInfo
                {
                    Name = cbufferName,
                    Size = cbufferDesc.Size,
                    RegisterSlot = bindDesc.BindPoint
                };
                cbufferRegistry.Add(cbufferName, cbufferInfo);

                for (uint j = 0; j < cbufferDesc.Variables; j++)
                {
                    var variable = cbuffer->GetVariableByIndex(j);
                    ShaderVariableDescription varDesc;
                    variable->GetDesc(&varDesc);

                    var variableName = Marshal.PtrToStringAnsi((IntPtr)varDesc.Name);
                    if (variableName == null || _propertyNameToIdMap.ContainsKey(variableName))
                    {
                        continue;
                    }

                    var propInfo = new PropertyInfo
                    {
                        Name = variableName,
                        CBufferIndex = cbufferInfo.RegisterSlot,
                        ByteOffset = varDesc.StartOffset,
                        Size = varDesc.Size
                    };

                    // Add to the list and create the name-to-ID mapping
                    var newId = _properties.Count;
                    _properties.Add(propInfo);
                    _propertyNameToIdMap.Add(variableName, newId);
                }
            }
            else if (bindDesc.Type == ShaderInputType.Texture)
            {
                var textureName = Marshal.PtrToStringAnsi((IntPtr)bindDesc.Name);
                if (textureName == null || textureRegistry.ContainsKey(textureName))
                {
                    continue;
                }

                // ALL texture input slots are regular textures!
                // Bindless textures don't use explicit texture inputs - they use ResourceDescriptorHeap[index]
                var textureInfo = new TextureInfo
                {
                    Name = textureName,
                    RegisterSlot = bindDesc.BindPoint,
                    RootParameterIndex = (uint)_constantBuffers.Count // Descriptor table comes after CBVs
                };

                textureRegistry.Add(textureName, textureInfo);
            }
        }

        _constantBuffers.Clear();
        _constantBuffers.AddRange(cbufferRegistry.Values);

        _regularTextures.Clear();
        _regularTextures.AddRange(textureRegistry.Values);
    }

    /// <summary>
    /// Gets a unique, stable ID for a shader property.
    /// </summary>
    /// <param name="propertyName">The name of the property (e.g., "_Color").</param>
    /// <returns>The integer ID of the property, or -1 if not found.</returns>
    public int GetPropertyId(string propertyName)
    {
        return _propertyNameToIdMap.TryGetValue(propertyName, out var id) ? id : -1;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _samplerHeap.Dispose();

        _constantBuffers.Clear();
        _properties.Clear();
        _propertyNameToIdMap.Clear();
        _regularTextures.Clear();

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}