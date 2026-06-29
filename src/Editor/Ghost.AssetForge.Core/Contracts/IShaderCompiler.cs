using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.AssetForge.Core.Contracts;

internal interface IShaderCompiler : IDisposable
{
    Result<UnsafeArray<byte>> Compile(ref readonly ShaderCompilationConfig config, AllocationHandle handle);
}
