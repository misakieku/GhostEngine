using Ghost.NativeWrapperGen.Config;

namespace Ghost.NativeWrapperGen.Transform;

public sealed class NamingConventions
{
    private readonly WrapperConfig _config;

    public NamingConventions(WrapperConfig config)
    {
        _config = config;
    }

    public string GetWrapperTypeName(string nativeTypeName)
    {
        if (_config.TypeNameOverrides.TryGetValue(nativeTypeName, out var overrideName))
        {
            return overrideName;
        }

        return ToPascalCase(StripKnownPrefix(nativeTypeName));
    }

    public string GetPropertyName(string nativeName)
    {
        return ToPascalCase(nativeName);
    }

    private string StripKnownPrefix(string nativeTypeName)
    {
        if (nativeTypeName.StartsWith(_config.NativeTypePrefix, StringComparison.Ordinal))
        {
            return nativeTypeName[_config.NativeTypePrefix.Length..];
        }

        return nativeTypeName;
    }

    public static string ToPascalCase(string value)
    {
        var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return value;
        }

        return string.Concat(parts.Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
