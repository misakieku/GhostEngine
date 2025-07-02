using Ghost.Core;
using Ghost.Graphics.D3D12;
using System.Runtime.InteropServices;
using System.Text;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Fxc;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Data;

public unsafe class Shader
{
    private static readonly Shader s_empty = new("ErrorShader");
    public static Shader Empty => s_empty;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    public ConstPtr<ID3D12RootSignature> RootSignature => new(_rootSignature.Get());

    public Shader(string shaderPath)
    {
    }

    /// <summary>
    /// Compiles HLSL source code from a string into shader bytecode.
    /// </summary>
    /// <param name="sourceCode">The string containing the HLSL code.</param>
    /// <param name="entryPoint">The name of the shader entry point function (e.g., "VSMain").</param>
    /// <param name="shaderProfile">The shader model to target (e.g., "vs_5_0", "ps_5_0").</param>
    /// <returns>A byte array containing the compiled shader bytecode.</returns>
    /// <exception cref="Exception">Thrown if shader compilation fails.</exception>
    public static unsafe byte[] CompileShader(string sourceCode, string entryPoint, string shaderProfile)
    {
        ComPtr<ID3DBlob> bytecodeBlob = default;
        ComPtr<ID3DBlob> errorBlob = default;

        // Convert strings to null-terminated ASCII for the native function
        var sourceCodeBytes = Encoding.UTF8.GetBytes(sourceCode);
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
            // If compilation fails, get the error message from the error blob
            var errorMessage = "Shader compilation failed.";
            if (errorBlob.Get() is not null)
            {
                errorMessage += "\n" + Encoding.ASCII.GetString(
                    (byte*)errorBlob.Get()->GetBufferPointer(),
                    (int)errorBlob.Get()->GetBufferSize()
                );
            }
            errorBlob.Dispose();
            throw new Exception(errorMessage);
        }

        // Copy the compiled bytecode from the blob into a managed byte array
        var bytecode = new byte[bytecodeBlob.Get()->GetBufferSize()];
        Marshal.Copy((IntPtr)bytecodeBlob.Get()->GetBufferPointer(), bytecode, 0, bytecode.Length);

        // Clean up the COM blobs
        bytecodeBlob.Dispose();
        errorBlob.Dispose();

        return bytecode;
    }

    private void LoadShader(Span<byte> byteCode)
    {
        using ComPtr<ID3D12ShaderReflection> reflector = default;
        fixed (void* codePtr = byteCode)
        {
            D3DReflect(codePtr, (nuint)byteCode.Length, __uuidof<ID3D12ShaderReflection>(), reflector.GetVoidAddressOf());
        }

        ShaderDescription shaderDesc;
        reflector.Get()->GetDesc(&shaderDesc);

        var rootParameters = new List<RootParameter>();
        var staticSamplers = new List<StaticSamplerDescription>();

        for (uint i = 0; i < shaderDesc.BoundResources; i++)
        {
            ShaderInputBindDescription bindDesc;
            reflector.Get()->GetResourceBindingDesc(i, &bindDesc);

            switch (bindDesc.Type)
            {
                case ShaderInputType.ConstantBuffer:
                    var cbufferParam = new RootParameter();
                    cbufferParam.ParameterType = RootParameterType.Cbv;
                    cbufferParam.ShaderVisibility = ShaderVisibility.All;
                    cbufferParam.Descriptor.RegisterSpace = bindDesc.Space;
                    cbufferParam.Descriptor.ShaderRegister = bindDesc.BindPoint;

                    rootParameters.Add(cbufferParam);

                    var cbuffer = reflector.Get()->GetConstantBufferByName(bindDesc.Name);
                    ShaderBufferDescription cbufferDesc;
                    cbuffer->GetDesc(&cbufferDesc);

                    for (var j = 0u; j < cbufferDesc.Variables; j++)
                    {
                        var variable = cbuffer->GetVariableByIndex(j);
                        ShaderVariableDescription varDesc;
                        variable->GetDesc(&varDesc);
                    }

                    break;
                case ShaderInputType.TextureBuffer:
                    break;
                case ShaderInputType.Texture:
                    break;
                case ShaderInputType.Sampler:
                    var samplerDesc = new StaticSamplerDescription
                    {
                        Filter = Filter.MinMagMipLinear,
                        AddressU = TextureAddressMode.Wrap,
                        AddressV = TextureAddressMode.Wrap,
                        AddressW = TextureAddressMode.Wrap,
                        ShaderVisibility = ShaderVisibility.All,
                        ShaderRegister = bindDesc.BindPoint,
                        RegisterSpace = bindDesc.Space,
                    };
                    staticSamplers.Add(samplerDesc);
                    break;

                case ShaderInputType.UavRwTyped:
                    break;
                case ShaderInputType.Structured:
                    break;
                case ShaderInputType.UavRwStructured:
                    break;
                case ShaderInputType.ByteAddress:
                    break;
                case ShaderInputType.UavRwByteAddress:
                    break;
                case ShaderInputType.UavAppendStructured:
                    break;
                case ShaderInputType.UavConsumeStructured:
                    break;
                case ShaderInputType.UavRwStructuredWithCounter:
                    break;
                case ShaderInputType.RtAccelerationStructure:
                    break;
                case ShaderInputType.UavFeedbackTexture:
                    break;
                default:
                    break;
            }
        }
    }

    private void CreateRootSignature()
    {
        var rootSignatureDesc = new RootSignatureDescription();

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        var hr = D3D12SerializeRootSignature(&rootSignatureDesc, RootSignatureVersion.V1_2, signature.GetAddressOf(), error.GetAddressOf());
        if (hr.Failure)
        {
            var errorMessage = System.Text.Encoding.ASCII.GetString((byte*)error.Get()->GetBufferPointer(), (int)error.Get()->GetBufferSize());
            throw new Exception($"Failed to serialize root signature: {errorMessage}");
        }

        GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>().NativeDevice.Ptr->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
    }
}