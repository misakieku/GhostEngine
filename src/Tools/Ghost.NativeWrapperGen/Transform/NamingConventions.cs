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
    ///   "$TBare" — strip the target struct name minus its type prefix from the start,
    ///                         case-insensitively (e.g. NvttSurface → "Surface" stripped from "SurfaceWidth")
    ///
    /// nameOpts is the dynamic opts.name object from JSON (may be null).
    /// If no nameOpts are provided, the name is returned with only the library prefix stripped.
    /// </summary>
    public string GetName(string nativeFunctionName, dynamic? nameOpts, string targetStructName)
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
            else if (string.Equals(token, "$TBare", StringComparison.Ordinal))
            {
                var bareStructName = StripPrefixIgnoreCase(targetStructName, _config.NativeTypePrefix);

                // Remove directly, the name maybe nvttSetOutputOptionsOutputHeader, if we only remove prefix and suffix, OutputOptions in the middle will be ignored, so we remove the bare struct name directly, case-insensitively.
                name = name.Replace(bareStructName, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            name = TrimUnderscores(name);
        }

        var style = nameOpts.style as string;
        if (!string.IsNullOrEmpty(style))
        {
            if (string.Equals(style, "PascalCase", StringComparison.Ordinal))
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
                        do
                        {
                            i++;
                        } while (i < name.Length && name[i] == '_');

                        nameSpan[counter] = char.ToUpperInvariant(name[i]);
                        counter++;

                        continue;
                    }

                    nameSpan[counter] = name[i];
                    counter++;
                }

                name = nameSpan[..counter].ToString();
            }
            else if (string.Equals(style, "ALL_CAPS", StringComparison.Ordinal))
            {
                int counter = 0;
                Span<char> nameSpan = stackalloc char[name.Length * 2]; // Worst case, every character is uppercase and followed by an underscore.

                for (int i = 0; i < name.Length; i++)
                {
                    // ___ to _
                    if (name[i] == '_')
                    {
                        while (i + 1 < name.Length && name[i + 1] == '_')
                        {
                            i++;
                        }

                        nameSpan[counter] = '_';
                        counter++;

                        continue;
                    }

                    // AbC to AB_C
                    if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                    {
                        nameSpan[counter] = '_';
                        counter++;
                    }

                    // ABC to ABC
                    while (i < name.Length && char.IsUpper(name[i]))
                    {
                        nameSpan[counter] = name[i];

                        counter++;
                        i++;
                    }

                    if (i == name.Length)
                    {
                        break;
                    }

                    nameSpan[counter] = char.ToUpperInvariant(name[i]);
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
