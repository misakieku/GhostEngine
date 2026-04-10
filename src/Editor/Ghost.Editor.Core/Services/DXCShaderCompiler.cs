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
using Ghost.DXC;

using static Ghost.DXC.UUID;
using System.Runtime.CompilerServices;

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
            CompilerOptimizeLevel.O0 => "-O0",
            CompilerOptimizeLevel.O1 => "-O1",
            CompilerOptimizeLevel.O2 => "-O2",
            CompilerOptimizeLevel.O3 => "-O3",
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
            argsArray.Add("-WX");
        }

        if (config.options.HasFlag(CompilerOption.SpirvCrossCompile))
        {
            argsArray.Add("-spirv");
        }

        argsArray.Add("-rootsig-define");
        argsArray.Add("GLOBAL_BINDLESS_SIG");

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
}

internal sealed unsafe partial class DXCShaderCompiler : IShaderCompiler
{
    private UniquePtr<IDxcCompiler3> _compiler;
    private UniquePtr<IDxcUtils> _utils;

    // NOTE: This is just a temporary cache for compiled shader code. We will implement a proper disk cache later.
    private readonly Dictionary<Key64<ShaderCompileResult>, ShaderCompileResult> _compiledResults;

    private bool _disposed;

    public DXCShaderCompiler()
    {
        IDxcCompiler3* pCompiler = default;
        IDxcUtils* pUtils = default;
        var hr = Api.DxcCreateInstance((Guid*)Unsafe.AsPointer(in Api.CLSID_DxcCompiler), __uuidof(pCompiler), (void**)&pCompiler);
        if (hr < 0)
        {
            throw new InvalidOperationException($"Failed to create DXC compiler instance. HRESULT: 0x{hr:X8}");
        }

        hr = Api.DxcCreateInstance((Guid*)Unsafe.AsPointer(in Api.CLSID_DxcUtils), __uuidof(pUtils), (void**)&pUtils);
        if (hr < 0)
        {
            pCompiler->Release();
            throw new InvalidOperationException($"Failed to create DXC utils instance. HRESULT: 0x{hr:X8}");
        }

        _compiler.Attach(pCompiler);
        _utils.Attach(pUtils);

        _compiledResults = new Dictionary<Key64<ShaderCompileResult>, ShaderCompileResult>();
    }

    ~DXCShaderCompiler()
    {
        Dispose();
    }

    public Result<Key64<ShaderCompileResult>> Compile(ref readonly ShaderCompilationConfig config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IDxcIncludeHandler* includeHandler = default;
        IDxcBlobEncoding* sourceBlob = default;
        try
        {
            var hr = _utils.Get()->CreateDefaultIncludeHandler(&includeHandler);
            if (hr < 0)
            {
                return Result.Failure($"Failed to create default include handler. HRESULT: 0x{hr:X8}");
            }

            fixed (byte* pCode = Encoding.UTF8.GetBytes(config.shaderCode))
            {
                var sizeInBytes = Encoding.UTF8.GetByteCount(config.shaderCode);
                hr = _utils.Get()->CreateBlobFromPinned(pCode, (uint)sizeInBytes, Api.DXC_CP_UTF8, &sourceBlob);
                if (hr < 0)
                {
                    return Result.Failure($"Failed to create blob from shader code. HRESULT: 0x{hr:X8}");
                }
            }

            var argsArray = GetCompilerArguments(in config);
            var argPtrs = stackalloc char*[argsArray.Count];
            for (var i = 0; i < argsArray.Count; i++)
            {
                argPtrs[i] = (char*)Marshal.StringToHGlobalUni(argsArray[i]);
            }

            IDxcResult* result = default;
            IDxcBlob* bytecodeBlob = default;
            
            try
            {
                // Compile shader
                var buffer = new DxcBuffer
                {
                    Ptr = sourceBlob->GetBufferPointer(),
                    Size = sourceBlob->GetBufferSize(),
                    Encoding = Api.DXC_CP_UTF8
                };

                hr = _compiler.Get()->Compile(&buffer, argPtrs, (uint)argsArray.Count, includeHandler, __uuidof(result), (void**)&result);
                if (hr < 0)
                {
                    return Result.Failure($"Failed to compile shader. HRESULT: 0x{hr:X8}");
                }

                // Check compilation result
                int hrStatus;
                result->GetStatus(&hrStatus);
                if (hrStatus < 0)
                {
                    // Get error messages
                    IDxcBlobEncoding* pErrorBlob = default;
                    result->GetErrorBuffer(&pErrorBlob);

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
                hr = result->GetResult(&bytecodeBlob);
                if (hr < 0)
                {
                    return Result.Failure($"Failed to get compiled shader bytecode. HRESULT: 0x{hr:X8}");
                }

                var bytecodeSize = bytecodeBlob->GetBufferSize();
                var bytecode = new UnsafeArray<byte>((int)bytecodeSize, Allocator.Persistent);

                NativeMemory.Copy(bytecodeBlob->GetBufferPointer(), bytecode.GetUnsafePtr(), (nuint)bytecodeSize);

                var compileResult = new ShaderCompileResult
                {
                    bytecode = bytecode,
                    hashCode = XxHash64.HashToUInt64(bytecode)
                };

                _compiledResults[compileResult.hashCode] = compileResult;
                return new Key64<ShaderCompileResult>(compileResult.hashCode);
            }
            finally
            {
                if (result != null)
                {
                    result->Release();
                }

                if (bytecodeBlob != null)
                {
                    bytecodeBlob->Release();
                }

                for (var i = 0; i < argsArray.Count; i++)
                {
                    Marshal.FreeHGlobal((nint)argPtrs[i]);
                }
            }
        }
        finally
        {
            if (includeHandler != null)
            {
                includeHandler->Release();
            }

            if (sourceBlob != null)
            {
                sourceBlob->Release();
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

        Key64<ShaderCompileResult> tsResult = default;
        var asCode = descriptor.amplificationShaderCode;
        if (asCode.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                shaderCode = asCode.code,
                entryPoint = asCode.entryPoint,
                stage = ShaderStage.TaskShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            tsResult = result.Value;
        }

        Key64<ShaderCompileResult> msResult;
        var msCode = descriptor.meshShaderCode;
        if (msCode.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                shaderCode = msCode.code,
                entryPoint = msCode.entryPoint,
                stage = ShaderStage.MeshShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config);
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

        Key64<ShaderCompileResult> psResult;
        var psCode = descriptor.pixelShaderCode;
        if (psCode.IsCreated)
        {
            var config = new ShaderCompilationConfig
            {
                defines = fullDefines,
                shaderCode = psCode.code,
                entryPoint = psCode.entryPoint,
                stage = ShaderStage.PixelShader,
                model = additionalConfig.model,
                optimizeLevel = additionalConfig.optimizeLevel,
                options = additionalConfig.options,
            };

            var result = Compile(ref config);
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
            tsResultHash = tsResult,
            msResultHash = msResult,
            psResultHash = psResult,
        };

        return compiled;
    }

    public Result<ShaderCompileResult, Error> GetCompiledCache(Key64<ShaderCompileResult> key)
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

        _compiler.Get()->Release();
        _utils.Get()->Release();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
