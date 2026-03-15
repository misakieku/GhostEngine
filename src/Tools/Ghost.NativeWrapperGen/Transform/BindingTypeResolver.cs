using Ghost.NativeWrapperGen.Model;
using Ghost.NativeWrapperGen.Parsing;

namespace Ghost.NativeWrapperGen.Transform;

/// <summary>
/// Resolves whether a given C# type is a pointer to a known binding struct.
/// Used by the emitter to apply the SELF_PTR / RETURN_BINDING_TYPE action conditions.
/// </summary>
public sealed class BindingTypeResolver
{
    private readonly NativeLibrary _library;

    public BindingTypeResolver(NativeLibrary library)
    {
        _library = library;
    }

    /// <summary>
    /// Returns the base struct name if <paramref name="typeName"/> is a single-pointer to a known binding struct,
    /// otherwise returns null.
    /// Example: "ufbx_scene*" → "ufbx_scene", "ufbx_scene**" → null, "sbyte*" → null.
    /// </summary>
    public string? TryGetBindingStructName(string typeName)
    {
        if (BindingParser.GetPointerDepth(typeName) != 1)
        {
            return null;
        }

        var baseName = BindingParser.TrimPointers(typeName);
        return _library.StructsByName.ContainsKey(baseName) ? baseName : null;
    }

    /// <summary>Returns true if the type is a known binding struct (without pointer).</summary>
    public bool IsBindingStruct(string typeName) =>
        _library.StructsByName.ContainsKey(typeName);
}
