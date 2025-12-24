using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Core;

public readonly struct ShaderPass : IResourceReleasable
{
    public ShaderPassKey Identifier
    {
        get; init;
    }

    public PipelineState DeafaultState
    {
        get; init;
    }

    readonly void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
    }
}

public struct ShaderProperty;

public partial struct Shader
{
    private static readonly Dictionary<string, int> s_passNameToID = new Dictionary<string, int>();
    private static int s_nextPassID = 0;

    private static readonly Dictionary<string, int> s_propertyNameToID = new Dictionary<string, int>();
    private static int s_nextPropertyID = 0;

    public static Identifier<ShaderPass> GetPassID(string passName)
    {
        return new Identifier<ShaderPass>(s_passNameToID.GetValueOrDefault(passName, s_nextPassID++));
    }

    public static Identifier<ShaderProperty> GetPropertyID(string propertyName)
    {
        return new Identifier<ShaderProperty>(s_propertyNameToID.GetValueOrDefault(propertyName, s_nextPropertyID++));
    }
}

/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public partial struct Shader : IResourceReleasable, IIdentifierType
{
    private readonly uint _cbufferSize;
    private UnsafeArray<ShaderPass> _shaderPasses;
    private UnsafeHashMap<int, int> _passLookup; // pass id to index

    public readonly int PassCount => _shaderPasses.Count;
    public readonly uint CBufferSize => _cbufferSize;

    internal Shader(ShaderDescriptor descriptor)
    {
        _cbufferSize = descriptor.cbufferSize;
        _shaderPasses = new UnsafeArray<ShaderPass>(descriptor.passes.Count, Allocator.Persistent);
        _passLookup = new UnsafeHashMap<int, int>(descriptor.passes.Count, Allocator.Persistent);

        for (var i = 0; i < descriptor.passes.Count; i++)
        {
            var pass = descriptor.passes[i];

            // TODO: Handle inherited passes
            if (pass is not FullPassDescriptor fullPass)
            {
                continue;
            }

            var passKey = new ShaderPassKey(pass.Identifier);

            _shaderPasses[i] = new ShaderPass
            {
                Identifier = passKey,
                DeafaultState = fullPass.localPipeline
            };

            _passLookup[GetPassID(pass.Name)] = i;
        }
    }

    public readonly int GetPassIndex(Identifier<ShaderPass> passID)
    {
        if (_passLookup.TryGetValue(passID.Value, out var index))
        {
            return index;
        }

        return -1;
    }

    public readonly int GetPassIndex(string passName)
    {
        if (_passLookup.TryGetValue(GetPassID(passName), out var index))
        {
            return index;
        }

        return -1;
    }

    public readonly ShaderPass GetPass(int index)
    {
        return _shaderPasses[index];
    }

    public readonly Result<ShaderPass, ErrorStatus> TryGetPass(Identifier<ShaderPass> passID, out int passIndex)
    {
        if (_passLookup.TryGetValue(passID.Value, out var index))
        {
            passIndex = -1;
            return ErrorStatus.NotFound;
        }

        passIndex = index;
        return _shaderPasses[index];
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        _shaderPasses.Dispose();
        _passLookup.Dispose();
    }
}
