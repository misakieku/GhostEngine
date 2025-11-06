using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal unsafe struct CompileResult : IDisposable
{
    public UnsafeArray<byte> bytecode;

    public readonly bool IsCreated => bytecode.IsCreated;

    public void Dispose()
    {
        bytecode.Dispose();
    }
}

internal readonly struct CBufferVariableInfo
{
    public string Name
    {
        get; init;
    }

    public uint StartOffset
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
    public string Name
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }

    public uint RegisterSpace
    {
        get; init;
    }

    public uint SizeInBytes
    {
        get; init;
    }

    public IReadOnlyList<CBufferVariableInfo> Variables
    {
        get; init;
    }
}

internal readonly struct ResourceBindingInfo
{
    public string Name
    {
        get; init;
    }

    public D3D_SHADER_INPUT_TYPE Type
    {
        get; init;
    }

    public uint BindPoint
    {
        get; init;
    }

    public uint BindCount
    {
        get; init;
    }

    public uint Space
    {
        get; init;
    }
}

internal readonly struct ShaderReflectionData
{
    public List<CBufferInfo> ConstantBuffers
    {
        get;
    }

    public List<ResourceBindingInfo> OtherResources
    {
        get;
    }

    // public List<ResourceBindingInfo> Samplers { get; } = new();
    // public List<ResourceBindingInfo> ShaderResourceViews { get; } = new();
    // public List<ResourceBindingInfo> UnorderedAccessViews { get; } = new();

    public ShaderReflectionData()
    {
        ConstantBuffers = new List<CBufferInfo>();
        OtherResources = new List<ResourceBindingInfo>();
    }
}

internal static unsafe class D3D12ShaderCompiler
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
            CompilerOptimizeLevel.O0 => "-O0",
            CompilerOptimizeLevel.O1 => "-O1",
            CompilerOptimizeLevel.O2 => "-O2",
            CompilerOptimizeLevel.O3 => "-O3",
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

        foreach (var include in config.includes)
        {
            argsArray.Add("-I");
            argsArray.Add(include);
        }

        foreach (var define in config.defines)
        {
            argsArray.Add("-D");
            argsArray.Add(define);
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

        return argsArray;
    }

    public static Result<CompileResult> Compile(ref readonly CompilerConfig config, Allocator allocator, IDxcBlob** ppReflectionBlob)
    {
        // NOTE: Should we cache the compiler and utils instances for better performance?
        using ComPtr<IDxcCompiler3> compiler = default;
        using ComPtr<IDxcUtils> utils = default;
        using ComPtr<IDxcIncludeHandler> includeHandler = default;

        // Create DXC compiler and utils
        var dxccID = CLSID.CLSID_DxcCompiler;
        var dxcuID = CLSID.CLSID_DxcUtils;

        ThrowIfFailed(DxcCreateInstance(&dxccID, compiler.IID(), compiler.PPV()));
        ThrowIfFailed(DxcCreateInstance(&dxcuID, utils.IID(), utils.PPV()));

        //includeHandler.Get()->LoadSource();
        utils.Get()->CreateDefaultIncludeHandler(includeHandler.GetAddressOf());

        // Create source blob
        using ComPtr<IDxcBlobEncoding> sourceBlob = default;
        if (utils.Get()->LoadFile(config.shaderPath.AsSpan().GetUnsafePtr(), null, sourceBlob.GetAddressOf()).FAILED)
        {
            return Result.Fail($"Failed to load shader file: {config.shaderPath}");
        }

        var argsArray = GetCompilerArguments(in config);
        var argPtrs = stackalloc char*[argsArray.Count];
        for (var i = 0; i < argsArray.Count; i++)
        {
            argPtrs[i] = (char*)Marshal.StringToHGlobalUni(argsArray[i]);
        }

        try
        {
            // Compile shader
            using ComPtr<IDxcResult> result = default;
            var buffer = new DxcBuffer
            {
                Ptr = sourceBlob.Get()->GetBufferPointer(),
                Size = sourceBlob.Get()->GetBufferSize(),
                Encoding = DXC.DXC_CP_UTF8
            };

            ThrowIfFailed(compiler.Get()->Compile(&buffer, argPtrs, (uint)argsArray.Count, includeHandler.Get(), result.IID(), result.PPV()));

            // Check compilation result
            HRESULT hrStatus;
            result.Get()->GetStatus(&hrStatus);
            if (hrStatus.FAILED)
            {
                // Get error messages
                using ComPtr<IDxcBlobEncoding> errorBlob = default;
                result.Get()->GetErrorBuffer(errorBlob.GetAddressOf());

                if (errorBlob.Get() != null)
                {
                    var errorMessage = Marshal.PtrToStringUni((IntPtr)errorBlob.Get()->GetBufferPointer());
                    return Result.Fail($"DXC shader compilation failed:\n{errorMessage}");
                }
                else
                {
                    return Result.Fail("DXC shader compilation failed with unknown error.");
                }
            }

            // Get compiled bytecode
            using ComPtr<IDxcBlob> bytecodeBlob = default;
            ThrowIfFailed(result.Get()->GetResult(bytecodeBlob.GetAddressOf()));

            // Get reflection data using DXC API
            if (ppReflectionBlob != null)
            {
                ThrowIfFailed(result.Get()->GetOutput(DXC_OUT_KIND.DXC_OUT_REFLECTION, __uuidof<IDxcBlob>(), (void**)ppReflectionBlob, null));
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
        }
    }

    // TODO: Since we are using fixed root signature layout, the reflection pass should only validate the layout, not generate it.
    // TODO: Ideally this should return a structured reflection data instead of populating raw lists/dictionaries.
    public static Result<ShaderReflectionData> PerformDXCReflection(IDxcBlob* reflectionBlob)
    {
        if (reflectionBlob == null)
        {
            return Result<ShaderReflectionData>.Fail("Reflection blob is null.");
        }

        // Create DXC utils to parse reflection data
        var dxcuID = CLSID.CLSID_DxcUtils;
        using ComPtr<IDxcUtils> utils = default;
        ThrowIfFailed(DxcCreateInstance(&dxcuID, utils.IID(), utils.PPV()));

        // Create reflection interface from blob
        var reflectionBuffer = new DxcBuffer
        {
            Ptr = reflectionBlob->GetBufferPointer(),
            Size = reflectionBlob->GetBufferSize(),
            Encoding = DXC.DXC_CP_ACP
        };

        using ComPtr<ID3D12ShaderReflection> reflection = default;
        ThrowIfFailed(utils.Get()->CreateReflection(&reflectionBuffer, reflection.IID(), reflection.PPV()));

        D3D12_SHADER_DESC shaderDesc;
        ThrowIfFailed(reflection.Get()->GetDesc(&shaderDesc));

        var reflectionData = new ShaderReflectionData();

        for (uint i = 0; i < shaderDesc.BoundResources; i++)
        {
            D3D12_SHADER_INPUT_BIND_DESC bindDesc;
            ThrowIfFailed(reflection.Get()->GetResourceBindingDesc(i, &bindDesc));

            var resourceName = Marshal.PtrToStringUTF8((IntPtr)bindDesc.Name);
            if (resourceName == null)
            {
                return Result.Fail("Failed to get resource name from reflection data.");
            }

            switch (bindDesc.Type)
            {
                case D3D_SHADER_INPUT_TYPE.D3D_SIT_CBUFFER:
                {
                    var cbuffer = reflection.Get()->GetConstantBufferByName(bindDesc.Name);
                    D3D12_SHADER_BUFFER_DESC cbufferDesc;
                    ThrowIfFailed(cbuffer->GetDesc(&cbufferDesc));

                    var variables = new List<CBufferVariableInfo>((int)cbufferDesc.Variables);

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

                        variables.Add(new CBufferVariableInfo
                        {
                            Name = variableName,
                            StartOffset = varDesc.StartOffset,
                            Size = varDesc.Size
                        });
                    }

                    reflectionData.ConstantBuffers.Add(new CBufferInfo
                    {
                        Name = resourceName,
                        RegisterSlot = bindDesc.BindPoint,
                        RegisterSpace = bindDesc.Space,
                        SizeInBytes = cbufferDesc.Size,
                        Variables = variables
                    });

                    break;
                }

                // NOTE: Currently we are not support resource bindings yet, everything access through bindless heaps.
                default:
                {
                    reflectionData.OtherResources.Add(new ResourceBindingInfo
                    {
                        Name = resourceName,
                        Type = bindDesc.Type,
                        BindPoint = bindDesc.BindPoint,
                        BindCount = bindDesc.BindCount,
                        Space = bindDesc.Space
                    });

                    break;
                }
            }
        }

        return reflectionData;
    }
}
