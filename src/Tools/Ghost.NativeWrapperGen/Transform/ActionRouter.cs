using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using System.Text.RegularExpressions;

namespace Ghost.NativeWrapperGen.Transform;

/// <summary>
/// Evaluates <see cref="ActionConfig"/> routing rules and conditions against native functions.
/// </summary>
internal sealed class ActionRouter
{
    private readonly WrapperConfig _config;
    private readonly BindingTypeResolver _resolver;
    private readonly NamingConventions _naming;
    private readonly Dictionary<string, Regex> _regexCache = new(StringComparer.Ordinal);

    public ActionRouter(WrapperConfig config, BindingTypeResolver resolver, NamingConventions naming)
    {
        _config = config;
        _resolver = resolver;
        _naming = naming;
    }

    /// <summary>
    /// Matches <paramref name="func"/> against configured actions (first-match-wins) and appends
    /// resulting methods and inheritance declarations to the target collections.
    /// </summary>
    public void RouteFunction(NativeFunction func, Dictionary<string, List<RoutedMethod>> methodsByStruct, Dictionary<string, HashSet<string>> baseTypesByStruct)
    {
        foreach (var action in _config.Actions)
        {
            if (!string.Equals(action.Filter, "EXTERN_API", StringComparison.Ordinal))
            {
                continue;
            }

            var targetStruct = ResolveTargetType(func, action.TargetType);
            if (targetStruct is null)
            {
                continue;
            }

            if (!EvaluateConditions(func, action.Conditions, targetStruct))
            {
                continue;
            }

            foreach (var apply in action.Apply)
            {
                if (string.Equals(apply.Type, "INSTANCE_METHOD", StringComparison.Ordinal) || string.Equals(apply.Type, "STATIC_METHOD", StringComparison.Ordinal))
                {
                    var isInstance = string.Equals(apply.Type, "INSTANCE_METHOD", StringComparison.Ordinal);
                    if (!methodsByStruct.TryGetValue(targetStruct, out var list))
                    {
                        list = new List<RoutedMethod>();
                        methodsByStruct[targetStruct] = list;
                    }

                    list.Add(new RoutedMethod
                    {
                        Function = func,
                        TargetStructName = targetStruct,
                        IsInstance = isInstance,
                        Apply = apply,
                    });
                }
                else if (string.Equals(apply.Type, "INHERITANCE", StringComparison.Ordinal))
                {
                    var baseTypes = apply.Opts?.BaseType;
                    if (baseTypes is { Count: > 0 })
                    {
                        if (!baseTypesByStruct.TryGetValue(targetStruct, out var set))
                        {
                            set = new HashSet<string>(StringComparer.Ordinal);
                            baseTypesByStruct[targetStruct] = set;
                        }

                        foreach (var bt in baseTypes)
                        {
                            if (!string.IsNullOrWhiteSpace(bt))
                            {
                                set.Add(bt);
                            }
                        }
                    }
                }
            }

            return;
        }
    }

    private bool EvaluateConditions(NativeFunction func, List<string> conditions, string targetStructName)
    {
        foreach (var condition in conditions)
        {
            var span = condition.AsSpan().Trim();
            if (span.IsEmpty)
            {
                return false;
            }

            var inverse = span[0] == '!';
            var remaining = (inverse ? span[1..] : span).Trim();
            if (remaining.IsEmpty)
            {
                return false;
            }

            bool matched;
            if (remaining.StartsWith("NAME_CONDITION(", StringComparison.Ordinal) && remaining.EndsWith(')'))
            {
                var pattern = remaining["NAME_CONDITION(".Length..^1];
                var bareName = _naming.StripKnownPrefix(targetStructName);
                var resolvedPattern = pattern.ToString().Replace("$TBare", bareName, StringComparison.Ordinal).Replace("$TSelf", targetStructName, StringComparison.Ordinal);
                matched = GetOrCompileRegex(resolvedPattern).IsMatch(func.Name);
            }
            else
            {
                switch (remaining)
                {
                    case "SELF_PTR":
                        matched = func.Parameters.Count > 0 && _resolver.TryGetBindingStructName(func.Parameters[0].TypeName) is not null;
                        break;
                    case "FIRST_PARAM_OTHER_TYPE":
                        matched = func.Parameters.Count == 0 || _resolver.TryGetBindingStructName(func.Parameters[0].TypeName) is null;
                        break;
                    case "RETURN_BINDING_TYPE":
                        matched = _resolver.TryGetBindingStructName(func.ReturnType) is not null;
                        break;
                    case "VOID_RETURN":
                        matched = string.Equals(func.ReturnType, "void", StringComparison.Ordinal);
                        break;
                    default:
                        return false;
                }
            }

            if (inverse)
            {
                matched = !matched;
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private string? ResolveTargetType(NativeFunction func, string targetTypeRule)
    {
        return targetTypeRule switch
        {
            "FIRST_PARAM_TYPE" => func.Parameters.Count > 0 ? _resolver.TryGetBindingStructName(func.Parameters[0].TypeName) : null,
            "RETURN_TYPE" => _resolver.TryGetBindingStructName(func.ReturnType),
            _ => string.IsNullOrWhiteSpace(targetTypeRule) ? null : targetTypeRule,
        };
    }

    private Regex GetOrCompileRegex(string pattern)
    {
        if (!_regexCache.TryGetValue(pattern, out var regex))
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            _regexCache[pattern] = regex;
        }

        return regex;
    }
}

internal sealed class RoutedMethod
{
    public required NativeFunction Function { get; init; }
    public required string TargetStructName { get; init; }
    public required bool IsInstance { get; init; }
    public required ActionApplyConfig Apply { get; init; }
}
