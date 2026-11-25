using Ghost.NativeWrapperGen.Config;

namespace Ghost.NativeWrapperGen.Transform;

public sealed class NamingConventions
{
    private readonly WrapperConfig _config;

    public NamingConventions(WrapperConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Converts a native function name to a method name using <paramref name="nameOpts"/>.
    /// </summary>
    public string GetName(string nativeFunctionName, NameOptsConfig? nameOpts, string targetStructName)
    {
        var name = nativeFunctionName;

        if (nameOpts is null)
        {
            return TrimUnderscores(StripPrefixIgnoreCase(name, _config.NativeTypePrefix));
        }

        if (!string.IsNullOrEmpty(nameOpts.Set))
        {
            return nameOpts.Set;
        }

        if (nameOpts.Remove is { Count: > 0 } removeTokens)
        {
            foreach (var token in removeTokens)
            {
                if (string.Equals(token, "PREFIX", StringComparison.Ordinal))
                {
                    name = StripPrefixIgnoreCase(name, _config.NativeTypePrefix);
                }
                else if (string.Equals(token, "$TBare", StringComparison.Ordinal))
                {
                    var bareStructName = StripPrefixIgnoreCase(targetStructName, _config.NativeTypePrefix);
                    // Remove directly because struct names can appear mid-identifier (e.g. nvttSetOutputOptionsOutputHeader).
                    name = name.Replace(bareStructName, string.Empty, StringComparison.OrdinalIgnoreCase);
                }

                name = TrimUnderscores(name);
            }
        }

        if (string.Equals(nameOpts.Style, "PascalCase", StringComparison.Ordinal))
        {
            name = ToPascalCase(name);
        }
        else if (string.Equals(nameOpts.Style, "ALL_CAPS", StringComparison.Ordinal))
        {
            name = ToAllCaps(name);
        }

        return name;
    }

    /// <summary>Strips the native type prefix (e.g. "ufbx_") from a type name.</summary>
    public string StripKnownPrefix(string nativeTypeName)
    {
        return StripPrefixIgnoreCase(nativeTypeName, _config.NativeTypePrefix);
    }

    private static string ToPascalCase(string name)
    {
        if (name.Length == 0)
        {
            return string.Empty;
        }

        var counter = 0;
        Span<char> nameSpan = name.Length <= 256 ? stackalloc char[name.Length] : new char[name.Length];

        for (var i = 0; i < name.Length; i++)
        {
            if (i == 0 && name[i] != '_')
            {
                nameSpan[counter++] = char.ToUpperInvariant(name[i]);
                continue;
            }

            if (name[i] == '_')
            {
                while (i < name.Length && name[i] == '_')
                {
                    i++;
                }

                if (i < name.Length)
                {
                    nameSpan[counter++] = char.ToUpperInvariant(name[i]);
                }

                continue;
            }

            nameSpan[counter++] = name[i];
        }

        return nameSpan[..counter].ToString();
    }

    private static string ToAllCaps(string name)
    {
        if (name.Length == 0)
        {
            return string.Empty;
        }

        var counter = 0;
        var maxLen = name.Length * 2;
        Span<char> nameSpan = maxLen <= 512 ? stackalloc char[maxLen] : new char[maxLen];

        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '_')
            {
                while (i + 1 < name.Length && name[i + 1] == '_')
                {
                    i++;
                }

                nameSpan[counter++] = '_';
                continue;
            }

            if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
            {
                nameSpan[counter++] = '_';
            }

            while (i < name.Length && char.IsUpper(name[i]))
            {
                nameSpan[counter++] = name[i];
                i++;
            }

            if (i == name.Length)
            {
                break;
            }

            nameSpan[counter++] = char.ToUpperInvariant(name[i]);
        }

        return nameSpan[..counter].ToString();
    }

    private static string StripPrefixIgnoreCase(string name, string? prefix)
    {
        if (!string.IsNullOrEmpty(prefix) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return name[prefix.Length..];
        }

        return name;
    }

    private static string TrimUnderscores(string name)
    {
        return name.Trim('_');
    }
}
