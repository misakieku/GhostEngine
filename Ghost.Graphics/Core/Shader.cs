using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public class ShaderPass : IResourceReleasable
{
    private CBufferInfo _cbufferInfo;
    // NOTE: This is for per pass cbuffer only. Global, per view, and per mesh cbuffers are fixed.
    private readonly Dictionary<string, int> _propertyLookup;

    public CBufferInfo CBuffer => _cbufferInfo;

    public ShaderPass(CBufferInfo info)
    {
        _cbufferInfo = info;

        _propertyLookup = new Dictionary<string, int>(info.Properties.Count);
        for (var i = 0; i < info.Properties.Count; i++)
        {
            _propertyLookup[info.Properties[i].Name] = i;
        }
    }

    public int GetPropertyId(string propertyName)
    {
        return _propertyLookup.TryGetValue(propertyName, out var id) ? id : -1;
    }

    public CBufferPropertyInfo GetPropertyInfo(int id)
    {
        return _cbufferInfo.Properties[id];
    }

    public CBufferPropertyInfo GetPropertyInfo(string propertyName)
    {
        return _cbufferInfo.Properties[GetPropertyId(propertyName)];
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
    }
}

/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public class Shader : IResourceReleasable, IIdentifierType
{
    private UnsafeArray<ShaderPassKey> _passIDs;
    // TODO: Optmize lookups with a better data structure if needed
    private readonly Dictionary<string, int> _passLookup; // pass name to index
    private readonly Dictionary<string, List<int>> _propertyLookup; // property name to pass index (property name to list of pass indices that contain the property)

    public int PassCount => _passIDs.Count;

    internal Shader(ShaderDescriptor descriptor)
    {
        _passIDs = new UnsafeArray<ShaderPassKey>(descriptor.passes.Count, Allocator.Persistent);
        _passLookup = new(descriptor.passes.Count);
        _propertyLookup = new(descriptor.passes.Count);

        for (var i = 0; i < descriptor.passes.Count; i++)
        {
            var pass = descriptor.passes[i];
            var passKey = new ShaderPassKey(pass.Identifier);

            _passIDs[i] = passKey;
            _passLookup[pass.Name] = i;

            if (pass is FullPassDescriptor fullPass)
            {
                if (fullPass.properties == null)
                {
                    continue;
                }

                foreach (var property in fullPass.properties)
                {
                    ref var passIndices = ref CollectionsMarshal.GetValueRefOrAddDefault(_propertyLookup, property.name, out var exists);
                    if (!exists || passIndices == null)
                    {
                        passIndices = new List<int>();
                    }

                    passIndices.Add(i);
                }
            }
            // TODO: handle inherited passes
        }
    }

    public int GetPassIndex(string passName)
    {
        return _passLookup.GetValueOrDefault(passName, -1);
    }

    public ShaderPassKey GetPassKey(int index)
    {
        return _passIDs[index];
    }

    public bool TryGetPassKey(string passName, out int passIndex, out ShaderPassKey passID)
    {
        var index = _passLookup.GetValueOrDefault(passName, -1);
        if (index == -1)
        {
            passIndex = -1;
            passID = new(0);
            return false;
        }

        passIndex = index;
        passID = _passIDs[index];
        return true;
    }

    public IReadOnlyCollection<int> GetPropertyPassIndices(string propertyName)
    {
        if (_propertyLookup.TryGetValue(propertyName, out var passIndices))
        {
            return passIndices;
        }

        return Array.Empty<int>();
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        _passIDs.Dispose();
    }
}
