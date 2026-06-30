using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;

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
