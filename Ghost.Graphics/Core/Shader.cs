using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public readonly struct ShaderPass
{
    public Key64<ShaderPass> Key
    {
        get; init;
    }

    public PipelineState DeafaultState
    {
        get; init;
    }

    public LocalKeywordSet KeywordIDs
    {
        get; init;
    }
}

public struct ShaderProperty;

public partial struct Shader
{
    private static readonly Dictionary<string, int> s_passNameToID = new Dictionary<string, int>();
    private static int s_nextPassID = 0;

    private static readonly Dictionary<string, int> s_propertyNameToID = new Dictionary<string, int>();
    private static int s_nextPropertyID = 0;

    private static readonly Dictionary<string, int> s_keywordNameToID = new Dictionary<string, int>();
    private static readonly Dictionary<int, string> s_keywordIDToName = new Dictionary<int, string>();
    private static int s_nextkeywordID = 0;

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
            id = s_nextkeywordID++;
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
    private readonly uint _cbufferSize;
    private UnsafeArray<ShaderPass> _shaderPasses;
    private UnsafeHashMap<int, int> _passIDToLocal;
    private UnsafeHashMap<int, int> _keywordIDToLocal;

    public readonly int PassCount => _shaderPasses.Count;
    public readonly uint CBufferSize => _cbufferSize;

    internal Shader(ShaderDescriptor descriptor)
    {
        _cbufferSize = descriptor.cbufferSize;
        _shaderPasses = new UnsafeArray<ShaderPass>(descriptor.passes.Length, Allocator.Persistent);
        _passIDToLocal = new UnsafeHashMap<int, int>(descriptor.passes.Length, Allocator.Persistent);
        _keywordIDToLocal = new UnsafeHashMap<int, int>(32, Allocator.Persistent);

        for (var i = 0; i < descriptor.passes.Length; i++)
        {
            var pass = descriptor.passes[i];
            var passKey = RHIUtility.CreateShaderPassKey(pass.identifier);
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
                DeafaultState = pass.localPipeline,
                KeywordIDs = keywords,
            };

            _passIDToLocal[GetPassID(pass.name)] = (ushort)i;
        }
    }

    internal int GetLocalKeywordIndex(int globalKeywordID)
    {
        if (_keywordIDToLocal.TryGetValue(globalKeywordID, out var localIndex))
        {
            return localIndex;
        }

        return -1;
    }

    public readonly int GetPassIndex(Identifier<ShaderPass> passID)
    {
        if (_passIDToLocal.TryGetValue(passID.Value, out var index))
        {
            return index;
        }

        return -1;
    }

    public readonly int GetPassIndex(string passName)
    {
        if (_passIDToLocal.TryGetValue(GetPassID(passName), out var index))
        {
            return index;
        }

        return -1;
    }

    public readonly ref ShaderPass GetPassReference(int index)
    {
        return ref _shaderPasses[index];
    }

    public readonly Result<ShaderPass, Error> TryGetPass(Identifier<ShaderPass> passID, out int passIndex)
    {
        if (_passIDToLocal.TryGetValue(passID.Value, out var index))
        {
            passIndex = -1;
            return Error.NotFound;
        }

        passIndex = index;
        return _shaderPasses[index];
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        _keywordIDToLocal.Dispose();
        _shaderPasses.Dispose();
        _passIDToLocal.Dispose();
    }
}
