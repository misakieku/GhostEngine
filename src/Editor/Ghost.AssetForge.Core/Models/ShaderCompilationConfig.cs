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
    public IEnumerable<string> defines;
    public string shaderCode;
    public string entryPoint;
    public ShaderStage stage;
    public ShaderModel shaderModel;
    public IReadOnlyList<string>? includeDirectories;
    public CompilerOptimizeLevel optimizeLevel;
    public CompilerOption options;

    public readonly ulong ComputeHash()
    {
        var hash = (ulong)stage ^ ((ulong)shaderModel << 8) ^ ((ulong)optimizeLevel << 16) ^ ((ulong)options << 24);
        if (!string.IsNullOrEmpty(entryPoint))
        {
            hash = Hash.Combine64(hash, XxHash64.HashToUInt64(MemoryMarshal.AsBytes(entryPoint.AsSpan())));
        }

        if (!string.IsNullOrEmpty(shaderCode))
        {
            hash = Hash.Combine64(hash, XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderCode.AsSpan())));
        }

        if (defines != null)
        {
            foreach (var define in defines)
            {
                if (!string.IsNullOrEmpty(define))
                {
                    hash = Hash.Combine64(hash, XxHash64.HashToUInt64(MemoryMarshal.AsBytes(define.AsSpan())));
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
