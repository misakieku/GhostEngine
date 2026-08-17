using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Symbols;
using System.Text;
namespace Ghost.DSL.Composition;

public static class SpecializationResolver
{
    /// <summary>
    /// Resolves the full specialization matrix for a derived shader in a workspace,
    /// forming pass-local Cartesian products of composed open pipeline interfaces while
    /// fixing shader-scoped and closed interface bindings.
    /// </summary>
    public static Result<ResolvedShaderComposition> ResolveShaderComposition(ShaderWorkspace workspace, ShaderSymbol shader)
    {
        TemplateSymbol? baseTemplate = null;
        if (shader.BaseTemplateId.HasValue)
        {
            if (!workspace.Templates.TryGetValue(shader.BaseTemplateId.Value, out baseTemplate))
            {
                return Result.Failure($"Shader '{shader.QualifiedName}' references unknown base template ID 0x{shader.BaseTemplateId.Value:X16}.");
            }
        }

        // Determine passes: either inherited from template or locally defined on shader
        var passList = new List<(string name, List<ulong> composedIfaces, PassBlockSyntax syntax)>();

        if (baseTemplate != null)
        {
            foreach (var tmplPass in baseTemplate.Passes)
            {
                passList.Add((tmplPass.Name, tmplPass.ComposedInterfaceIds, tmplPass.Syntax));
            }
        }
        else if (shader.Syntax.Passes.Count > 0)
        {
            foreach (var passSyntax in shader.Syntax.Passes)
            {
                var composedIds = new List<ulong>();
                if (passSyntax.Compose != null)
                {
                    foreach (var ifaceName in passSyntax.Compose.Interfaces)
                    {
                        var iface = workspace.ResolveInterfaceReference(shader.ModuleName, ifaceName);
                        if (iface != null)
                        {
                            composedIds.Add(iface.Id);
                        }
                    }
                }
                passList.Add((passSyntax.Name, composedIds, passSyntax));
            }
        }
        else
        {
            return Result.Failure($"Shader '{shader.QualifiedName}' has no passes and does not derive from a template.");
        }

        var resolvedPasses = new List<ResolvedPassSpecializationSet>(passList.Count);

        for (var passIdx = 0; passIdx < passList.Count; passIdx++)
        {
            var (passName, composedInterfaceIds, passSyntax) = passList[passIdx];

            // 1. Pass with no composed interfaces -> single specialization, potentially template-shared
            if (composedInterfaceIds.Count == 0)
            {
                var isTemplateShared = baseTemplate != null && !shader.Syntax.Passes.Any(p => p.Name == passName);
                var templatePassId = isTemplateShared
                    ? SymbolId.Compute($"{baseTemplate!.QualifiedName}.{passName}")
                    : (ulong?)null;

                var singleSpec = new PassSpecialization
                {
                    CompositionKey = 0,
                    Bindings = new Dictionary<ulong, ulong>(),
                    Implementations = Array.Empty<ImplementationSymbol>(),
                    CompilerDefines = Array.Empty<string>(),
                    RequiredFeatureProviders = Array.Empty<string>()
                };

                resolvedPasses.Add(new ResolvedPassSpecializationSet
                {
                    PassName = passName,
                    PassIndex = passIdx,
                    IsTemplateShared = isTemplateShared,
                    TemplatePassId = templatePassId,
                    Specializations = new[] { singleSpec },
                    Syntax = passSyntax
                });
                continue;
            }

            // 2. Pass with composed interfaces: collect fixed bindings & pipeline domains
            var fixedBindings = new List<(InterfaceSymbol iface, ImplementationSymbol impl)>();
            var pipelineDomains = new List<(InterfaceSymbol iface, IReadOnlyList<ImplementationSymbol> candidates)>();

            foreach (var ifaceId in composedInterfaceIds)
            {
                if (!workspace.Interfaces.TryGetValue(ifaceId, out var iface))
                {
                    return Result.Failure($"Pass '{passName}' references unknown interface ID 0x{ifaceId:X16}.");
                }

                if (iface.Scope == InterfaceScope.Shader)
                {
                    // Shader-scoped interface: must be bound by shader or have template default
                    if (shader.Bindings.TryGetValue(iface.Id, out var boundImplId))
                    {
                        var impl = workspace.Implementations.TryGetValue(boundImplId, out var directImpl)
                            ? directImpl
                            : shader.LocalImplementations.Values.FirstOrDefault(l => l.Id == boundImplId);

                        if (impl == null)
                        {
                            return Result.Failure($"Shader '{shader.QualifiedName}' bound implementation 0x{boundImplId:X16} to interface '{iface.QualifiedName}', but symbol was not found.");
                        }

                        fixedBindings.Add((iface, impl));
                    }
                    else
                    {
                        // Check template slot default
                        var defaultSlot = baseTemplate?.Slots.FirstOrDefault(s => s.InterfaceId == iface.Id);
                        if (defaultSlot?.DefaultImplementationId != null && workspace.Implementations.TryGetValue(defaultSlot.DefaultImplementationId.Value, out var defaultImpl))
                        {
                            fixedBindings.Add((iface, defaultImpl));
                        }
                        else
                        {
                            return Result.Failure($"Required shader interface '{iface.QualifiedName}' in pass '{passName}' is not bound by shader '{shader.QualifiedName}'.");
                        }
                    }
                }
                else if (iface.IsClosed)
                {
                    // Closed interface: fixed to template default
                    var defaultSlot = baseTemplate?.Slots.FirstOrDefault(s => s.InterfaceId == iface.Id);
                    if (defaultSlot?.DefaultImplementationId != null && workspace.Implementations.TryGetValue(defaultSlot.DefaultImplementationId.Value, out var defaultImpl))
                    {
                        fixedBindings.Add((iface, defaultImpl));
                    }
                    else
                    {
                        return Result.Failure($"Closed interface '{iface.QualifiedName}' in pass '{passName}' requires a template default implementation.");
                    }
                }
                else
                {
                    // Open pipeline interface: Cartesian product across packaged candidate implementations
                    var packagedCandidates = workspace.GetPackagedPipelineImplementations(iface.Id);
                    if (packagedCandidates.Count == 0)
                    {
                        // Fallback to template default if present
                        var defaultSlot = baseTemplate?.Slots.FirstOrDefault(s => s.InterfaceId == iface.Id);
                        if (defaultSlot?.DefaultImplementationId != null && workspace.Implementations.TryGetValue(defaultSlot.DefaultImplementationId.Value, out var defaultImpl))
                        {
                            packagedCandidates = new[] { defaultImpl };
                        }
                        else
                        {
                            return Result.Failure($"No packaged implementations found for pipeline interface '{iface.QualifiedName}' in pass '{passName}'.");
                        }
                    }

                    pipelineDomains.Add((iface, packagedCandidates));
                }
            }

            // 3. Form Cartesian Product of Pipeline Interface Domains
            var product = GenerateCartesianProduct(pipelineDomains);
            var specializations = new List<PassSpecialization>(product.Count);

            foreach (var pipelineCombo in product)
            {
                var allBindingsList = new List<(ulong ifaceId, ulong implId)>(fixedBindings.Count + pipelineCombo.Count);
                var allImplsList = new List<ImplementationSymbol>(fixedBindings.Count + pipelineCombo.Count);
                var bindingsMap = new Dictionary<ulong, ulong>();
                var defines = new List<string>();
                var providers = new List<string>();

                // Add fixed bindings
                foreach (var (iface, impl) in fixedBindings)
                {
                    allBindingsList.Add((iface.Id, impl.Id));
                    allImplsList.Add(impl);
                    bindingsMap[iface.Id] = impl.Id;
                    defines.Add(BuildCompilerDefine(iface, impl));
                    if (!string.IsNullOrEmpty(impl.Provider))
                    {
                        providers.Add(impl.Provider);
                    }
                }

                // Add pipeline combination bindings
                foreach (var (iface, impl) in pipelineCombo)
                {
                    allBindingsList.Add((iface.Id, impl.Id));
                    allImplsList.Add(impl);
                    bindingsMap[iface.Id] = impl.Id;
                    defines.Add(BuildCompilerDefine(iface, impl));
                    if (!string.IsNullOrEmpty(impl.Provider))
                    {
                        providers.Add(impl.Provider);
                    }
                }

                var compositionKey = CompositionKey.Compute(allBindingsList.ToArray());

                specializations.Add(new PassSpecialization
                {
                    CompositionKey = compositionKey,
                    Bindings = bindingsMap,
                    Implementations = allImplsList,
                    CompilerDefines = defines,
                    RequiredFeatureProviders = providers.Distinct().ToList()
                });
            }

            resolvedPasses.Add(new ResolvedPassSpecializationSet
            {
                PassName = passName,
                PassIndex = passIdx,
                IsTemplateShared = false,
                TemplatePassId = null,
                Specializations = specializations,
                Syntax = passSyntax
            });
        }

        return new ResolvedShaderComposition
        {
            Shader = shader,
            BaseTemplate = baseTemplate,
            Passes = resolvedPasses
        };
    }

    private static List<List<(InterfaceSymbol iface, ImplementationSymbol impl)>> GenerateCartesianProduct(
        IReadOnlyList<(InterfaceSymbol iface, IReadOnlyList<ImplementationSymbol> candidates)> domains)
    {
        var result = new List<List<(InterfaceSymbol iface, ImplementationSymbol impl)>>();
        if (domains.Count == 0)
        {
            result.Add(new List<(InterfaceSymbol iface, ImplementationSymbol impl)>());
            return result;
        }

        var current = new (InterfaceSymbol iface, ImplementationSymbol impl)[domains.Count];

        void Backtrack(int domainIndex)
        {
            if (domainIndex == domains.Count)
            {
                result.Add(new List<(InterfaceSymbol iface, ImplementationSymbol impl)>(current));
                return;
            }

            var (iface, candidates) = domains[domainIndex];
            foreach (var candidate in candidates)
            {
                current[domainIndex] = (iface, candidate);
                Backtrack(domainIndex + 1);
            }
        }

        Backtrack(0);
        return result;
    }

    public static string BuildCompilerDefine(InterfaceSymbol iface, ImplementationSymbol impl)
    {
        var macroName = "GHOST_IMPL_" + SanitizeMacroName(iface.QualifiedName);
        var typeName = MangleSymbolName(impl.QualifiedName);
        return $"{macroName}={typeName}";
    }

    public static string MangleSymbolName(string qualifiedName)
    {
        var sb = new StringBuilder(qualifiedName.Length + 8);
        for (var i = 0; i < qualifiedName.Length; i++)
        {
            var c = qualifiedName[i];
            if (c is '.' or '/' or '-' or ' ')
            {
                sb.Append("__");
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static string SanitizeMacroName(string qualifiedName)
    {
        var dotIdx = qualifiedName.LastIndexOf('.');
        var localName = dotIdx >= 0 ? qualifiedName.Substring(dotIdx + 1) : qualifiedName;
        return localName.ToUpperInvariant();
    }
}
