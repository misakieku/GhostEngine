using Ghost.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.D3D12.Utilities;
using Misaki.HighPerformance.LowLevel.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Fxc;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Shading;

internal readonly struct PropertyInfo
{
    public required string Name
    {
        get; init;
    }

    public required string CBufferName
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

    private readonly Dictionary<string, CBufferInfo> _constantBuffers = new();
    private readonly Dictionary<string, PropertyInfo> _properties = new();

    private bool _disposed;

    internal ConstPtr<ID3D12PipelineState> PipelineState => new(_pipelineState.Get());
    internal ConstPtr<ID3D12RootSignature> RootSignature => new(_rootSignature.Get());

    internal IReadOnlyDictionary<string, CBufferInfo> ConstantBuffers => _constantBuffers;
    internal IReadOnlyDictionary<string, PropertyInfo> Properties => _properties;

    //public Shader(string shaderPath)
    //{

    //}

    public Shader(string shaderCode)
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
    public static unsafe byte[] CompileShader(byte[] sourceCodeBytes, string entryPoint, string shaderProfile)
    {
        using ComPtr<ID3DBlob> bytecodeBlob = default;
        using ComPtr<ID3DBlob> errorBlob = default;

        var entryPointBytes = Encoding.UTF8.GetBytes(entryPoint);
        var shaderProfileBytes = Encoding.UTF8.GetBytes(shaderProfile);

        // Call the D3DCompile function
        var hr = D3DCompile(
            sourceCodeBytes.AsSpan(),
            entryPointBytes.AsSpan(),
            shaderProfileBytes.AsSpan(),
            CompileFlags.EnableStrictness | CompileFlags.Debug,
            bytecodeBlob.GetAddressOf(),
            errorBlob.GetAddressOf()
        );

        if (hr.Failure)
        {
            var errorMessage = "Shader compilation failed.";
            if (errorBlob.Get() is not null)
            {
                errorMessage += "\n" + Encoding.ASCII.GetString(
                    (byte*)errorBlob.Get()->GetBufferPointer(),
                    (int)errorBlob.Get()->GetBufferSize()
                );
            }

            throw new Exception(errorMessage);
        }

        var bytecode = new byte[bytecodeBlob.Get()->GetBufferSize()];
        Unsafe.CopyBlock(bytecode.AsSpan().GetPointer(), bytecodeBlob.Get()->GetBufferPointer(), (uint)bytecode.Length);

        return bytecode;
    }

    private void CreateRootSignature()
    {
        var rootParameters = new RootParameter1[_constantBuffers.Values.Count];

        var i = 0;
        foreach (var cbufferInfo in _constantBuffers.Values)
        {
            var rootParameter = new RootParameter1
            {
                ParameterType = RootParameterType.Cbv,
                ShaderVisibility = ShaderVisibility.All,
                Descriptor = new RootDescriptor1(cbufferInfo.RegisterSlot, 0),
            };

            rootParameters[i++] = rootParameter;
        }

        var rootSignatureDesc = new RootSignatureDescription((uint)rootParameters.Length, (RootParameter*)Unsafe.AsPointer(ref rootParameters[0]))
        {
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
        };

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        var hr = D3D12SerializeRootSignature(&rootSignatureDesc, RootSignatureVersion.V1_0, signature.GetAddressOf(), error.GetAddressOf());
        if (hr.Failure)
        {
            var errorMessage = System.Text.Encoding.ASCII.GetString((byte*)error.Get()->GetBufferPointer(), (int)error.Get()->GetBufferSize());
            throw new InvalidOperationException($"Failed to serialize root signature: {errorMessage}");
        }

        GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>().NativeDevice.Ptr->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
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
                GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>().NativeDevice.Ptr->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), _pipelineState.GetVoidAddressOf());
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

        for (uint i = 0; i < shaderDesc.BoundResources; i++)
        {
            ShaderInputBindDescription bindDesc;
            reflection.Get()->GetResourceBindingDesc(i, &bindDesc);

            if (bindDesc.Type == ShaderInputType.ConstantBuffer)
            {
                var cbufferName = Marshal.PtrToStringAnsi((IntPtr)bindDesc.Name);
                if (cbufferName == null || _constantBuffers.ContainsKey(cbufferName))
                {
                    continue;
                }

                var cbuffer = reflection.Get()->GetConstantBufferByName(bindDesc.Name);
                ShaderBufferDescription cbufferDesc;
                cbuffer->GetDesc(&cbufferDesc);

                _constantBuffers.Add(cbufferName, new CBufferInfo
                {
                    Name = cbufferName,
                    Size = cbufferDesc.Size,
                    RegisterSlot = bindDesc.BindPoint
                });

                for (uint j = 0; j < cbufferDesc.Variables; j++)
                {
                    var variable = cbuffer->GetVariableByIndex(j);
                    ShaderVariableDescription varDesc;
                    variable->GetDesc(&varDesc);

                    var variableName = Marshal.PtrToStringAnsi((IntPtr)varDesc.Name);
                    if (variableName == null || _properties.ContainsKey(variableName))
                    {
                        continue;
                    }

                    _properties.Add(variableName, new PropertyInfo
                    {
                        Name = variableName,
                        CBufferName = cbufferName,
                        ByteOffset = varDesc.StartOffset,
                        Size = varDesc.Size
                    });
                }
            }
        }
        reflection.Dispose();
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

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}