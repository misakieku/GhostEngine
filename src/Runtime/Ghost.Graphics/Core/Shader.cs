using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public struct ShaderProperty;

public partial struct Shader
{
    private static readonly Dictionary<string, int> s_passNameToID = new Dictionary<string, int>();
    private static int s_nextPassID = 0;

    private static readonly Dictionary<string, int> s_propertyNameToID = new Dictionary<string, int>();
    private static int s_nextPropertyID = 0;

    private static readonly Dictionary<string, int> s_keywordNameToID = new Dictionary<string, int>();
    private static readonly Dictionary<int, string> s_keywordIDToName = new Dictionary<int, string>();
    private static int s_nextKeywordID = 0;

    public static Identifier<ShaderPass> GetPassID(string passName)
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(s_passNameToID, passName, out var exists);
        if (!exists)
        {
            id = s_nextPassID++;
        }

        return id;
    }

    public static Identifier<ShaderProperty> GetPropertyID(string propertyName)
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(s_propertyNameToID, propertyName, out var exists);
        if (!exists)
        {
            id = s_nextPropertyID++;
        }

        return id;
    }

    public static int GetKeywordID(string keywordName)
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(s_keywordNameToID, keywordName, out var exists);
        if (!exists)
        {
            id = s_nextKeywordID++;
        }

        s_keywordIDToName[id] = keywordName;
        return id;
    }

    public static string? GetKeywordName(int keywordID)
    {
        if (s_keywordIDToName.TryGetValue(keywordID, out var name))
        {
            return name;
        }

        return null;
    }

    // TODO: Global keywords
}

/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public partial struct Shader : IResourceReleasable
{
    private readonly uint _propertyBufferSize;
    private UnsafeArray<ShaderPass> _shaderPasses;
    private UnsafeHashMap<int, int> _passIDToLocal;
    private UnsafeHashMap<int, int> _keywordIDToLocal;

    // TODO: Tag to pass index for fast lookup.
    // We can use a int array since the number and index of tags are fixed at compile time.

    public readonly int PassCount => _shaderPasses.Count;
    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal Shader(GraphicsShaderDescriptor descriptor, ReadOnlySpan<GraphicsCompiledResult> compiledResults)
    {
        Debug.Assert(descriptor.passes.Length == compiledResults.Length);

        _propertyBufferSize = descriptor.propertyBufferSize;
        _shaderPasses = new UnsafeArray<ShaderPass>(descriptor.passes.Length, Allocator.Persistent);
        _passIDToLocal = new UnsafeHashMap<int, int>(descriptor.passes.Length, Allocator.Persistent);
        _keywordIDToLocal = new UnsafeHashMap<int, int>(32, Allocator.Persistent);

        for (var i = 0; i < descriptor.passes.Length; i++)
        {
            ref readonly var pass = ref descriptor.passes[i];

            var passKey = RHIUtility.CreateShaderPassKey(pass.identifier, compiledResults[i].HashCode);
            var keywords = default(LocalKeywordSet);

            if (pass.keywords.Length > 0)
            {
                var localKeywordIndex = 0;

                for (var j = 0; j < pass.keywords.Length; j++)
                {
                    var group = pass.keywords[j];
                    if (group.keywords == null)
                    {
                        continue;
                    }

                    if (group.space == KeywordSpace.Local)
                    {
                        foreach (var kw in group.keywords)
                        {
                            var kwID = GetKeywordID(kw);
                            var idx = localKeywordIndex++;

                            keywords.SetKeyword(idx, true);
                            _keywordIDToLocal.TryAdd(kwID, idx);
                        }
                    }

                    // TODO: Global keywords
                }
            }

            _shaderPasses[i] = new ShaderPass
            {
                Key = passKey,
                DefaultState = pass.localPipeline,
                KeywordIDs = keywords,
            };

            _passIDToLocal[GetPassID(pass.name)] = (ushort)i;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetLocalKeywordIndex(int globalKeywordID)
    {
        if (_keywordIDToLocal.TryGetValue(globalKeywordID, out var localIndex))
        {
            return localIndex;
        }

        return -1;
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
    public ref ShaderPass GetPassReference(int index)
    {
        return ref _shaderPasses[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<ShaderPass, Error> TryGetPass(Identifier<ShaderPass> passID, out int passIndex)
    {
        if (_passIDToLocal.TryGetValue(passID.Value, out var index))
        {
            passIndex = -1;
            return Error.NotFound;
        }

        passIndex = index;
        return _shaderPasses[index];
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _keywordIDToLocal.Dispose();
        _shaderPasses.Dispose();
        _passIDToLocal.Dispose();
    }
}

public unsafe partial struct ComputeShader : IResourceReleasable
{
    private fixed ulong _entryHash[8];
    private readonly int _entryPointCount;
    private readonly uint _propertyBufferSize;

    private LocalKeywordSet _localKeywordSet;
    private UnsafeHashMap<int, int> _keywordIDToLocal;

    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal ComputeShader(ComputeShaderDescriptor descriptor, ReadOnlySpan<ShaderCompileResult> compiledResults)
    {
        Debug.Assert(descriptor.shaderCodes.Length == compiledResults.Length);

        _propertyBufferSize = descriptor.propertyBufferSize;
        _entryPointCount = descriptor.shaderCodes.Length;

        _keywordIDToLocal = new UnsafeHashMap<int, int>(32, Allocator.Persistent);

        for (var i = 0; i < descriptor.shaderCodes.Length; i++)
        {
            _entryHash[i] = Hash.Combine64(descriptor.identifier, compiledResults[i].hashCode);
        }

        var localKeywordIndex = 0;
        for (var i = 0; i < descriptor.keywords.Length; i++)
        {
            var group = descriptor.keywords[i];
            if (group.keywords == null)
            {
                continue;
            }

            if (group.space == KeywordSpace.Local)
            {
                foreach (var kw in group.keywords)
                {
                    var kwID = Shader.GetKeywordID(kw);
                    var idx = localKeywordIndex++;

                    _localKeywordSet.SetKeyword(idx, true);
                    _keywordIDToLocal.TryAdd(kwID, idx);
                }
            }
        }
    }

    public ulong GetEntryHash(int entryPointIndex)
    {
        Debug.Assert(entryPointIndex >= 0 && entryPointIndex < _entryPointCount);
        return _entryHash[entryPointIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetLocalKeywordIndex(int globalKeywordID)
    {
        if (_keywordIDToLocal.TryGetValue(globalKeywordID, out var localIndex))
        {
            return localIndex;
        }

        return -1;
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _keywordIDToLocal.Dispose();
    }
}