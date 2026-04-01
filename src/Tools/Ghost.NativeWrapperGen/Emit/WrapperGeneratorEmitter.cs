using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using Ghost.NativeWrapperGen.Transform;
using System.Text.RegularExpressions;

namespace Ghost.NativeWrapperGen.Emit;

/// <summary>
/// Emits partial struct files containing low-level method wrappers that route
/// DllImport functions from the Api class into the native struct types they belong to.
/// No wrapper classes, no properties, no owned/marshalled types — just methods.
/// </summary>
public sealed class WrapperGeneratorEmitter
{
    public IEnumerable<GeneratedFile> Emit(NativeLibrary library, WrapperConfig config)
    {
        var naming = new NamingConventions(config);
        var resolver = new BindingTypeResolver(library);
        var skipFunctions = new HashSet<string>(config.SkipFunctions, StringComparer.Ordinal);

        // Collect all DllImport functions, grouped by the target struct they'll be emitted on.
        // Each struct tracks a list of methods AND a set of base types (from INHERITANCE apply steps).
        var methodsByStruct = new Dictionary<string, List<RoutedMethod>>(StringComparer.Ordinal);
        var baseTypesByStruct = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var func in library.Functions)
        {
            if (!func.IsDllImport)
            {
                continue;
            }

            if (skipFunctions.Contains(func.Name))
            {
                continue;
            }

            RouteFunction(func, config, resolver, methodsByStruct, baseTypesByStruct);
        }

        // Emit one file per struct that has at least one routed method.
        foreach (var (structName, methods) in methodsByStruct.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            baseTypesByStruct.TryGetValue(structName, out var baseTypes);
            yield return EmitStructFile(structName, methods, baseTypes, config, naming);
        }
    }

    // ─── Routing ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to match the function against each action in order. On match, processes
    /// every apply step in the action's Apply list — INSTANCE_METHOD/STATIC_METHOD steps
    /// produce a RoutedMethod; INHERITANCE steps record base types on the target struct.
    /// Only the first matching action is used (first-match-wins).
    /// </summary>
    private static void RouteFunction(
        NativeFunction func,
        WrapperConfig config,
        BindingTypeResolver resolver,
        Dictionary<string, List<RoutedMethod>> methodsByStruct,
        Dictionary<string, HashSet<string>> baseTypesByStruct)
    {
        foreach (var action in config.Actions)
        {
            if (!string.Equals(action.Filter, "EXTERN_API", StringComparison.Ordinal))
            {
                continue;
            }

            // Determine the target struct name before evaluating NAME_CONDITION
            // (because NAME_CONDITION may reference $TSelf / $TBare).
            var targetStruct = ResolveTargetType(func, action.TargetType, resolver);
            if (targetStruct is null)
            {
                continue;
            }

            if (!EvaluateConditions(func, action.Conditions, resolver, targetStruct, config))
            {
                continue;
            }

            // Process each apply step.
            foreach (var apply in action.Apply)
            {
                var applyType = apply.Type;

                if (string.Equals(applyType, "INSTANCE_METHOD", StringComparison.Ordinal) ||
                    string.Equals(applyType, "STATIC_METHOD", StringComparison.Ordinal))
                {
                    var isInstance = string.Equals(applyType, "INSTANCE_METHOD", StringComparison.Ordinal);

                    var routed = new RoutedMethod
                    {
                        Function = func,
                        TargetStructName = targetStruct,
                        IsInstance = isInstance,
                        Apply = apply,
                    };

                    if (!methodsByStruct.TryGetValue(targetStruct, out var list))
                    {
                        list = [];
                        methodsByStruct[targetStruct] = list;
                    }

                    list.Add(routed);
                }
                else if (string.Equals(applyType, "INHERITANCE", StringComparison.Ordinal))
                {
                    var baseTypes = apply.Opts?.baseType as object?[];
                    if (baseTypes is { Length: > 0 })
                    {
                        if (!baseTypesByStruct.TryGetValue(targetStruct, out var set))
                        {
                            set = new HashSet<string>(StringComparer.Ordinal);
                            baseTypesByStruct[targetStruct] = set;
                        }

                        foreach (var bt in baseTypes)
                        {
                            if (bt is string s) set.Add(s);
                        }
                    }
                }
            }

            // First-match-wins: stop after the first matching action.
            return;
        }
    }

    private static bool EvaluateConditions(
        NativeFunction func,
        List<string> conditions,
        BindingTypeResolver resolver,
        string targetStructName,
        WrapperConfig config)
    {
        foreach (var condition in conditions)
        {
            var inverseCondition = false;

            var conditionSpan = condition.AsSpan();
            if (conditionSpan[0] == '!')
            {
                inverseCondition = true;
            }

            var remainingCondition = conditionSpan[(inverseCondition ? 1 : 0)..].Trim();

            // Handle parameterised conditions before the switch.
            if (remainingCondition.StartsWith("NAME_CONDITION(", StringComparison.Ordinal) &&
                remainingCondition.EndsWith(')'))
            {
                var pattern = remainingCondition["NAME_CONDITION(".Length..^1];

                // $TSelf → full struct name (e.g. "NvttSurface")
                // $TBare → struct name with the library prefix stripped (e.g. "Surface")
                var bareName = StripPrefix(targetStructName, config.NativeTypePrefix);
                var resolvedPattern = pattern.ToString()
                    .Replace("$TBare", bareName, StringComparison.Ordinal)
                    .Replace("$TSelf", targetStructName, StringComparison.Ordinal);

                var match = Regex.IsMatch(func.Name, resolvedPattern, RegexOptions.IgnoreCase);
                if (inverseCondition)
                {
                    match = !match;
                }

                if (!match)
                {
                    return false;
                }

                continue;
            }

            switch (remainingCondition)
            {
                case "SELF_PTR":
                    if (func.Parameters.Count == 0 ||
                        resolver.TryGetBindingStructName(func.Parameters[0].TypeName) is null)
                    {
                        return inverseCondition;
                    }

                    break;

                case "FIRST_PARAM_OTHER_TYPE":
                    if (func.Parameters.Count > 0 &&
                        resolver.TryGetBindingStructName(func.Parameters[0].TypeName) is not null)
                    {
                        return inverseCondition;
                    }

                    break;

                case "RETURN_BINDING_TYPE":
                    if (resolver.TryGetBindingStructName(func.ReturnType) is null)
                    {
                        return inverseCondition;
                    }

                    break;

                case "VOID_RETURN":
                    if (!string.Equals(func.ReturnType, "void", StringComparison.Ordinal))
                    {
                        return inverseCondition;
                    }

                    break;

                default:
                    // Unknown condition — treat as not-matched.
                    return false;
            }
        }

        return true;
    }

    private static string? ResolveTargetType(
        NativeFunction func,
        string targetTypeRule,
        BindingTypeResolver resolver)
    {
        return targetTypeRule switch
        {
            "FIRST_PARAM_TYPE" when func.Parameters.Count > 0 =>
                resolver.TryGetBindingStructName(func.Parameters[0].TypeName),
            "RETURN_TYPE" =>
                resolver.TryGetBindingStructName(func.ReturnType),
            _ => targetTypeRule,
        };
    }

    // ─── Code emission ────────────────────────────────────────────────────────

    private static GeneratedFile EmitStructFile(
        string structName,
        List<RoutedMethod> methods,
        HashSet<string>? baseTypes,
        WrapperConfig config,
        NamingConventions naming)
    {
        var writer = new CodeWriter();

        writer.WriteLine("// <auto-generated>");
        writer.WriteLine("// This file is generated by Ghost.NativeWrapperGen. Do not edit manually.");
        writer.WriteLine("// </auto-generated>");
        writer.WriteLine();
        writer.WriteLine($"namespace {config.OutputNamespace};");
        writer.WriteLine();

        // Build struct header with optional base-type list.
        var baseTypeList = baseTypes is { Count: > 0 }
            ? " : " + string.Join(", ", baseTypes.Order(StringComparer.Ordinal))
            : "";
        writer.WriteLine($"public unsafe partial struct {structName}{baseTypeList}");
        writer.WriteLine("{");

        using (writer.IndentScope())
        {
            var first = true;
            foreach (var method in methods)
            {
                if (!first)
                {
                    writer.WriteLine();
                }

                first = false;
                EmitMethod(writer, method, config, naming);
            }
        }

        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{structName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    private static void EmitMethod(
        CodeWriter writer,
        RoutedMethod routed,
        WrapperConfig config,
        NamingConventions naming)
    {
        var func = routed.Function;
        var nameOpts = routed.Apply.Opts?.name;
        var methodName = naming.GetName(func.Name, nameOpts, routed.TargetStructName);

        // Build the parameter plan: for each native parameter, determine the public type
        // and how to pass it to the Api call (applying remaps).
        var plan = BuildParameterPlan(func, config, routed);

        // Comment showing the source function.
        writer.WriteLine("/// <summary>");
        writer.WriteLine($"/// From: <see cref=\"Api.{func.Name}({string.Join(", ", func.Parameters.Select(static p => p.TypeName))})\" />");
        writer.WriteLine("/// </summary>");

        writer.WriteLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");

        // Signature
        var staticModifier = routed.IsInstance ? "" : "static ";
        var publicParams = plan.PublicParams;
        var paramList = string.Join(", ", publicParams.Select(static p => $"{p.PublicType} {p.PublicName}"));
        writer.WriteLine($"public {staticModifier}{func.ReturnType} {methodName}({paramList})");
        writer.WriteLine("{");

        using (writer.IndentScope())
        {
            EmitMethodBody(writer, func, routed, plan, config);
        }

        writer.WriteLine("}");
    }

    private static void EmitMethodBody(
        CodeWriter writer,
        NativeFunction func,
        RoutedMethod routed,
        ParameterPlan plan,
        WrapperConfig config)
    {
        // Collect the remapped params that need wrapCall blocks, in order.
        var wrappedParams = plan.PublicParams
            .Where(static p => p.WrapCall is not null)
            .ToList();

        // Build the Api call arguments.
        var args = new List<string>();

        if (routed.IsInstance)
        {
            // Self pointer — first native param is skipped from the public signature.
            // Use the passAs expression from apply opts if present, substituting $TSelf.
            var passAs = (string?)routed.Apply.Opts?.passAs;
            if (passAs is not null)
            {
                args.Add(passAs.Replace("$TSelf", routed.TargetStructName, StringComparison.Ordinal));
            }
            else
            {
                // Fallback: construct the self pointer expression directly.
                args.Add($"({routed.TargetStructName}*)Unsafe.AsPointer(ref this)");
            }
        }

        foreach (var p in plan.AllNativeParams)
        {
            if (p.IsConsumedByDerivesFrom)
            {
                args.Add(p.DerivedExpr!);
            }
            else if (p.IsSelfParam)
            {
                // Already handled above.
            }
            else if (p.PassAs is not null)
            {
                args.Add(p.PassAs.Replace("$arg", p.PublicName, StringComparison.Ordinal));
            }
            else
            {
                args.Add(p.PublicName);
            }
        }

        var hasReturn = !string.Equals(func.ReturnType, "void", StringComparison.Ordinal);
        var returnKeyword = hasReturn ? "return " : "";

        // Wrap the call in nested fixed() blocks (one per remapped parameter with a wrapCall).
        EmitNestedWrapCall(writer, wrappedParams, 0, (w) =>
        {
            if (args.Count <= 1)
            {
                var callExpr = $"Api.{func.Name}({string.Join(", ", args)})";
                w.WriteLine($"{returnKeyword}{callExpr};");
            }
            else
            {
                w.WriteLine($"{returnKeyword}Api.{func.Name}(");
                using (w.IndentScope())
                {
                    for (var i = 0; i < args.Count; i++)
                    {
                        var trailing = i < args.Count - 1 ? "," : ");";
                        w.WriteLine($"{args[i]}{trailing}");
                    }
                }
            }
        });
    }

    private static void EmitNestedWrapCall(
        CodeWriter writer,
        List<PublicParam> wrappedParams,
        int index,
        Action<CodeWriter> emitInner)
    {
        if (index >= wrappedParams.Count)
        {
            emitInner(writer);
            return;
        }

        var param = wrappedParams[index];
        var wrapCall = param.WrapCall!;

        var callPlaceholder = "$CALL";
        var splitIndex = wrapCall.IndexOf(callPlaceholder, StringComparison.Ordinal);

        if (splitIndex < 0)
        {
            writer.WriteLine(wrapCall.Replace("$arg", param.PublicName, StringComparison.Ordinal));
            EmitNestedWrapCall(writer, wrappedParams, index + 1, emitInner);
            return;
        }

        var before = wrapCall[..splitIndex].Replace("$arg", param.PublicName, StringComparison.Ordinal).Trim();
        var after = wrapCall[(splitIndex + callPlaceholder.Length)..].Replace("$arg", param.PublicName, StringComparison.Ordinal).Trim();

        if (before.EndsWith('{'))
        {
            writer.WriteLine(before[..^1].TrimEnd());
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                EmitNestedWrapCall(writer, wrappedParams, index + 1, emitInner);
            }

            if (after.StartsWith('}'))
            {
                writer.WriteLine("}");
                var remaining = after[1..].Trim();
                if (remaining.Length > 0)
                {
                    writer.WriteLine(remaining);
                }
            }
        }
        else
        {
            writer.WriteLine(before);
            using (writer.IndentScope())
            {
                EmitNestedWrapCall(writer, wrappedParams, index + 1, emitInner);
            }

            if (after.Length > 0)
            {
                writer.WriteLine(after);
            }
        }
    }

    // ─── Parameter planning ───────────────────────────────────────────────────

    private static ParameterPlan BuildParameterPlan(
        NativeFunction func,
        WrapperConfig config,
        RoutedMethod routed)
    {
        var allNativeParams = new List<NativeParamInfo>();
        var publicParams = new List<PublicParam>();

        var consumedByDerivesFrom = new HashSet<int>();
        var derivedExprs = new Dictionary<int, string>();
        var remapHasSibling = new HashSet<int>();

        for (var i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            var remap = FindRemap(param, config);
            if (remap?.DerivesFrom is null)
            {
                continue;
            }

            var expectedSiblingName = remap.DerivesFrom.ParamPrefix + param.Name + remap.DerivesFrom.ParamSuffix;
            for (var j = i + 1; j < func.Parameters.Count; j++)
            {
                if (string.Equals(func.Parameters[j].Name, expectedSiblingName, StringComparison.Ordinal))
                {
                    consumedByDerivesFrom.Add(j);
                    derivedExprs[j] = remap.DerivesFrom.Expr.Replace("$arg", param.Name, StringComparison.Ordinal);
                    remapHasSibling.Add(i);
                    break;
                }
            }
        }

        var selfParamIndex = routed.IsInstance ? 0 : -1;

        for (var i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            var isSelf = i == selfParamIndex;
            var isConsumed = consumedByDerivesFrom.Contains(i);

            if (isSelf)
            {
                allNativeParams.Add(new NativeParamInfo
                {
                    NativeName = param.Name,
                    PublicName = param.Name,
                    IsSelfParam = true,
                });
                continue;
            }

            if (isConsumed)
            {
                allNativeParams.Add(new NativeParamInfo
                {
                    NativeName = param.Name,
                    PublicName = param.Name,
                    IsConsumedByDerivesFrom = true,
                    DerivedExpr = derivedExprs[i],
                });
                continue;
            }

            var matchedRemap = FindRemap(param, config);

            if (matchedRemap?.DerivesFrom is not null && !remapHasSibling.Contains(i))
            {
                matchedRemap = null;
            }

            if (matchedRemap?.Adapter?.ConvertBack is not null)
            {
                var convertBack = matchedRemap.Adapter.ConvertBack;
                var publicParam = new PublicParam
                {
                    PublicType = matchedRemap.Dst.Replace("$TYPE", param.RawTypeName),
                    PublicName = param.Name,
                    WrapCall = convertBack.WrapCall.Replace("$TYPE", param.RawTypeName),
                    PassAs = convertBack.PassAs.Replace("$TYPE", param.RawTypeName),
                };
                publicParams.Add(publicParam);
                allNativeParams.Add(new NativeParamInfo
                {
                    NativeName = param.Name,
                    PublicName = param.Name,
                    PassAs = convertBack.PassAs.Replace("$TYPE", param.RawTypeName),
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
                    NativeName = param.Name,
                    PublicName = param.Name,
                });
            }
        }

        return new ParameterPlan
        {
            PublicParams = publicParams,
            AllNativeParams = allNativeParams,
        };
    }

    private static string StripPrefix(string name, string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return name;
        }

        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return name[prefix.Length..];
        }

        return name;
    }

    private static RemapConfig? FindRemap(NativeParameter param, WrapperConfig config)
    {
        foreach (var remap in config.Remaps)
        {
            if (!remap.Scope.Contains("parameter", StringComparer.Ordinal))
            {
                continue;
            }

            var src = remap.Src.Replace("$TYPE", param.RawTypeName);
            if (!string.Equals(src, param.TypeName, StringComparison.Ordinal))
            {
                continue;
            }

            if (remap.Filter is { Count: > 0 } filters)
            {
                var matches = filters.Any(f =>
                    Regex.IsMatch(param.Name, f, RegexOptions.IgnoreCase));
                if (!matches)
                {
                    continue;
                }
            }

            return remap;
        }

        return null;
    }

    // ─── Inner types ──────────────────────────────────────────────────────────

    private sealed class RoutedMethod
    {
        public required NativeFunction Function { get; init; }
        public required string TargetStructName { get; init; }
        public required bool IsInstance { get; init; }
        /// <summary>The specific INSTANCE_METHOD/STATIC_METHOD apply step that produced this method.</summary>
        public required ActionApplyConfig Apply { get; init; }
    }

    private sealed class ParameterPlan
    {
        public required List<PublicParam> PublicParams { get; init; }
        public required List<NativeParamInfo> AllNativeParams { get; init; }
    }

    private sealed class PublicParam
    {
        public required string PublicType { get; init; }
        public required string PublicName { get; init; }
        public string? WrapCall { get; init; }
        public string? PassAs { get; init; }
    }

    private sealed class NativeParamInfo
    {
        public required string NativeName { get; init; }
        public required string PublicName { get; init; }
        public bool IsSelfParam { get; init; }
        public bool IsConsumedByDerivesFrom { get; init; }
        public string? DerivedExpr { get; init; }
        public string? PassAs { get; init; }
    }
}
