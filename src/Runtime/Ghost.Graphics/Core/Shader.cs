using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public struct ShaderProperty;

public partial struct Shader
{
    private static readonly Dictionary<string, int> s_passNameToID = new Dictionary<string, int>();
    private static int s_nextPassID = 0;

    public static Identifier<ShaderPass> GetPassID(string passName)
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(s_passNameToID, passName, out var exists);
        if (!exists)
        {
            id = s_nextPassID++;
        }

        return id;
    }
}
/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public unsafe partial struct Shader : IResourceReleasable
{
    private readonly ulong _nameHash;
    private readonly uint _propertyBufferSize;
    private UnsafeArray<ShaderPass> _shaderPasses;
    private UnsafeHashMap<int, int> _passIDToLocal;
    private fixed sbyte _semanticPassMap[8];

    public readonly ulong UniqueID => _nameHash;
    public readonly int PassCount => _shaderPasses.Count;
    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal Shader(GraphicsShaderDescriptor descriptor)
    {
        _nameHash = RHIUtility.GetShaderID(descriptor.Name);
        _propertyBufferSize = descriptor.PropertyBufferSize;
        _shaderPasses = new UnsafeArray<ShaderPass>(descriptor.Passes.Length, AllocationHandle.Persistent);
        _passIDToLocal = new UnsafeHashMap<int, int>(descriptor.Passes.Length, AllocationHandle.Persistent);

        for (var s = 0; s < 8; s++)
        {
            _semanticPassMap[s] = -1;
        }

        for (var i = 0; i < descriptor.Passes.Length; i++)
        {
            ref readonly var pass = ref descriptor.Passes[i];

            _shaderPasses[i] = new ShaderPass
            {
                Key = RHIUtility.GetPassID(_nameHash, i),
                StageMask = pass.stageMask,
                DefaultState = pass.localPipeline,
            };

            _passIDToLocal[GetPassID(pass.name)] = (ushort)i;

            if ((uint)pass.semantic < (uint)PassSemantic.Count && pass.semantic != PassSemantic.Custom)
            {
                Logger.DebugAssert(_semanticPassMap[(byte)pass.semantic] < 0, $"Shader '{descriptor.Name}' contains more than one {pass.semantic} pass.");
                _semanticPassMap[(byte)pass.semantic] = (sbyte)i;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetPassIndex(PassSemantic semantic)
    {
        return _semanticPassMap[(byte)semantic];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetPassIndex(Identifier<ShaderPass> passID)
    {
        if (_passIDToLocal.TryGetValue(passID.Value, out var index))
        {
            return index;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetPassIndex(string passName)
    {
        if (_passIDToLocal.TryGetValue(GetPassID(passName), out var index))
        {
            return index;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ShaderPass GetPassReference(int index)
    {
        return ref _shaderPasses[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<ShaderPass, Error> TryGetPass(Identifier<ShaderPass> passID, out int passIndex)
    {
        if (!_passIDToLocal.TryGetValue(passID.Value, out var index))
        {
            passIndex = -1;
            return Error.NotFound;
        }

        passIndex = index;
        return _shaderPasses[index];
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _shaderPasses.Dispose();
        _passIDToLocal.Dispose();
    }
}

public unsafe partial struct ComputeShader : IResourceReleasable
{
    private readonly ulong _nameHash;
    private fixed ulong _entryHashes[8]; // Support up to 8 entry points for now, can be extended if needed.
    private readonly uint _propertyBufferSize;

    public readonly ulong UniqueID => _nameHash;
    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal ComputeShader(ComputeShaderDescriptor descriptor)
    {
        _nameHash = RHIUtility.GetShaderID(descriptor.Name);
        _propertyBufferSize = descriptor.PropertyBufferSize;

        for (var i = 0; i < descriptor.ShaderCodes.Length; i++)
        {
            _entryHashes[i] = RHIUtility.GetPassID(_nameHash, i);
        }
    }

    public ulong GetEntryID(int entryIndex)
    {
        Logger.DebugAssert(entryIndex >= 0 && entryIndex < 8, "Entry index out of bounds.");
        return _entryHashes[entryIndex];
    }

    public void ReleaseResource(IResourceDatabase database)
    {
    }
}
