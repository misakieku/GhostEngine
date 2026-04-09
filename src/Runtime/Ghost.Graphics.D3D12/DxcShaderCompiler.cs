using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Utilities;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.DXC;

namespace Ghost.Graphics.Core;

internal sealed partial class DXCShaderCompiler
{
    private static string GetProfileString(ShaderStage stage, ShaderModel version)
    {
        return (stage, version) switch
        {
            (ShaderStage.TaskShader, ShaderModel.SM_6_6) => "as_6_6",
            (ShaderStage.PixelShader, ShaderModel.SM_6_6) => "ps_6_6",
            (ShaderStage.MeshShader, ShaderModel.SM_6_6) => "ms_6_6",
            (ShaderStage.ComputeShader, ShaderModel.SM_6_6) => "cs_6_6",
            (ShaderStage.TaskShader, ShaderModel.SM_6_7) => "as_6_7",
            (ShaderStage.PixelShader, ShaderModel.SM_6_7) => "ps_6_7",
            (ShaderStage.MeshShader, ShaderModel.SM_6_7) => "ms_6_7",
            (ShaderStage.ComputeShader, ShaderModel.SM_6_7) => "cs_6_7",
            (ShaderStage.TaskShader, ShaderModel.SM_6_8) => "as_6_8",
            (ShaderStage.PixelShader, ShaderModel.SM_6_8) => "ps_6_8",
            (ShaderStage.MeshShader, ShaderModel.SM_6_8) => "ms_6_8",
            (ShaderStage.ComputeShader, ShaderModel.SM_6_8) => "cs_6_8",
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

    private static List<string> GetCompilerArguments(ref readonly ShaderCompilationConfig config)
    {
        var argsArray = new List<string>
        {
            "-T", GetProfileString(config.stage, config.model),   // Target profile (ms_6_6, ps_6_6)
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

        if (config.stage == ShaderStage.TaskShader
            || config.stage == ShaderStage.MeshShader
            || config.stage == ShaderStage.PixelShader)
        {
            argsArray.Add("-D");
            argsArray.Add("__GRAPHICS__");
        }
        else if (config.stage == ShaderStage.ComputeShader)
        {
            argsArray.Add("-D");
            argsArray.Add("__COMPUTE__");
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

        if (config.options.HasFlag(CompilerOption.SpirvCrossCompile))
        {
            argsArray.Add("-spirv");
        }

        return argsArray;
    }

    private static Result<string, Error> BuildFinalShaderCode(string shaderPath, ReadOnlySpan<string> includes, string? injectedCode)
    {
        string shaderCode;
        if (shaderPath == "hlsl_block")
        {
            if (string.IsNullOrEmpty(injectedCode))
            {
                return Error.InvalidArgument;
            }

            shaderCode = string.Empty;
        }
        else
        {
            if (!File.Exists(shaderPath))
            {
                return Error.NotFound;
            }

            shaderCode = File.ReadAllText(shaderPath);
        }

        var sb = new StringBuilder();
        foreach (var includePath in includes)
        {
            sb.AppendLine($"#include \"{includePath}\"");
        }

        if (!string.IsNullOrEmpty(injectedCode))
        {
            sb.AppendLine($"#line 0 \"injected_code\"");
            sb.AppendLine(injectedCode);
        }

        if (!string.IsNullOrEmpty(shaderCode))
        {
            sb.AppendLine($"#line 0 \"{shaderPath}\"");
            sb.AppendLine(shaderCode);
        }

        return sb.ToString();
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

internal sealed unsafe partial class DXCShaderCompiler : IShaderCompiler
{
    private UniquePtr<IDxcCompiler3> _compiler;
    private UniquePtr<IDxcUtils> _utils;
    // NOTE: This is just a temporary cache for compiled shader code. We will implement a proper disk cache later.
    // TODO: This should be shader variant specific cache instead of pass specific.
    private readonly Dictionary<ulong, GraphicsCompiledResult> _compiledResults;

    private bool _disposed;

    public DXCShaderCompiler()
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

        _compiledResults = new Dictionary<ulong, GraphicsCompiledResult>();
    }

    ~DXCShaderCompiler()
    {
        Dispose();
    }

    private Result<ShaderReflectionData> PerformDXCReflection(IDxcBlob* pReflectionBlob)
    {
        ID3D12ShaderReflection* pReflection = default;

        try
        {
            // Create DXC _utils.Get() to parse reflection data
            var dxcuID = CLSID.CLSID_DxcUtils;

            // Create reflection interface from blob
            var reflectionBuffer = new DxcBuffer
            {
                Ptr = pReflectionBlob->GetBufferPointer(),
                Size = pReflectionBlob->GetBufferSize(),
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
                    return Result.Failure("Failed to get resource name from reflection data.");
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

    public Result<ShaderCompileResult> Compile(ref readonly ShaderCompilationConfig config, Allocator allocator)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using ComPtr<IDxcIncludeHandler> includeHandler = default;
        using ComPtr<IDxcBlobEncoding> sourceBlob = default;

        ThrowIfFailed(_utils.Get()->CreateDefaultIncludeHandler(includeHandler.GetAddressOf()));

        var finalShaderCodeResult = BuildFinalShaderCode(config.shaderPath, config.includes, config.injectedCode);
        if (finalShaderCodeResult.IsFailure)
        {
            return Result.Failure(finalShaderCodeResult.Error);
        }

        var finalShaderCode = finalShaderCodeResult.Value;
        fixed (byte* pCode = System.Text.Encoding.UTF8.GetBytes(finalShaderCode))
        {
            var sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(finalShaderCode);
            ThrowIfFailed(_utils.Get()->CreateBlobFromPinned(pCode, (uint)sizeInBytes, DXC_CP_UTF8, sourceBlob.GetAddressOf()));
        }

        var argsArray = GetCompilerArguments(in config);
        var argPtrs = stackalloc char*[argsArray.Count];
        for (var i = 0; i < argsArray.Count; i++)
        {
            argPtrs[i] = (char*)Marshal.StringToHGlobalUni(argsArray[i]);
        }

        using ComPtr<IDxcResult> result = default;

        try
        {
            // Compile shader
            var buffer = new DxcBuffer
            {
                Ptr = sourceBlob.Get()->GetBufferPointer(),
                Size = sourceBlob.Get()->GetBufferSize(),
                Encoding = DXC_CP_UTF8
            };

            ThrowIfFailed(_compiler.Get()->Compile(&buffer, argPtrs, (uint)argsArray.Count, includeHandler, __uuidof(result.Get()), (void**)&result));

            // Check compilation result
            HRESULT hrStatus;
            result.Get()->GetStatus(&hrStatus);
            if (hrStatus.FAILED)
            {
                // Get error messages
                IDxcBlobEncoding* pErrorBlob = default;
                result.Get()->GetErrorBuffer(&pErrorBlob);

                if (pErrorBlob != null)
                {
                    var errorMessage = Marshal.PtrToStringUTF8((IntPtr)pErrorBlob->GetBufferPointer());
                    pErrorBlob->Release();

                    return Result.Failure($"DXC shader compilation failed:\n{errorMessage}");
                }
                else
                {
                    return Result.Failure("DXC shader compilation failed with unknown error.");
                }
            }

            // Get compiled bytecode
            using ComPtr<IDxcBlob> bytecodeBlob = default;
            ThrowIfFailed(result.Get()->GetResult(bytecodeBlob.GetAddressOf()));

            ShaderReflectionData reflectionData = default;
            if (config.options.HasFlag(CompilerOption.KeepReflections))
            {
                using ComPtr<IDxcBlob> reflection = default;
                if (result.Get()->GetOutput(DXC_OUT_KIND.DXC_OUT_REFLECTION, __uuidof(reflection.Get()), (void**)&reflection, null).SUCCEEDED)
                {
                    reflectionData = PerformDXCReflection(reflection).GetValueOrDefault();
                }
            }

            var bytecodeSize = bytecodeBlob.Get()->GetBufferSize();
            var bytecode = new UnsafeArray<byte>((int)bytecodeSize, allocator);

            NativeMemory.Copy(bytecodeBlob.Get()->GetBufferPointer(), bytecode.GetUnsafePtr(), bytecodeSize);

            return new ShaderCompileResult
            {
                bytecode = bytecode,
                reflectionData = reflectionData,
                hashCode = XxHash64.HashToUInt64(bytecode)
            };
        }
        finally
        {
            for (var i = 0; i < argsArray.Count; i++)
            {
                Marshal.FreeHGlobal((nint)argPtrs[i]);
            }
        }
    }

    public Result<GraphicsCompiledResult> CompilePass(ref readonly PassDescriptor descriptor, ref readonly ShaderCompilationConfig additionalConfig, ref readonly LocalKeywordSet keywords)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string[] fullDefines;
        var totalDefineCount = descriptor.defines.Length + additionalConfig.defines.Length;
        if (totalDefineCount == 0)
        {
            fullDefines = Array.Empty<string>();
        }
        else
        {
            fullDefines = new string[totalDefineCount];
            descriptor.defines.CopyTo(fullDefines);
            additionalConfig.defines.CopyTo(fullDefines.AsSpan(descriptor.defines.Length));
        }

        var injectedCodeBuilder = new StringBuilder();
        injectedCodeBuilder.AppendLine(descriptor.shader.propertiesCode);
        injectedCodeBuilder.AppendLine(descriptor.hlsl);
        injectedCodeBuilder.AppendLine(additionalConfig.injectedCode);

        var injectedCode = injectedCodeBuilder.ToString();

        ShaderCompileResult tsResult = default;
        var tsEntry = descriptor.taskShader;
        if (tsEntry.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                includes = descriptor.includes,
                shaderPath = tsEntry.shader,
                entryPoint = tsEntry.entry,
                injectedCode = injectedCode,
                stage = ShaderStage.TaskShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config, Allocator.Persistent);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            tsResult = result.Value;
        }

        ShaderCompileResult msResult;
        var msEntry = descriptor.meshShader;
        if (msEntry.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                includes = descriptor.includes,
                shaderPath = msEntry.shader,
                entryPoint = msEntry.entry,
                injectedCode = injectedCode,
                stage = ShaderStage.MeshShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config, Allocator.Persistent);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            msResult = result.Value;
        }
        else
        {
            return Result.Failure("Mesh shader expected.");
        }

        ShaderCompileResult psResult;
        var psEntry = descriptor.pixelShader;
        if (psEntry.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                includes = descriptor.includes,
                shaderPath = psEntry.shader,
                entryPoint = psEntry.entry,
                injectedCode = injectedCode,
                stage = ShaderStage.PixelShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config, Allocator.Persistent);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            psResult = result.Value;
        }
        else
        {
            return Result.Failure("Pixel shader expected.");
        }

        var compiled = new GraphicsCompiledResult
        {
            tsResult = tsResult,
            msResult = msResult,
            psResult = psResult,
        };

        var passHash = RHIUtility.CreateShaderPassKey(descriptor.identifier, compiled.HashCode);
        var variantHash = RHIUtility.CreateShaderVariantKey(passHash, in keywords);

        _compiledResults[variantHash] = compiled;
        return compiled;
    }

    public Result<GraphicsCompiledResult, Error> LoadCompiledCache(Key64<ShaderVariant> key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_compiledResults.TryGetValue(key, out var compiledResult))
        {
            return compiledResult;
        }

        return Error.NotFound;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var kvp in _compiledResults)
        {
            kvp.Value.Dispose();
        }

        _compiler.Dispose();
        _utils.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
