using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.NativeWrapperGen.Transform;

/// <summary>
/// Plans public signatures, parameter remappings, sibling length derivations, and return remappings.
/// </summary>
internal sealed class ParameterPlanner
{
    private static readonly HashSet<string> s_numericLengthTypes = new(StringComparer.Ordinal)
    {
        "int", "uint", "long", "ulong", "short", "ushort", "byte", "sbyte", "nint", "nuint", "IntPtr", "UIntPtr",
    };

    private readonly List<CompiledParameterRemap> _parameterRemaps = new();
    private readonly List<CompiledReturnRemap> _returnRemaps = new();

    public ParameterPlanner(WrapperConfig config)
    {
        foreach (var remap in config.Remaps)
        {
            var hasGenericTypePlaceholder = remap.Dst.Contains("<$TYPE>", StringComparison.Ordinal);

            if (remap.Scope.Contains("parameter", StringComparer.Ordinal) && remap.Adapter?.ConvertBack is not null)
            {
                var filters = remap.Filter is { Count: > 0 } f
                    ? f.Select(static p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)).ToArray()
                    : Array.Empty<Regex>();

                Regex? siblingRegex = null;
                if (remap.DerivesFrom is not null && !string.IsNullOrWhiteSpace(remap.DerivesFrom.RegexName))
                {
                    siblingRegex = new Regex(remap.DerivesFrom.RegexName, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                }

                _parameterRemaps.Add(new CompiledParameterRemap(remap, hasGenericTypePlaceholder, filters, siblingRegex));
            }

            if (remap.Scope.Contains("return", StringComparer.Ordinal) && !string.IsNullOrEmpty(remap.Adapter?.ConvertTo))
            {
                _returnRemaps.Add(new CompiledReturnRemap(remap, hasGenericTypePlaceholder));
            }
        }
    }

    public ParameterPlan BuildParameterPlan(NativeFunction func, bool isInstance, bool removeFirstParam)
    {
        var selfParamIndex = isInstance && removeFirstParam && func.Parameters.Count > 0 ? 0 : -1;
        var consumedByDerivesFrom = new HashSet<int>();
        var derivedExprs = new Dictionary<int, string>();
        var matchedRemaps = new Dictionary<int, CompiledParameterRemap>();

        for (var i = 0; i < func.Parameters.Count; i++)
        {
            if (i == selfParamIndex || consumedByDerivesFrom.Contains(i))
            {
                continue;
            }

            var param = func.Parameters[i];
            foreach (var compiled in _parameterRemaps)
            {
                if (!MatchesParameterRemap(param, compiled))
                {
                    continue;
                }

                var derivesFrom = compiled.Config.DerivesFrom;
                if (derivesFrom is null)
                {
                    matchedRemaps[i] = compiled;
                    break;
                }

                var siblingIndex = FindSiblingLengthIndex(func.Parameters, i, selfParamIndex, consumedByDerivesFrom, matchedRemaps, compiled);
                if (siblingIndex >= 0)
                {
                    consumedByDerivesFrom.Add(siblingIndex);
                    derivedExprs[siblingIndex] = SubstituteArg(derivesFrom.Expr, param.Name);
                    matchedRemaps[i] = compiled;
                    break;
                }
            }
        }

        var publicParams = new List<PublicParam>();
        var allNativeParams = new List<NativeParamInfo>();

        for (var i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];

            if (i == selfParamIndex)
            {
                allNativeParams.Add(new NativeParamInfo
                {
                    PublicName = param.Name,
                    IsSelfParam = true,
                });
                continue;
            }

            if (consumedByDerivesFrom.Contains(i))
            {
                allNativeParams.Add(new NativeParamInfo
                {
                    PublicName = param.Name,
                    IsConsumedByDerivesFrom = true,
                    DerivedExpr = derivedExprs[i],
                });
                continue;
            }

            if (matchedRemaps.TryGetValue(i, out var matched))
            {
                var convertBack = matched.Config.Adapter!.ConvertBack!;
                var passAs = SubstituteArg(convertBack.PassAs.Replace("$TYPE", param.RawTypeName, StringComparison.Ordinal), param.Name);
                publicParams.Add(new PublicParam
                {
                    PublicType = matched.Config.Dst.Replace("$TYPE", param.RawTypeName, StringComparison.Ordinal),
                    PublicName = param.Name,
                    WrapCall = convertBack.WrapCall.Replace("$TYPE", param.RawTypeName, StringComparison.Ordinal),
                });
                allNativeParams.Add(new NativeParamInfo
                {
                    PublicName = param.Name,
                    PassAs = passAs,
                });
            }
            else
            {
                publicParams.Add(new PublicParam
                {
                    PublicType = param.TypeName,
                    PublicName = param.Name,
                });
                allNativeParams.Add(new NativeParamInfo
                {
                    PublicName = param.Name,
                });
            }
        }

        return new ParameterPlan
        {
            PublicParams = publicParams,
            AllNativeParams = allNativeParams,
            HasSelfParam = selfParamIndex >= 0,
        };
    }

    public ReturnPlan PlanReturn(NativeFunction func)
    {
        var rawReturnType = func.ReturnType.TrimEnd('*', '&');
        foreach (var compiled in _returnRemaps)
        {
            if (compiled.HasGenericTypePlaceholder && string.Equals(rawReturnType, "void", StringComparison.Ordinal))
            {
                continue;
            }

            var expectedSrc = compiled.Config.Src.Replace("$TYPE", rawReturnType, StringComparison.Ordinal);
            if (!string.Equals(expectedSrc, func.ReturnType, StringComparison.Ordinal))
            {
                continue;
            }

            return new ReturnPlan
            {
                PublicReturnType = compiled.Config.Dst.Replace("$TYPE", rawReturnType, StringComparison.Ordinal),
                ConvertToExpr = compiled.Config.Adapter!.ConvertTo!.Replace("$TYPE", rawReturnType, StringComparison.Ordinal),
            };
        }

        return new ReturnPlan
        {
            PublicReturnType = func.ReturnType,
            ConvertToExpr = null,
        };
    }

    /// <summary>
    /// Substitutes <c>$arg</c> in a template. When <c>$arg</c> is glued onto an identifier prefix
    /// (such as <c>p$arg</c>), any leading <c>@</c> on a keyword parameter name is stripped so the
    /// resulting C# identifier remains valid.
    /// </summary>
    public static string SubstituteArg(string template, string paramName)
    {
        const string placeholder = "$arg";
        if (!template.Contains(placeholder, StringComparison.Ordinal))
        {
            return template;
        }

        var bareName = StripVerbatimPrefix(paramName);
        var builder = new StringBuilder(template.Length);
        var index = 0;

        while (index < template.Length)
        {
            var match = template.IndexOf(placeholder, index, StringComparison.Ordinal);
            if (match < 0)
            {
                builder.Append(template, index, template.Length - index);
                break;
            }

            builder.Append(template, index, match - index);
            var gluedToIdentifier = match > 0 && (char.IsLetterOrDigit(template[match - 1]) || template[match - 1] == '_');
            builder.Append(gluedToIdentifier ? bareName : paramName);
            index = match + placeholder.Length;
        }

        return builder.ToString();
    }

    private static bool MatchesParameterRemap(NativeParameter param, CompiledParameterRemap compiled)
    {
        if (compiled.HasGenericTypePlaceholder && string.Equals(param.RawTypeName, "void", StringComparison.Ordinal))
        {
            return false;
        }

        var expectedSrc = compiled.Config.Src.Replace("$TYPE", param.RawTypeName, StringComparison.Ordinal);
        if (!string.Equals(expectedSrc, param.TypeName, StringComparison.Ordinal))
        {
            return false;
        }

        if (compiled.Filters.Length > 0)
        {
            var bareName = StripVerbatimPrefix(param.Name);
            var matched = false;
            foreach (var filter in compiled.Filters)
            {
                if (filter.IsMatch(bareName))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static int FindSiblingLengthIndex(
        IReadOnlyList<NativeParameter> parameters,
        int paramIndex,
        int selfParamIndex,
        HashSet<int> consumed,
        Dictionary<int, CompiledParameterRemap> matchedRemaps,
        CompiledParameterRemap compiled)
    {
        var derivesFrom = compiled.Config.DerivesFrom!;
        var bareParamName = StripVerbatimPrefix(parameters[paramIndex].Name);
        var expectedName = compiled.SiblingRegex is null ? derivesFrom.ParamPrefix + bareParamName + derivesFrom.ParamSuffix : string.Empty;

        // Search forward first to preserve pairing order when multiple buffer+len pairs exist, then backward.
        for (var j = paramIndex + 1; j < parameters.Count; j++)
        {
            if (IsMatchingSibling(parameters[j], j, selfParamIndex, consumed, matchedRemaps, compiled.SiblingRegex, expectedName))
            {
                return j;
            }
        }

        for (var j = 0; j < paramIndex; j++)
        {
            if (IsMatchingSibling(parameters[j], j, selfParamIndex, consumed, matchedRemaps, compiled.SiblingRegex, expectedName))
            {
                return j;
            }
        }

        return -1;
    }

    private static bool IsMatchingSibling(
        NativeParameter candidate,
        int candidateIndex,
        int selfParamIndex,
        HashSet<int> consumed,
        Dictionary<int, CompiledParameterRemap> matchedRemaps,
        Regex? siblingRegex,
        string expectedName)
    {
        if (candidateIndex == selfParamIndex || consumed.Contains(candidateIndex) || matchedRemaps.ContainsKey(candidateIndex))
        {
            return false;
        }

        if (!s_numericLengthTypes.Contains(candidate.TypeName))
        {
            return false;
        }

        var bareCandidateName = StripVerbatimPrefix(candidate.Name);
        return siblingRegex is not null
            ? siblingRegex.IsMatch(bareCandidateName)
            : string.Equals(bareCandidateName, expectedName, StringComparison.Ordinal);
    }

    private static string StripVerbatimPrefix(string identifier)
    {
        return identifier.StartsWith('@') ? identifier[1..] : identifier;
    }

    private sealed record CompiledParameterRemap(RemapConfig Config, bool HasGenericTypePlaceholder, Regex[] Filters, Regex? SiblingRegex);

    private sealed record CompiledReturnRemap(RemapConfig Config, bool HasGenericTypePlaceholder);
}

internal sealed class ParameterPlan
{
    public required List<PublicParam> PublicParams { get; init; }
    public required List<NativeParamInfo> AllNativeParams { get; init; }
    public required bool HasSelfParam { get; init; }
}

internal sealed class ReturnPlan
{
    public required string PublicReturnType { get; init; }
    public required string? ConvertToExpr { get; init; }
}

internal sealed class PublicParam
{
    public required string PublicType { get; init; }
    public required string PublicName { get; init; }
    public string? WrapCall { get; init; }
}

internal sealed class NativeParamInfo
{
    public required string PublicName { get; init; }
    public bool IsSelfParam { get; init; }
    public bool IsConsumedByDerivesFrom { get; init; }
    public string? DerivedExpr { get; init; }
    public string? PassAs { get; init; }
}
