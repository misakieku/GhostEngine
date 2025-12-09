using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Core;

public struct ShaderPass : IResourceReleasable
{
    public ShaderPassKey Identifier
    {
        get; init;
    }

    public ZTestOptions ZTest
    {
        get; set;
    }

    public ZWriteOptions ZWrite
    {
        get; set;
    }

    public CullOptions Cull
    {
        get; set;
    }

    public BlendOptions Blend
    {
        get; set;
    }

    public uint ColorMask
    {
        get; set;
    }

    // TODO: Shader variant.

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
    }
}

/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public class Shader : IResourceReleasable, IIdentifierType
{
    private readonly uint _cbufferSize;
    private UnsafeArray<ShaderPass> _passes;
    // TODO: Optmize lookups with a better data structure if needed
    private readonly Dictionary<string, int> _passLookup; // pass name to index

    public int PassCount => _passes.Count;
    public uint CBufferSize => _cbufferSize;

    internal Shader(ShaderDescriptor descriptor)
    {
        _cbufferSize = descriptor.cbufferSize;
        _passes = new UnsafeArray<ShaderPass>(descriptor.passes.Count, Allocator.Persistent);
        _passLookup = new Dictionary<string, int>(descriptor.passes.Count);

        for (var i = 0; i < descriptor.passes.Count; i++)
        {
            var pass = descriptor.passes[i];

            // TODO: Handle inherited passes
            if (pass is not FullPassDescriptor fullPass)
            {
                continue;
            }

            var passKey = new ShaderPassKey(pass.Identifier);

            _passes[i] = new ShaderPass
            {
                Identifier = passKey,
                ZTest = fullPass.localPipeline.zTest,
                ZWrite = fullPass.localPipeline.zWrite,
                Cull = fullPass.localPipeline.cull,
                Blend = fullPass.localPipeline.blend,
                ColorMask = fullPass.localPipeline.colorMask
            };

            _passLookup[pass.Name] = i;
        }
    }

    public int GetPassIndex(string passName)
    {
        return _passLookup.GetValueOrDefault(passName, -1);
    }

    public ref ShaderPass GetPassReference(int index)
    {
        return ref _passes[index];
    }

    public RefResult<ShaderPass, ErrorStatus> TryGetPassKey(string passName, out int passIndex)
    {
        var index = _passLookup.GetValueOrDefault(passName, -1);
        if (index == -1)
        {
            passIndex = -1;
            return ErrorStatus.NotFound;
        }

        passIndex = index;
        return RefResult<ShaderPass, ErrorStatus>.Success(ref _passes[index]);
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        _passes.Dispose();
    }
}
