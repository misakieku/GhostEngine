using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using Ghost.NativeWrapperGen.Parsing;

namespace Ghost.NativeWrapperGen.Transform;

public sealed class PublicTypeResolver
{
    private readonly NativeLibrary _library;
    private readonly WrapperConfig _config;
    private readonly NamingConventions _naming;

    public PublicTypeResolver(NativeLibrary library, WrapperConfig config, NamingConventions naming)
    {
        _library = library;
        _config = config;
        _naming = naming;
    }

    public string GetPublicType(string nativeTypeName)
    {
        if (string.Equals(nativeTypeName, "void", StringComparison.Ordinal))
        {
            return "void*";
        }

        if (_config.PublicTypeOverrides.TryGetValue(nativeTypeName, out var overrideType))
        {
            return overrideType;
        }

        var pointerDepth = BindingParser.GetPointerDepth(nativeTypeName);
        var baseType = BindingParser.TrimPointers(nativeTypeName);

        if (pointerDepth == 0)
        {
            return baseType;
        }

        if (_library.StructsByName.ContainsKey(baseType))
        {
            return pointerDepth switch
            {
                1 => _naming.GetWrapperTypeName(baseType),
                _ => nativeTypeName,
            };
        }

        return nativeTypeName;
    }

    public bool HasWrapper(string nativeTypeName)
    {
        return _library.StructsByName.ContainsKey(nativeTypeName);
    }
}
