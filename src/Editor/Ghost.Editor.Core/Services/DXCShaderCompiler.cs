using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DXC;
using Ghost.Editor.Core.Contracts;
using Ghost.Graphics.D3D12.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Ghost.DXC.UUID;

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
            (ShaderStage.Library, ShaderModel.SM_6_6) => "lib_6_6",
            (ShaderStage.TaskShader, ShaderModel.SM_6_7) => "as_6_7",
            (ShaderStage.PixelShader, ShaderModel.SM_6_7) => "ps_6_7",
            (ShaderStage.MeshShader, ShaderModel.SM_6_7) => "ms_6_7",
            (ShaderStage.ComputeShader, ShaderModel.SM_6_7) => "cs_6_7",
            (ShaderStage.Library, ShaderModel.SM_6_7) => "lib_6_7",
            (ShaderStage.TaskShader, ShaderModel.SM_6_8) => "as_6_8",
            (ShaderStage.PixelShader, ShaderModel.SM_6_8) => "ps_6_8",
            (ShaderStage.MeshShader, ShaderModel.SM_6_8) => "ms_6_8",
            (ShaderStage.ComputeShader, ShaderModel.SM_6_8) => "cs_6_8",
            (ShaderStage.Library, ShaderModel.SM_6_8) => "lib_6_8",
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
}

internal sealed unsafe partial class DXCShaderCompiler : IShaderCompiler
{
    private UniquePtr<IDxcCompiler3> _compiler;
    private UniquePtr<IDxcUtils> _utils;

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
    }

    ~DXCShaderCompiler()
    {
        Dispose();
    }

    public Result<UnsafeArray<byte>> Compile(ref readonly ShaderCompilationConfig config, AllocationHandle allocationHandle)
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
                var bytecode = new UnsafeArray<byte>((int)bytecodeSize, allocationHandle);

                NativeMemory.Copy(bytecodeBlob->GetBufferPointer(), bytecode.GetUnsafePtr(), (nuint)bytecodeSize);

                return bytecode;
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _compiler.Get()->Release();
        _utils.Get()->Release();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
