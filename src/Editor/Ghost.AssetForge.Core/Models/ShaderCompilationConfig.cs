using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Models;

public unsafe struct ComputeCompileResult
{
    public fixed ulong resultHash[8];
    public readonly int count;

    public ulong HashCode
    {
        get
        {
            var a = Hash.Combine64(resultHash[0], resultHash[1], resultHash[2], resultHash[3]);
            var b = Hash.Combine64(resultHash[4], resultHash[5], resultHash[6], resultHash[7]);
            return Hash.Combine64(a, b);
        }
    }
}

public struct ShaderCompilationConfig
{
    public string[] defines;
    public string shaderCode;
    public string entryPoint;
    public ShaderStage stage;
    public ShaderModel model;
    public IReadOnlyList<string>? includeDirectories;
    public CompilerOptimizeLevel optimizeLevel;
    public CompilerOption options;

    public readonly ulong ComputeHash()
    {
        var hash = (ulong)stage ^ ((ulong)model << 8) ^ ((ulong)optimizeLevel << 16) ^ ((ulong)options << 24);
        if (!string.IsNullOrEmpty(entryPoint))
        {
            hash = Hash.Combine64(hash, System.IO.Hashing.XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(entryPoint.AsSpan())));
        }

        if (!string.IsNullOrEmpty(shaderCode))
        {
            hash = Hash.Combine64(hash, System.IO.Hashing.XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(shaderCode.AsSpan())));
        }

        if (defines != null)
        {
            for (var i = 0; i < defines.Length; i++)
            {
                if (!string.IsNullOrEmpty(defines[i]))
                {
                    hash = Hash.Combine64(hash, System.IO.Hashing.XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(defines[i].AsSpan())));
                }
            }
        }

        return hash;
    }
}

public enum CompilerOptimizeLevel
{
    O0,
    O1,
    O2,
    O3
}

[Flags]
public enum CompilerOption
{
    None = 0,
    KeepDebugInfo = 1 << 0,
    KeepReflections = 1 << 1,
    WarnAsError = 1 << 2,
    SpirvCrossCompile = 1 << 3
}
