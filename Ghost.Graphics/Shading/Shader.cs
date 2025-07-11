using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Misaki.HighPerformance.LowLevel.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Fxc;
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

public unsafe class Shader : IDisposable
{
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;

    private readonly byte[] _vertexShaderBytecode;
    private readonly byte[] _pixelShaderBytecode;

    private readonly List<CBufferInfo> _constantBuffers = new();
    private readonly List<PropertyInfo> _properties = new();
    private readonly Dictionary<string, int> _propertyNameToIdMap = new();

    private readonly List<TextureInfo> _textures = new();
    private readonly Dictionary<string, int> _textureNameToIdMap = new();

    private bool _disposed;

    internal ConstPtr<ID3D12PipelineState> PipelineState => new(_pipelineState.Get());
    internal ConstPtr<ID3D12RootSignature> RootSignature => new(_rootSignature.Get());

    internal IReadOnlyList<CBufferInfo> ConstantBuffers => _constantBuffers;
    internal IReadOnlyList<PropertyInfo> Properties => _properties;
    internal IReadOnlyList<TextureInfo> Textures => _textures;

    //public Shader(string shaderPath)
    //{

    //}

    internal Shader(string shaderCode)
    {
        _vertexShaderBytecode = CompileShader(Encoding.UTF8.GetBytes(shaderCode), "VSMain", "vs_5_0");
        _pixelShaderBytecode = CompileShader(Encoding.UTF8.GetBytes(shaderCode), "PSMain", "ps_5_0");

        PerformReflection(_vertexShaderBytecode);
        PerformReflection(_pixelShaderBytecode);

        CreateRootSignature();
        CreatePipelineStateObject();
    }

    ~Shader()
    {
        Dispose();
    }

    /// <summary>
    /// Compiles HLSL source code from a string into shader bytecode.
    /// </summary>
    /// <param name="sourceCode">The string containing the HLSL code.</param>
    /// <param name="entryPoint">The name of the shader entry point function (e.g., "VSMain").</param>
    /// <param name="shaderProfile">The shader model to target (e.g., "vs_5_0", "ps_5_0").</param>
    /// <returns>A byte array containing the compiled shader bytecode.</returns>
    /// <exception cref="Exception">Thrown if shader compilation fails.</exception>
    public static unsafe byte[] CompileShader(ReadOnlySpan<byte> sourceCodeBytes, string entryPoint, string shaderProfile)
    {
        using ComPtr<ID3DBlob> bytecodeBlob = default;
        using ComPtr<ID3DBlob> errorBlob = default;

        var entryPointBytes = Encoding.UTF8.GetBytes(entryPoint);
        var shaderProfileBytes = Encoding.UTF8.GetBytes(shaderProfile);

        ThrowIfFailed(D3DCompile(
            sourceCodeBytes,
            entryPointBytes.AsSpan(),
            shaderProfileBytes.AsSpan(),
            CompileFlags.EnableStrictness | CompileFlags.Debug,
            bytecodeBlob.GetAddressOf(),
            errorBlob.GetAddressOf()
        ));

        var bytecode = new byte[bytecodeBlob.Get()->GetBufferSize()];
        Unsafe.CopyBlock(bytecode.AsSpan().GetPointer(), bytecodeBlob.Get()->GetBufferPointer(), (uint)bytecode.Length);

        return bytecode;
    }

    private void CreateRootSignature()
    {
        // Calculate total root parameters: CBVs + 1 descriptor table for SRVs (if any textures)
        var totalRootParameters = _constantBuffers.Count + (_textures.Count > 0 ? 1 : 0);
        var rootParameters = new RootParameter1[totalRootParameters];

        var parameterIndex = 0;

        // Add CBV root parameters
        for (var i = 0; i < _constantBuffers.Count; i++)
        {
            var cbufferInfo = _constantBuffers[i];
            Debug.Assert(i == cbufferInfo.RegisterSlot);

            var rootParameter = new RootParameter1
            {
                ParameterType = RootParameterType.Cbv,
                ShaderVisibility = ShaderVisibility.All,
                Descriptor = new RootDescriptor1(cbufferInfo.RegisterSlot, 0),
            };

            rootParameters[parameterIndex++] = rootParameter;
        }

        // Add descriptor table for SRVs if we have textures
        if (_textures.Count > 0)
        {
            var ranges = new DescriptorRange1[1];
            ranges[0] = new DescriptorRange1
            {
                RangeType = DescriptorRangeType.Srv,
                NumDescriptors = (uint)_textures.Count,
                BaseShaderRegister = 0, // Start from t0
                RegisterSpace = 0,
                Flags = DescriptorRangeFlags.None,
                OffsetInDescriptorsFromTableStart = 0
            };

            fixed (DescriptorRange1* rangesPtr = ranges)
            {
                var rootParameter = new RootParameter1
                {
                    ParameterType = RootParameterType.DescriptorTable,
                    ShaderVisibility = ShaderVisibility.All,
                    DescriptorTable = new RootDescriptorTable1(1, rangesPtr)
                };

                rootParameters[parameterIndex++] = rootParameter;
            }
        }

        // Create static samplers for textures
        var staticSamplers = new StaticSamplerDescription[_textures.Count];
        for (var i = 0; i < _textures.Count; i++)
        {
            staticSamplers[i] = new StaticSamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0,
                MaxAnisotropy = 1,
                BorderColor = StaticBorderColor.OpaqueWhite,
                MinLOD = 0,
                MaxLOD = 0,
                ShaderRegister = (uint)i, // s0, s1, etc.
                RegisterSpace = 0,
                ShaderVisibility = ShaderVisibility.All
            };
        }

        var parameterCount = (uint)rootParameters.Length;
        var parameters = parameterCount > 0
            ? (RootParameter*)Unsafe.AsPointer(ref rootParameters[0])
            : null;

        var samplerCount = (uint)staticSamplers.Length;
        var samplers = samplerCount > 0
            ? (StaticSamplerDescription*)Unsafe.AsPointer(ref staticSamplers[0])
            : null;

        var rootSignatureDesc = new RootSignatureDescription(parameterCount, parameters, samplerCount, samplers)
        {
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
        };

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        ThrowIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, RootSignatureVersion.V1_0, signature.GetAddressOf(), error.GetAddressOf()));

        GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
    }

    private void CreatePipelineStateObject()
    {
        try
        {
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

                // Create the PSO
                GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), _pipelineState.GetVoidAddressOf());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private unsafe void PerformReflection(Span<byte> byteCode)
    {
        using ComPtr<ID3D12ShaderReflection> reflection = default;
        fixed (void* codePtr = byteCode)
        {
            D3DReflect(codePtr, (nuint)byteCode.Length, __uuidof<ID3D12ShaderReflection>(), reflection.GetVoidAddressOf());
        }

        ShaderDescription shaderDesc;
        reflection.Get()->GetDesc(&shaderDesc);

        var cbufferRegistry = _constantBuffers.ToDictionary(cb => cb.Name);
        var textureRegistry = _textures.ToDictionary(t => t.Name);

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

                // The root parameter index for textures is after all CBVs
                var textureInfo = new TextureInfo
                {
                    Name = textureName,
                    RegisterSlot = bindDesc.BindPoint,
                    RootParameterIndex = (uint)_constantBuffers.Count // Descriptor table comes after CBVs
                };

                textureRegistry.Add(textureName, textureInfo);

                // Add to the texture name-to-ID mapping
                var newId = _textures.Count;
                _textureNameToIdMap.Add(textureName, newId);
            }
        }

        _constantBuffers.Clear();
        _constantBuffers.AddRange(cbufferRegistry.Values);

        _textures.Clear();
        _textures.AddRange(textureRegistry.Values);

        reflection.Dispose();
    }

    /// <summary>
    /// Gets a unique, stable ID for a shader property.
    /// This should be called once and the ID cached for repeated use.
    /// </summary>
    /// <param name="propertyName">The name of the property (e.g., "_Color").</param>
    /// <returns>The integer ID of the property, or -1 if not found.</returns>
    public int GetPropertyId(string propertyName)
    {
        return _propertyNameToIdMap.TryGetValue(propertyName, out var id) ? id : -1;
    }

    /// <summary>
    /// Gets a unique, stable ID for a texture property.
    /// This should be called once and the ID cached for repeated use.
    /// </summary>
    /// <param name="textureName">The name of the texture (e.g., "_MainTex").</param>
    /// <returns>The integer ID of the texture, or -1 if not found.</returns>
    public int GetTextureId(string textureName)
    {
        return _textureNameToIdMap.TryGetValue(textureName, out var id) ? id : -1;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pipelineState.Dispose();
        _rootSignature.Dispose();

        _constantBuffers.Clear();
        _properties.Clear();
        _textures.Clear();
        _textureNameToIdMap.Clear();

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}