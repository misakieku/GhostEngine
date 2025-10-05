using Ghost.Graphics.Data;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Dxc;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

internal unsafe static class D3D12ShaderCompiler
{
    public enum CompilerVersion
    {
        SM_6_6,
        SM_7_0
    }

    public enum ShaderStage
    {
        VertexShader,
        PixelShader,
        MeshShader,
        ComputeShader
    }

    public struct CompileResult : IDisposable
    {
        public UnsafeArray<byte> bytecode;
        public ComPtr<IDxcBlob> reflection;

        public void Dispose()
        {
            bytecode.Dispose();
            reflection.Dispose();
        }
    }

    private static string GetProfileString(ShaderStage stage, CompilerVersion version)
    {
        return (stage, version) switch
        {
            (ShaderStage.VertexShader, CompilerVersion.SM_6_6) => "vs_6_6",
            (ShaderStage.PixelShader, CompilerVersion.SM_6_6) => "ps_6_6",
            (ShaderStage.MeshShader, CompilerVersion.SM_6_6) => "ms_6_6",
            (ShaderStage.ComputeShader, CompilerVersion.SM_6_6) => "cs_6_6",
            (ShaderStage.VertexShader, CompilerVersion.SM_7_0) => "vs_7_0",
            (ShaderStage.PixelShader, CompilerVersion.SM_7_0) => "ps_7_0",
            (ShaderStage.MeshShader, CompilerVersion.SM_7_0) => "ms_7_0",
            (ShaderStage.ComputeShader, CompilerVersion.SM_7_0) => "cs_7_0",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), "Unsupported shader stage or compiler version")
        };
    }

    private static string GetEntryPoint(ShaderStage stage)
    {
        return stage switch
        {
            ShaderStage.VertexShader => "VSMain",
            ShaderStage.PixelShader => "PSMain",
            ShaderStage.MeshShader => "MSMain",
            ShaderStage.ComputeShader => "CSMain",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), "Unsupported shader stage")
        };
    }

    public static CompileResult Compile(string shaderPath, ShaderStage stage, CompilerVersion version)
    {
        using ComPtr<IDxcCompiler3> compiler = default;
        using ComPtr<IDxcUtils> utils = default;
        using ComPtr<IDxcIncludeHandler> includeHandler = default;

        // Create DXC compiler and utils
        DxcCreateInstance(in CLSID_DxcCompiler, __uuidof<IDxcCompiler3>(), compiler.GetVoidAddressOf());
        DxcCreateInstance(in CLSID_DxcUtils, __uuidof<IDxcUtils>(), utils.GetVoidAddressOf());
        utils.Get()->CreateDefaultIncludeHandler(includeHandler.GetAddressOf());

        // Create source blob
        using ComPtr<IDxcBlobEncoding> sourceBlob = default;
        //var sourceBytes = System.Text.Encoding.UTF8.GetBytes(shaderPath);

        fixed (char* pShaderPath = shaderPath.AsSpan())
        {
            utils.Get()->LoadFile(pShaderPath, null, sourceBlob.GetAddressOf());
            //utils.Get()->CreateBlob(sourceBytesPtr, (uint)sourceBytes.Length, DXC_CP_UTF8, sourceBlob.GetAddressOf());
        }

        // Prepare compilation arguments - NOTE: NO -Qstrip_reflect to keep reflection data
        var argsArray = new string[]
        {
            "-T", GetProfileString(stage, version),                 // Target profile (vs_6_6, ps_6_6)
            "-E", GetEntryPoint(stage),                             // Entry point
            "-HV", "2021",                                          // HLSL version 2021 (required for SM 6.6)
            "-enable-16bit-types",                                  // Enable 16-bit types
            "-O3",                                                  // Optimization level
            "-Qstrip_debug"                                         // Strip debug info but KEEP reflection
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

                compiler.Get()->Compile(&buffer, (char**)argsPtr, (uint)argsArray.Length, includeHandler.Get(), __uuidof<IDxcResult>(), result.GetVoidAddressOf());
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

            var bytecodeSize = bytecodeBlob.Get()->GetBufferSize();
            var bytecode = new UnsafeArray<byte>((int)bytecodeSize, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

            NativeMemory.Copy(bytecodeBlob.Get()->GetBufferPointer(), bytecode.GetUnsafePtr(), bytecodeSize);

            return new CompileResult
            {
                bytecode = bytecode,
                reflection = reflectionBlob.Move()
            };
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

    private static void AddProperty(ref Shader shader, string name, PropertyInfo propertyInfo)
    {
        var id = shader.Properties.Count;
        shader.Properties.Add(propertyInfo);
        shader.PropertyNameToIdMap[name] = id;
    }

    public static void PerformDXCReflection(ref Shader shader, IDxcBlob* reflectionBlob)
    {
        // Create DXC utils to parse reflection data
        using ComPtr<IDxcUtils> utils = default;
        DxcCreateInstance(CLSID_DxcUtils, __uuidof<IDxcUtils>(), utils.GetVoidAddressOf());

        // Create reflection interface from blob
        var reflectionData = new DxcBuffer
        {
            Ptr = reflectionBlob->GetBufferPointer(),
            Size = reflectionBlob->GetBufferSize(),
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

        var cbufferRegistry = new Dictionary<string, CBufferInfo>();
        var textureRegistry = new Dictionary<string, TextureInfo>();

        for (uint i = 0; i < shaderDesc.BoundResources; i++)
        {
            ShaderInputBindDescription bindDesc;
            reflection.Get()->GetResourceBindingDesc(i, &bindDesc);

            switch (bindDesc.Type)
            {
                case ShaderInputType.ConstantBuffer:
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
                        if (variableName == null || shader.PropertyNameToIdMap.ContainsKey(variableName))
                        {
                            continue;
                        }

                        var propInfo = new PropertyInfo
                        {
                            CBufferIndex = cbufferInfo.RegisterSlot,
                            ByteOffset = varDesc.StartOffset,
                            Size = varDesc.Size
                        };

                        AddProperty(ref shader, variableName, propInfo);
                    }

                    break;
                }

                case ShaderInputType.Texture:
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
                        RegisterSlot = bindDesc.BindPoint,
                        RootParameterIndex = (uint)shader.ConstantBuffers.Count // Descriptor table comes after CBVs
                    };

                    textureRegistry.Add(textureName, textureInfo);
                    break;
                }
            }
        }

        shader.ConstantBuffers.Clear();
        foreach (var cbuf in cbufferRegistry.Values)
        {
            shader.ConstantBuffers.Add(cbuf);
        }

        shader.RegularTextures.Clear();
        foreach (var tex in textureRegistry.Values)
        {
            shader.RegularTextures.Add(tex);
        }
    }
}