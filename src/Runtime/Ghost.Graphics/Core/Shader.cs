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
    private readonly ulong _nameHash;
    private readonly uint _propertyBufferSize;
    private UnsafeArray<ShaderPass> _shaderPasses;
    private UnsafeHashMap<int, int> _passIDToLocal;
    private UnsafeHashMap<int, int> _keywordIDToLocal;

    // TODO: Tag to pass index for fast lookup.
    // We can use a int array since the number and index of tags are fixed at compile time.

    public readonly ulong UniqueID => _nameHash;
    public readonly int PassCount => _shaderPasses.Count;
    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal Shader(GraphicsShaderDescriptor descriptor)
    {
        _nameHash = RHIUtility.GetShaderID(descriptor.name);
        _propertyBufferSize = descriptor.propertyBufferSize;
        _shaderPasses = new UnsafeArray<ShaderPass>(descriptor.passes.Length, AllocationHandle.Persistent);
        _passIDToLocal = new UnsafeHashMap<int, int>(descriptor.passes.Length, AllocationHandle.Persistent);
        _keywordIDToLocal = new UnsafeHashMap<int, int>(32, AllocationHandle.Persistent);

        for (var i = 0; i < descriptor.passes.Length; i++)
        {
            ref readonly var pass = ref descriptor.passes[i];

            var keywords = new LocalKeywordSet();

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
                Key = RHIUtility.GetPassID(_nameHash, i),
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
    public ref readonly ShaderPass GetPassReference(int index)
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
    private readonly ulong _nameHash;
    private fixed ulong entryHashes[8]; // Support up to 8 entry points for now, can be extended if needed.
    private readonly uint _propertyBufferSize;

    private LocalKeywordSet _localKeywordSet;
    private UnsafeHashMap<int, int> _keywordIDToLocal;

    public readonly ulong UniqueID => _nameHash;
    public readonly uint PropertyBufferSize => _propertyBufferSize;

    internal ComputeShader(ComputeShaderDescriptor descriptor)
    {
        _nameHash = RHIUtility.GetShaderID(descriptor.name);
        _propertyBufferSize = descriptor.propertyBufferSize;

        _keywordIDToLocal = new UnsafeHashMap<int, int>(32, AllocationHandle.Persistent);

        for (var i = 0; i < descriptor.shaderCodes.Length; i++)
        {
            entryHashes[i] = RHIUtility.GetPassID(_nameHash, i);
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

    public ulong GetEntryID(int entryIndex)
    {
        Logger.DebugAssert(entryIndex >= 0 && entryIndex < 8, "Entry index out of bounds.");
        return entryHashes[entryIndex];
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
