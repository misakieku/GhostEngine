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
    /// Converts a native function name to a method name using the action's name.remove chain.
    /// Each entry in the remove list is applied in order, then leading/trailing underscores are trimmed.
    ///
    /// Supported remove tokens:
    ///   "PREFIX"            — strip the config's NativeTypePrefix from the start (e.g. "nvtt", "ufbx_")
    ///   "NO_PREFIX($TSelf)" — strip the target struct name minus its type prefix from the start,
    ///                         case-insensitively (e.g. NvttSurface → "Surface" stripped from "SurfaceWidth")
    ///
    /// nameOpts is the dynamic opts.name object from JSON (may be null).
    /// If no nameOpts are provided, the name is returned with only the library prefix stripped.
    /// </summary>
    public string GetMethodName(string nativeFunctionName, dynamic? nameOpts, string targetStructName)
    {
        var name = nativeFunctionName;

        if (nameOpts is null)
        {
            // Fallback: just strip the library prefix.
            return TrimUnderscores(StripPrefixIgnoreCase(name, _config.NativeTypePrefix));
        }

        string? set = nameOpts.set as string;
        if (!string.IsNullOrEmpty(set))
        {
            return set;
        }

        var removeTokens = nameOpts.remove as object?[] ?? [];
        foreach (var tokenObj in removeTokens)
        {
            var token = tokenObj as string ?? string.Empty;
            if (string.Equals(token, "PREFIX", StringComparison.Ordinal))
            {
                name = StripPrefixIgnoreCase(name, _config.NativeTypePrefix);
            }
            else if (token.StartsWith("NO_PREFIX(", StringComparison.Ordinal) && token.EndsWith(')'))
            {
                // Extract $TSelf — it's the literal token "NO_PREFIX($TSelf)", so the struct name
                // is resolved from the targetStructName argument passed in.
                // Strip the config prefix from the struct name to get the "bare" part.
                // Try prefix first, then suffix (handles both nvtt "SurfaceWidth"→"Width"
                // and ufbx "free_scene"→"free_" styles).
                var bareStructName = StripPrefixIgnoreCase(targetStructName, _config.NativeTypePrefix);

                // Remove directly, the name maybe nvttSetOutputOptionsOutputHeader, if we only remove prefix and suffix, OutputOptions in the middle will be ignored, so we remove the bare struct name directly, case-insensitively.
                name = name.Replace(bareStructName, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            name = TrimUnderscores(name);
        }

        var style = nameOpts.style as string;
        if (!string.IsNullOrEmpty(style))
        {
            if (string.Equals(style, "PascalCase", StringComparison.OrdinalIgnoreCase))
            {
                int counter = 0;
                Span<char> nameSpan = stackalloc char[name.Length];

                for (int i = 0; i < name.Length; i++)
                {
                    if (i == 0)
                    {
                        nameSpan[counter] = char.ToUpperInvariant(name[i]);
                        counter++;

                        continue;
                    }

                    if (name[i] == '_')
                    {
                        while (name[i] == '_' && i < name.Length)
                        {
                            i++;
                        }

                        nameSpan[counter] = char.ToUpperInvariant(name[i]);
                        counter++;

                        continue;
                    }

                    nameSpan[counter] = name[i];
                    counter++;
                }

                name = nameSpan[..counter].ToString();
            }
        }

        return name;
    }

    /// <summary>Strips the native type prefix (e.g. "ufbx_") from a type name.</summary>
    public string StripKnownPrefix(string nativeTypeName)
    {
        return StripPrefixIgnoreCase(nativeTypeName, _config.NativeTypePrefix);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string StripPrefixIgnoreCase(string name, string prefix)
    {
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return name[prefix.Length..];
        }

        return name;
    }

    private static string StripSuffixIgnoreCase(string name, string suffix)
    {
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return name[..^suffix.Length];
        }

        return name;
    }

    private static string TrimUnderscores(string name)
    {
        return name.Trim('_');
    }
}
