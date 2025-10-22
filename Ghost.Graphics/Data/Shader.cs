using Ghost.Core;
using Ghost.Core.Contracts;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Data;

public readonly struct TextureInfo
{
    public uint RegisterSlot
    {
        get; init;
    }

    public uint RootParameterIndex
    {
        get; init;
    }
}

public readonly struct PropertyInfo
{
    public uint CBufferIndex
    {
        get; init;
    }

    public uint ByteOffset
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }
}

public readonly struct CBufferInfo
{
    public uint Size
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }
}

public readonly unsafe struct ShaderPass
{
    // NOTE: This is for per pass cbuffer only. Global, per view, and per mesh cbuffers are fixed.
    private readonly Dictionary<string, int> _propertyLookup;
    private readonly UnsafeList<PropertyInfo> _properties;

    internal readonly CBufferInfo PassPropertyInfo
    {
        get;
    }

    public ShaderPass(CBufferInfo info, UnsafeList<PropertyInfo> properties, Dictionary<string, int> propertyNameToIdMap)
    {
        PassPropertyInfo = info;
        _properties = properties;
        _propertyLookup = propertyNameToIdMap;
    }

    public readonly int GetPropertyId(string propertyName)
    {
        return _propertyLookup.TryGetValue(propertyName, out var id) ? id : -1;
    }

    public readonly PropertyInfo GetPropertyInfo(int id)
    {
        return _properties[id];
    }

    public readonly PropertyInfo GetPropertyInfo(string propertyName)
    {
        return _properties[GetPropertyId(propertyName)];
    }
}

/// <summary>
/// A representation of a GPU shader, including all the passes it contains.
/// </summary>
public readonly struct Shader : IResourceReleasable, IIdentifierType
{
    private readonly ShaderPassKey[] _passIDs;
    private readonly Dictionary<string, int> _passLookup; // pass name to index
    private readonly Dictionary<string, List<int>> _propertyLookup; // property name to pass index (property name to list of pass indices that contain the property)

    public int PassCount => _passIDs.Length;

    internal Shader(ShaderDescriptor descriptor)
    {
        _passIDs = new ShaderPassKey[descriptor.passes.Count];
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

    public readonly ShaderPassKey GetPassKey(int index)
    {
        return _passIDs[index];
    }

    public readonly bool TryGetPassKey(string passName, out ShaderPassKey? passID)
    {
        var index = _passLookup.GetValueOrDefault(passName, -1);
        if (index == -1)
        {
            passID = new(0);
            return false;
        }

        passID = _passIDs[index];
        return true;
    }

    public readonly IReadOnlyCollection<int> GetPropertyPassIndices(string propertyName)
    {
        if (_propertyLookup.TryGetValue(propertyName, out var passIndices))
        {
            return passIndices;
        }

        return Array.Empty<int>();
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        // Should we do something here?
    }
}