using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.DXC;

namespace Ghost.Graphics.D3D12;

internal partial class D3D12ShaderCompiler
{
    private static string GetProfileString(ShaderStage stage, CompilerTier version)
    {
        return (stage, version) switch
        {
            (ShaderStage.TaskShader, CompilerTier.Tier0) => "as_6_6",
            (ShaderStage.PixelShader, CompilerTier.Tier0) => "ps_6_6",
            (ShaderStage.MeshShader, CompilerTier.Tier0) => "ms_6_6",
            (ShaderStage.ComputeShader, CompilerTier.Tier0) => "cs_6_6",
            (ShaderStage.TaskShader, CompilerTier.Tier1) => "as_6_7",
            (ShaderStage.PixelShader, CompilerTier.Tier1) => "ps_6_7",
            (ShaderStage.MeshShader, CompilerTier.Tier1) => "ms_6_7",
            (ShaderStage.ComputeShader, CompilerTier.Tier1) => "cs_6_7",
            (ShaderStage.TaskShader, CompilerTier.Tier2) => "as_6_8",
            (ShaderStage.PixelShader, CompilerTier.Tier2) => "ps_6_8",
            (ShaderStage.MeshShader, CompilerTier.Tier2) => "ms_6_8",
            (ShaderStage.ComputeShader, CompilerTier.Tier2) => "cs_6_8",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), "Unsupported shader stage or compiler version")
        };
    }

    private static string GetOptimizeLevelString(CompilerOptimizeLevel level)
    {
        return level switch
        {
            CompilerOptimizeLevel.O0 => DXC_ARG_OPTIMIZATION_LEVEL0,
            CompilerOptimizeLevel.O1 => DXC_ARG_OPTIMIZATION_LEVEL1,
            CompilerOptimizeLevel.O2 => DXC_ARG_OPTIMIZATION_LEVEL2,
            CompilerOptimizeLevel.O3 => DXC_ARG_OPTIMIZATION_LEVEL3,
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Unsupported optimization level")
        };
    }

    private static List<string> GetCompilerArguments(ref readonly CompilerConfig config)
    {
        var argsArray = new List<string>
        {
            "-T", GetProfileString(config.stage, config.tier),   // Target profile (ms_6_6, ps_6_6)
            "-E", config.entryPoint,                                    // Entry point
            "-HV", "2021",                                              // HLSL version 2021
            "-enable-16bit-types",                                      // Enable 16-bit types
            GetOptimizeLevelString(config.optimizeLevel),         // Optimization level
        };

        foreach (var define in config.defines)
        {
            argsArray.Add("-D");
            argsArray.Add(define);
        }

        // HACK: Currently DXC does not support force include, we have to use GENERATED_CODE_PATH define as a workaround.
        // User must to write '#include GENERATED_CODE_PATH' in their shader code manually.
        if (File.Exists(config.include))
        {
            argsArray.Add("-D");
            argsArray.Add($"GENERATED_CODE_PATH={'"' + config.include.Replace("\\", "/") + '"'}");
        }

        if (!config.options.HasFlag(CompilerOption.KeepDebugInfo))
        {
            argsArray.Add("-Qstrip_debug");
        }

        if (!config.options.HasFlag(CompilerOption.KeepReflections))
        {
            argsArray.Add("-Qstrip_reflect");
        }

        if (config.options.HasFlag(CompilerOption.WarnAsError))
        {
            argsArray.Add(DXC_ARG_WARNINGS_ARE_ERRORS);
        }

        return argsArray;
    }

    private static ShaderInputType ToInputType(D3D_SHADER_INPUT_TYPE type)
    {
        return type switch
        {
            D3D_SHADER_INPUT_TYPE.D3D_SIT_CBUFFER => ShaderInputType.ConstantBuffer,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_TBUFFER => ShaderInputType.Texture,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_TEXTURE => ShaderInputType.Texture,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_SAMPLER => ShaderInputType.Sampler,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_UAV_RWTYPED => ShaderInputType.UAV,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_STRUCTURED => ShaderInputType.StructuredBuffer,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_BYTEADDRESS => ShaderInputType.ByteAddressBuffer,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_UAV_RWSTRUCTURED => ShaderInputType.RWStructuredBuffer,
            D3D_SHADER_INPUT_TYPE.D3D_SIT_UAV_RWBYTEADDRESS => ShaderInputType.RWByteAddressBuffer,
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Unsupported shader input type")
        };
    }
}

internal unsafe partial class D3D12ShaderCompiler : IShaderCompiler
{
    private ComPtr<IDxcCompiler3> _compiler;
    private ComPtr<IDxcUtils> _utils;

    public D3D12ShaderCompiler()
    {
        // Initialize DXC _compiler.Get() and _utils.Get()
        var dxccID = CLSID.CLSID_DxcCompiler;
        var dxcuID = CLSID.CLSID_DxcUtils;

        IDxcCompiler3* pCompiler = default;
        IDxcUtils* pUtils = default;
        ThrowIfFailed(DxcCreateInstance(&dxccID, __uuidof(pCompiler), (void**)&pCompiler));
        ThrowIfFailed(DxcCreateInstance(&dxcuID, __uuidof(pUtils), (void**)&pUtils));

        _compiler.Attach(pCompiler);
        _utils.Attach(pUtils);
    }

    public Result<CompileResult> Compile(ref readonly CompilerConfig config, Allocator allocator, void** ppReflection)
    {
        // NOTE: Should we cache the _compiler.Get() and _utils.Get() instances for better performance?
        IDxcIncludeHandler* pIncludeHandler = default;

        try
        {
            // Create DXC _compiler.Get() and _utils.Get()
            var dxccID = CLSID.CLSID_DxcCompiler;
            var dxcuID = CLSID.CLSID_DxcUtils;


            ThrowIfFailed(_utils.Get()->CreateDefaultIncludeHandler(&pIncludeHandler));

            // Create source blob
            using ComPtr<IDxcBlobEncoding> sourceBlob = default;
            fixed (char* pPath = config.shaderPath)
            {
                if (_utils.Get()->LoadFile(pPath, null, sourceBlob.GetAddressOf()).FAILED)
                {
                    return Result.Fail($"Failed to load shader file: {config.shaderPath}");
                }
            }

            var argsArray = GetCompilerArguments(in config);
            var argPtrs = stackalloc char*[argsArray.Count];
            for (var i = 0; i < argsArray.Count; i++)
            {
                argPtrs[i] = (char*)Marshal.StringToHGlobalUni(argsArray[i]);
            }

            IDxcResult* pResult = default;

            try
            {
                // Compile shader
                var buffer = new DxcBuffer
                {
                    Ptr = sourceBlob.Get()->GetBufferPointer(),
                    Size = sourceBlob.Get()->GetBufferSize(),
                    Encoding = DXC_CP_UTF8
                };

                ThrowIfFailed(_compiler.Get()->Compile(&buffer, argPtrs, (uint)argsArray.Count, pIncludeHandler, __uuidof(pResult), (void**)&pResult));

                // Check compilation pResult
                HRESULT hrStatus;
                pResult->GetStatus(&hrStatus);
                if (hrStatus.FAILED)
                {
                    // Get error messages
                    using ComPtr<IDxcBlobEncoding> errorBlob = default;
                    pResult->GetErrorBuffer(errorBlob.GetAddressOf());

                    if (errorBlob.Get() != null)
                    {
                        var errorMessage = Marshal.PtrToStringUTF8((IntPtr)errorBlob.Get()->GetBufferPointer());
                        return Result.Fail($"DXC shader compilation failed:\n{errorMessage}");
                    }
                    else
                    {
                        return Result.Fail("DXC shader compilation failed with unknown error.");
                    }
                }

                // Get compiled bytecode
                using ComPtr<IDxcBlob> bytecodeBlob = default;
                ThrowIfFailed(pResult->GetResult(bytecodeBlob.GetAddressOf()));

                // Get pReflection data using DXC API
                if (ppReflection != null)
                {
                    ThrowIfFailed(pResult->GetOutput(DXC_OUT_KIND.DXC_OUT_REFLECTION, __uuidof<IDxcBlob>(), ppReflection, null));
                }

                var bytecodeSize = bytecodeBlob.Get()->GetBufferSize();
                var bytecode = new UnsafeArray<byte>((int)bytecodeSize, allocator);

                NativeMemory.Copy(bytecodeBlob.Get()->GetBufferPointer(), bytecode.GetUnsafePtr(), bytecodeSize);

                return new CompileResult
                {
                    bytecode = bytecode,
                };
            }
            finally
            {
                for (var i = 0; i < argsArray.Count; i++)
                {
                    Marshal.FreeHGlobal((nint)argPtrs[i]);
                }

                pResult->Release();
            }
        }
        finally
        {
            pIncludeHandler->Release();
        }
    }

    // TODO: Since we are using fixed root signature layout, the pReflection pass should only validate the layout, not generate it.
    // TODO: Ideally this should return a structured pReflection data instead of populating raw lists/dictionaries.
    public Result<ShaderReflectionData> PerformDXCReflection<T>(T* pReflectionBlob)
        where T : unmanaged
    {
        if (typeof(T) != typeof(IDxcBlob))
        {
            return Result<ShaderReflectionData>.Fail("Unsupported reflection type. Only IDxcBlob is supported.");
        }

        ID3D12ShaderReflection* pReflection = default;
        IDxcBlob* pDxcReflectionBlob = (IDxcBlob*)pReflectionBlob;

        try
        {
            // Create DXC _utils.Get() to parse pReflection data
            var dxcuID = CLSID.CLSID_DxcUtils;

            // Create pReflection interface from blob
            var reflectionBuffer = new DxcBuffer
            {
                Ptr = pDxcReflectionBlob->GetBufferPointer(),
                Size = pDxcReflectionBlob->GetBufferSize(),
                Encoding = DXC_CP_ACP
            };

            ThrowIfFailed(_utils.Get()->CreateReflection(&reflectionBuffer, __uuidof(pReflection), (void**)&pReflection));

            D3D12_SHADER_DESC shaderDesc;
            ThrowIfFailed(pReflection->GetDesc(&shaderDesc));

            var reflectionData = new ShaderReflectionData();

            for (uint i = 0; i < shaderDesc.BoundResources; i++)
            {
                D3D12_SHADER_INPUT_BIND_DESC bindDesc;
                ThrowIfFailed(pReflection->GetResourceBindingDesc(i, &bindDesc));

                var resourceName = Marshal.PtrToStringUTF8((IntPtr)bindDesc.Name);
                if (resourceName == null)
                {
                    return Result.Fail("Failed to get resource name from reflection data.");
                }

                var info = new ResourceBindingInfo
                {
                    Name = resourceName,
                    Type = ToInputType(bindDesc.Type),
                    BindPoint = bindDesc.BindPoint,
                    BindCount = bindDesc.BindCount,
                    Space = bindDesc.Space
                };

                switch (bindDesc.Type)
                {
                    case D3D_SHADER_INPUT_TYPE.D3D_SIT_CBUFFER:
                    {
                        var cbuffer = pReflection->GetConstantBufferByName(bindDesc.Name);
                        D3D12_SHADER_BUFFER_DESC cbufferDesc;
                        ThrowIfFailed(cbuffer->GetDesc(&cbufferDesc));

                        var variables = new List<CBufferPropertyInfo>((int)cbufferDesc.Variables);

                        // Now we iterate all variables for *every* cbuffer, not just b3
                        for (uint j = 0; j < cbufferDesc.Variables; j++)
                        {
                            var variable = cbuffer->GetVariableByIndex(j);
                            D3D12_SHADER_VARIABLE_DESC varDesc;
                            variable->GetDesc(&varDesc);

                            var variableName = Marshal.PtrToStringUTF8((IntPtr)varDesc.Name);
                            if (variableName == null)
                            {
                                continue;
                            }

                            variables.Add(new CBufferPropertyInfo
                            {
                                Name = variableName,
                                StartOffset = varDesc.StartOffset,
                                Size = varDesc.Size
                            });
                        }

                        info.Size = cbufferDesc.Size;
                        info.Properties = variables;

                        break;
                    }

                    // NOTE: Currently we do not support resource bindings yet, everything access through bindless heaps.
                }

                reflectionData.ResourcesBindings.Add(info);
            }

            return reflectionData;
        }
        finally
        {
            pReflection->Release();
        }
    }
}
