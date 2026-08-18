using Ghost.Core;
using Ghost.DSL.Composition;
using Ghost.DSL.Properties;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Symbols;
using Ghost.DSL.Syntax.Symbols;
using System.Text;

namespace Ghost.DSL.ShaderCompiler;

public sealed class ShaderWorkspace
{
    private readonly Dictionary<string, ModuleSymbol> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, InterfaceSymbol> _interfacesById = new();
    private readonly Dictionary<string, InterfaceSymbol> _interfacesByQualifiedName = new(StringComparer.Ordinal);

    private readonly Dictionary<ulong, ImplementationSymbol> _implementationsById = new();
    private readonly Dictionary<string, ImplementationSymbol> _implementationsByQualifiedName = new(StringComparer.Ordinal);

    private readonly Dictionary<ulong, TemplateSymbol> _templatesById = new();
    private readonly Dictionary<string, TemplateSymbol> _templatesByQualifiedName = new(StringComparer.Ordinal);

    private readonly Dictionary<ulong, ShaderSymbol> _shadersById = new();
    private readonly Dictionary<string, ShaderSymbol> _shadersByQualifiedName = new(StringComparer.Ordinal);

    private readonly Dictionary<ulong, List<ImplementationSymbol>> _packagedPipelineImplementations = new();
    private readonly List<DSLShaderError> _diagnostics = new();

    public IReadOnlyDictionary<string, ModuleSymbol> Modules => _modules;
    public IReadOnlyDictionary<ulong, InterfaceSymbol> Interfaces => _interfacesById;
    public IReadOnlyDictionary<ulong, ImplementationSymbol> Implementations => _implementationsById;
    public IReadOnlyDictionary<ulong, TemplateSymbol> Templates => _templatesById;
    public IReadOnlyDictionary<ulong, ShaderSymbol> Shaders => _shadersById;
    public IReadOnlyList<DSLShaderError> Diagnostics => _diagnostics;

    /// <summary>
    /// Creates and populates a ShaderWorkspace by discovering all .gshdr, .gmod, and .gcomp
    /// files across all specified asset directories.
    /// </summary>
    public static Result<ShaderWorkspace> CreateFromAssetDirectories(IEnumerable<string> assetDirectories)
    {
        var workspace = new ShaderWorkspace();

        foreach (var dir in assetDirectories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".gshdr", StringComparison.OrdinalIgnoreCase)
                                          || f.EndsWith(".gmod", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                try
                {
                    var code = File.ReadAllText(file);
                    var parseResult = DSLShaderCompiler.ParseDSLDocument(code);
                    if (parseResult.IsFailure)
                    {
                        return Result.Failure($"Syntax error in '{file}': {parseResult.Message}");
                    }

                    workspace.IndexDocument(file, parseResult.Value);
                }
                catch (Exception ex)
                {
                    return Result.Failure($"Failed to read '{file}': {ex.Message}");
                }
            }
        }

        var resolveResult = workspace.ResolveAndValidate();
        if (resolveResult.IsFailure)
        {
            return Result.Failure(resolveResult.Message);
        }

        return workspace;
    }

    /// <summary>
    /// Indexes a parsed DSL document's declarations into the workspace symbol tables.
    /// </summary>
    public void IndexDocument(string filePath, DSLDocumentSyntax document)
    {
        // 1. Index Top-Level Modules
        foreach (var modSyntax in document.Modules)
        {
            IndexModule(filePath, modSyntax);
        }

        // 2. Index Top-Level Standalone Declarations (outside any module)
        foreach (var ifaceSyntax in document.Interfaces)
        {
            RegisterInterface(filePath, null, ifaceSyntax);
        }

        foreach (var implSyntax in document.Implementations)
        {
            RegisterImplementation(filePath, null, implSyntax);
        }

        foreach (var tmplSyntax in document.Templates)
        {
            RegisterTemplate(filePath, null, tmplSyntax);
        }

        foreach (var shdrSyntax in document.Shaders)
        {
            RegisterShader(filePath, null, shdrSyntax);
        }
    }

    private void IndexModule(string filePath, ModuleDeclarationSyntax modSyntax)
    {
        if (!_modules.TryGetValue(modSyntax.Name, out var module))
        {
            module = new ModuleSymbol
            {
                Name = modSyntax.Name,
                SourceFile = filePath
            };
            _modules[module.Name] = module;
        }

        foreach (var imp in modSyntax.Imports)
        {
            module.Imports.Add(imp.ModuleName);
        }

        foreach (var ifaceSyntax in modSyntax.Interfaces)
        {
            RegisterInterface(filePath, module, ifaceSyntax);
        }

        foreach (var implSyntax in modSyntax.Implementations)
        {
            RegisterImplementation(filePath, module, implSyntax);
        }

        foreach (var tmplSyntax in modSyntax.Templates)
        {
            RegisterTemplate(filePath, module, tmplSyntax);
        }

        foreach (var shdrSyntax in modSyntax.Shaders)
        {
            RegisterShader(filePath, module, shdrSyntax);
        }
    }

    private void RegisterInterface(string filePath, ModuleSymbol? module, InterfaceDeclarationSyntax syntax)
    {
        var qualifiedName = QualifyName(module?.Name, syntax.Name);
        var id = SymbolId.Compute(qualifiedName);

        if (_interfacesById.TryGetValue(id, out var existing))
        {
            _diagnostics.Add(new DSLShaderError
            {
                message = $"Duplicate interface declaration '{qualifiedName}' found in '{filePath}' (already defined in '{existing.SourceFile}').",
                line = 1,
                column = 1
            });
            return;
        }

        var symbol = new InterfaceSymbol
        {
            QualifiedName = qualifiedName,
            Id = id,
            Scope = syntax.Scope,
            IsClosed = syntax.IsClosed,
            IsExported = syntax.IsExported,
            SourceFile = filePath,
            ModuleName = module?.Name,
            SignatureBody = syntax.Body,
            Syntax = syntax
        };

        _interfacesById[id] = symbol;
        _interfacesByQualifiedName[qualifiedName] = symbol;
        if (module != null)
        {
            module.Interfaces[syntax.Name] = symbol;
        }
    }

    private void RegisterImplementation(string filePath, ModuleSymbol? module, ImplementationDeclarationSyntax syntax)
    {
        var qualifiedName = QualifyName(module?.Name, syntax.Name);
        var id = SymbolId.Compute(qualifiedName);

        if (_implementationsById.TryGetValue(id, out var existing))
        {
            _diagnostics.Add(new DSLShaderError
            {
                message = $"Duplicate implementation declaration '{qualifiedName}' found in '{filePath}' (already defined in '{existing.SourceFile}').",
                line = 1,
                column = 1
            });
            return;
        }

        var symbol = new ImplementationSymbol
        {
            QualifiedName = qualifiedName,
            Id = id,
            InterfaceQualifiedName = syntax.InterfaceName,
            InterfaceId = 0, // Resolved in ResolveAndValidate
            IsExported = syntax.IsExported,
            SourceFile = filePath,
            ModuleName = module?.Name,
            Provider = syntax.Provider,
            Body = syntax.Body,
            Syntax = syntax
        };

        _implementationsById[id] = symbol;
        _implementationsByQualifiedName[qualifiedName] = symbol;
        if (module != null)
        {
            module.Implementations[syntax.Name] = symbol;
        }
    }

    private void RegisterTemplate(string filePath, ModuleSymbol? module, TemplateDeclarationSyntax syntax)
    {
        var qualifiedName = QualifyName(module?.Name, syntax.Name);
        var id = SymbolId.Compute(qualifiedName);

        if (_templatesById.TryGetValue(id, out var existing))
        {
            _diagnostics.Add(new DSLShaderError
            {
                message = $"Duplicate template declaration '{qualifiedName}' found in '{filePath}' (already defined in '{existing.SourceFile}').",
                line = 1,
                column = 1
            });
            return;
        }

        var symbol = new TemplateSymbol
        {
            QualifiedName = qualifiedName,
            Id = id,
            IsExported = syntax.IsExported,
            SourceFile = filePath,
            ModuleName = module?.Name,
            Syntax = syntax
        };
        var propErrors = new List<Ghost.DSL.Parser.DSLShaderError>();
        symbol.PropertySchema = PropertyLayoutEngine.ComputeTemplateLayout(syntax, qualifiedName, propErrors);
        foreach (var err in propErrors)
        {
            _diagnostics.Add(new DSLShaderError
            {
                message = err.Message,
                line = err.Line,
                column = err.Column
            });
        }


        foreach (var slot in syntax.Slots)
        {
            symbol.Slots.Add(new TemplateSlotSymbol
            {
                InterfaceQualifiedName = slot.InterfaceName,
                InterfaceId = 0,
                DefaultImplementationQualifiedName = slot.DefaultImplementationName,
                DefaultImplementationId = null
            });
        }

        foreach (var pass in syntax.Passes)
        {
            var passSym = new TemplatePassSymbol
            {
                Name = pass.Name,
                Syntax = pass
            };
            if (pass.Compose != null)
            {
                passSym.ComposedInterfaces.AddRange(pass.Compose.Interfaces);
            }
            symbol.Passes.Add(passSym);
        }

        _templatesById[id] = symbol;
        _templatesByQualifiedName[qualifiedName] = symbol;
        if (module != null)
        {
            module.Templates[syntax.Name] = symbol;
        }
    }

    private void RegisterShader(string filePath, ModuleSymbol? module, ShaderDeclarationSyntax syntax)
    {
        var qualifiedName = QualifyName(module?.Name, syntax.Name);
        var id = SymbolId.Compute(qualifiedName);

        if (_shadersById.TryGetValue(id, out var existing))
        {
            _diagnostics.Add(new DSLShaderError
            {
                message = $"Duplicate shader declaration '{qualifiedName}' found in '{filePath}' (already defined in '{existing.SourceFile}').",
                line = 1,
                column = 1
            });
            return;
        }

        var symbol = new ShaderSymbol
        {
            QualifiedName = qualifiedName,
            Id = id,
            BaseTemplateQualifiedName = syntax.TemplateName,
            BaseTemplateId = null,
            IsExported = syntax.IsExported,
            SourceFile = filePath,
            ModuleName = module?.Name,
            PayloadBody = syntax.Payload?.Body,
            Syntax = syntax
        };

        // Register local implementations
        foreach (var implSyntax in syntax.Implementations)
        {
            var localQualName = $"{qualifiedName}.{implSyntax.Name}";
            var localId = SymbolId.Compute(localQualName);
            var localImpl = new ImplementationSymbol
            {
                QualifiedName = localQualName,
                Id = localId,
                InterfaceQualifiedName = implSyntax.InterfaceName,
                InterfaceId = 0,
                IsExported = false,
                SourceFile = filePath,
                ModuleName = module?.Name,
                Provider = implSyntax.Provider,
                Body = implSyntax.Body,
                Syntax = implSyntax
            };
            symbol.LocalImplementations[implSyntax.Name] = localImpl;
        }

        _shadersById[id] = symbol;
        _shadersByQualifiedName[qualifiedName] = symbol;
        if (module != null)
        {
            module.Shaders[syntax.Name] = symbol;
        }
    }

    /// <summary>
    /// Resolves symbol references, builds dependency DAGs, verifies ownership invariants,
    /// and collects packaged pipeline candidate domains.
    /// </summary>
    public Result ResolveAndValidate()
    {
        if (_diagnostics.Count > 0)
        {
            return BuildFailureResult();
        }

        // 1. Validate Module Imports and Detect Dependency Cycles
        ValidateModuleDAG();
        if (_diagnostics.Count > 0)
        {
            return BuildFailureResult();
        }

        // 2. Resolve Implementations -> Interfaces
        ResolveImplementations();

        // 3. Resolve Templates -> Slots and Passes
        ResolveTemplates();

        // 4. Resolve Shaders -> Base Templates and Bindings
        ResolveShaders();

        // 5. Build Packaged Pipeline Implementation Domains
        BuildPipelineDomains();

        if (_diagnostics.Count > 0)
        {
            return BuildFailureResult();
        }

        return Result.Success();
    }

    private void ValidateModuleDAG()
    {
        // 1. Check all imports resolve to existing modules
        foreach (var (modName, module) in _modules)
        {
            foreach (var importName in module.Imports)
            {
                if (!_modules.ContainsKey(importName))
                {
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Module '{modName}' in '{module.SourceFile}' imports unknown module '{importName}'.",
                        line = 1,
                        column = 1
                    });
                }
            }
        }

        if (_diagnostics.Count > 0)
        {
            return;
        }

        // 2. Cycle detection via DFS (0 = White, 1 = Gray/Visiting, 2 = Black/Visited)
        var visited = new Dictionary<string, int>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var modName in _modules.Keys)
        {
            if (!visited.ContainsKey(modName))
            {
                if (DetectCycleDFS(modName, visited, path))
                {
                    return;
                }
            }
        }
    }

    private bool DetectCycleDFS(string current, Dictionary<string, int> visited, List<string> path)
    {
        visited[current] = 1; // Gray
        path.Add(current);

        if (_modules.TryGetValue(current, out var module))
        {
            foreach (var next in module.Imports)
            {
                if (!visited.TryGetValue(next, out var state) || state == 0)
                {
                    if (DetectCycleDFS(next, visited, path))
                    {
                        return true;
                    }
                }
                else if (state == 1) // Cycle detected
                {
                    var cycleStartIdx = path.IndexOf(next);
                    var cycleStr = string.Join(" -> ", path.Skip(cycleStartIdx)) + " -> " + next;
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Circular module dependency detected: {cycleStr}",
                        line = 1,
                        column = 1
                    });
                    return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        visited[current] = 2; // Black
        return false;
    }

    private void ResolveImplementations()
    {
        foreach (var impl in _implementationsById.Values)
        {
            var iface = ResolveInterfaceReference(impl.ModuleName, impl.InterfaceQualifiedName);
            if (iface == null)
            {
                _diagnostics.Add(new DSLShaderError
                {
                    message = $"Implementation '{impl.QualifiedName}' implements unknown interface '{impl.InterfaceQualifiedName}'.",
                    line = 1,
                    column = 1
                });
            }
            else
            {
                impl.InterfaceQualifiedName = iface.QualifiedName;
                impl.InterfaceId = iface.Id;
            }
        }
    }

    private void ResolveTemplates()
    {
        foreach (var tmpl in _templatesById.Values)
        {
            var slotInterfaceIds = new HashSet<ulong>();

            foreach (var slot in tmpl.Slots)
            {
                var iface = ResolveInterfaceReference(tmpl.ModuleName, slot.InterfaceQualifiedName);
                if (iface == null)
                {
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Template '{tmpl.QualifiedName}' references unknown interface '{slot.InterfaceQualifiedName}' in slot.",
                        line = 1,
                        column = 1
                    });
                    continue;
                }

                slot.InterfaceQualifiedName = iface.QualifiedName;
                slot.InterfaceId = iface.Id;
                slotInterfaceIds.Add(iface.Id);

                if (iface.IsClosed && string.IsNullOrEmpty(slot.DefaultImplementationQualifiedName))
                {
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Closed interface '{iface.QualifiedName}' in template '{tmpl.QualifiedName}' requires a template-owned default implementation.",
                        line = 1,
                        column = 1
                    });
                }

                if (!string.IsNullOrEmpty(slot.DefaultImplementationQualifiedName))
                {
                    var defaultImpl = ResolveImplementationReference(tmpl.ModuleName, null, slot.DefaultImplementationQualifiedName);
                    if (defaultImpl == null)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Template '{tmpl.QualifiedName}' references unknown default implementation '{slot.DefaultImplementationQualifiedName}' for slot '{iface.QualifiedName}'.",
                            line = 1,
                            column = 1
                        });
                    }
                    else
                    {
                        slot.DefaultImplementationQualifiedName = defaultImpl.QualifiedName;
                        slot.DefaultImplementationId = defaultImpl.Id;

                        if (defaultImpl.InterfaceId != 0 && defaultImpl.InterfaceId != iface.Id)
                        {
                            _diagnostics.Add(new DSLShaderError
                            {
                                message = $"Default implementation '{defaultImpl.QualifiedName}' implements '{defaultImpl.InterfaceQualifiedName}', not expected interface '{iface.QualifiedName}'.",
                                line = 1,
                                column = 1
                            });
                        }
                    }
                }
            }

            foreach (var pass in tmpl.Passes)
            {
                foreach (var composedRef in pass.ComposedInterfaces)
                {
                    var iface = ResolveInterfaceReference(tmpl.ModuleName, composedRef);
                    if (iface == null)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Pass '{pass.Name}' in template '{tmpl.QualifiedName}' composes unknown interface '{composedRef}'.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    if (!slotInterfaceIds.Contains(iface.Id))
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Pass '{pass.Name}' in template '{tmpl.QualifiedName}' composes interface '{iface.QualifiedName}' which was not declared in template slots.",
                            line = 1,
                            column = 1
                        });
                    }

                    pass.ComposedInterfaceIds.Add(iface.Id);
                }
            }
        }
    }

    private void ResolveShaders()
    {
        foreach (var shdr in _shadersById.Values)
        {
            TemplateSymbol? baseTemplate = null;

            if (!string.IsNullOrEmpty(shdr.BaseTemplateQualifiedName))
            {
                baseTemplate = ResolveTemplateReference(shdr.ModuleName, shdr.BaseTemplateQualifiedName);
                if (baseTemplate == null)
                {
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Shader '{shdr.QualifiedName}' derives from unknown template '{shdr.BaseTemplateQualifiedName}'.",
                        line = 1,
                        column = 1
                    });
                }
                else
                {
                    shdr.BaseTemplateQualifiedName = baseTemplate.QualifiedName;
                    shdr.BaseTemplateId = baseTemplate.Id;
                }
            }
            var shaderPropErrors = new List<Ghost.DSL.Parser.DSLShaderError>();
            shdr.PropertySchema = PropertyLayoutEngine.ComputeShaderLayout(shdr.Syntax, shdr.QualifiedName, baseTemplate?.PropertySchema, shaderPropErrors);
            foreach (var err in shaderPropErrors)
            {
                _diagnostics.Add(new DSLShaderError
                {
                    message = err.Message,
                    line = err.Line,
                    column = err.Column
                });
            }


            // Resolve local implementations
            foreach (var localImpl in shdr.LocalImplementations.Values)
            {
                var iface = ResolveInterfaceReference(shdr.ModuleName, localImpl.InterfaceQualifiedName);
                if (iface == null)
                {
                    _diagnostics.Add(new DSLShaderError
                    {
                        message = $"Local implementation '{localImpl.QualifiedName}' implements unknown interface '{localImpl.InterfaceQualifiedName}'.",
                        line = 1,
                        column = 1
                    });
                }
                else
                {
                    localImpl.InterfaceQualifiedName = iface.QualifiedName;
                    localImpl.InterfaceId = iface.Id;
                }
            }

            // Resolve shader bindings
            if (shdr.Syntax.Bind != null)
            {
                foreach (var bind in shdr.Syntax.Bind.Bindings)
                {
                    var iface = ResolveInterfaceReference(shdr.ModuleName, bind.InterfaceName);
                    if (iface == null)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Shader '{shdr.QualifiedName}' binds unknown interface '{bind.InterfaceName}'.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    if (iface.Scope == InterfaceScope.Pipeline)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Shader '{shdr.QualifiedName}' cannot bind pipeline interface '{iface.QualifiedName}'. Pipeline interfaces are configured at runtime.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    if (iface.IsClosed)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Shader '{shdr.QualifiedName}' cannot bind closed interface '{iface.QualifiedName}'.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    var impl = ResolveImplementationReference(shdr.ModuleName, shdr, bind.ImplementationName);
                    if (impl == null)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Shader '{shdr.QualifiedName}' binds unknown implementation '{bind.ImplementationName}' to interface '{iface.QualifiedName}'.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    if (impl.InterfaceId != 0 && impl.InterfaceId != iface.Id)
                    {
                        _diagnostics.Add(new DSLShaderError
                        {
                            message = $"Implementation '{impl.QualifiedName}' implements '{impl.InterfaceQualifiedName}', not target interface '{iface.QualifiedName}'.",
                            line = 1,
                            column = 1
                        });
                        continue;
                    }

                    shdr.Bindings[iface.Id] = impl.Id;
                }
            }
        }
    }

    private void BuildPipelineDomains()
    {
        _packagedPipelineImplementations.Clear();

        foreach (var iface in _interfacesById.Values)
        {
            if (iface.Scope != InterfaceScope.Pipeline)
            {
                continue;
            }

            var candidateList = new List<ImplementationSymbol>();

            foreach (var impl in _implementationsById.Values)
            {
                if (impl.InterfaceId == iface.Id && impl.IsExported)
                {
                    candidateList.Add(impl);
                }
            }

            _packagedPipelineImplementations[iface.Id] = candidateList;
        }
    }

    public InterfaceSymbol? ResolveInterfaceReference(string? currentModuleName, string nameOrRef)
    {
        // 1. Direct qualified match
        if (_interfacesByQualifiedName.TryGetValue(nameOrRef, out var direct))
        {
            return IsSymbolVisibleFrom(currentModuleName, direct.ModuleName, direct.IsExported) ? direct : null;
        }

        // 2. Same module match
        if (currentModuleName != null && _modules.TryGetValue(currentModuleName, out var currentMod))
        {
            if (currentMod.Interfaces.TryGetValue(nameOrRef, out var local))
            {
                return local;
            }

            // 3. Search explicitly imported modules (exported only)
            foreach (var impName in currentMod.Imports)
            {
                if (_modules.TryGetValue(impName, out var impMod) && impMod.Interfaces.TryGetValue(nameOrRef, out var impIface))
                {
                    if (impIface.IsExported)
                    {
                        return impIface;
                    }
                }
            }

            return null;
        }

        // 4. Standalone file match
        return _interfacesById.Values.FirstOrDefault(i => string.Equals(GetUnqualifiedName(i.QualifiedName), nameOrRef, StringComparison.Ordinal) && (i.ModuleName == null || i.IsExported));
    }

    public ImplementationSymbol? ResolveImplementationReference(string? currentModuleName, ShaderSymbol? currentShader, string nameOrRef)
    {
        // 1. Check shader-local implementations
        if (currentShader != null && currentShader.LocalImplementations.TryGetValue(nameOrRef, out var localImpl))
        {
            return localImpl;
        }

        // 2. Direct qualified match
        if (_implementationsByQualifiedName.TryGetValue(nameOrRef, out var direct))
        {
            return IsSymbolVisibleFrom(currentModuleName, direct.ModuleName, direct.IsExported) ? direct : null;
        }

        // 3. Same module match
        if (currentModuleName != null && _modules.TryGetValue(currentModuleName, out var currentMod))
        {
            if (currentMod.Implementations.TryGetValue(nameOrRef, out var local))
            {
                return local;
            }

            // 4. Search explicitly imported modules (exported only)
            foreach (var impName in currentMod.Imports)
            {
                if (_modules.TryGetValue(impName, out var impMod) && impMod.Implementations.TryGetValue(nameOrRef, out var impImpl))
                {
                    if (impImpl.IsExported)
                    {
                        return impImpl;
                    }
                }
            }

            return null;
        }

        // 5. Standalone file match
        return _implementationsById.Values.FirstOrDefault(i => string.Equals(GetUnqualifiedName(i.QualifiedName), nameOrRef, StringComparison.Ordinal) && (i.ModuleName == null || i.IsExported));
    }

    public TemplateSymbol? ResolveTemplateReference(string? currentModuleName, string nameOrRef)
    {
        // 1. Direct qualified match
        if (_templatesByQualifiedName.TryGetValue(nameOrRef, out var direct))
        {
            return IsSymbolVisibleFrom(currentModuleName, direct.ModuleName, direct.IsExported) ? direct : null;
        }

        // 2. Same module match
        if (currentModuleName != null && _modules.TryGetValue(currentModuleName, out var currentMod))
        {
            if (currentMod.Templates.TryGetValue(nameOrRef, out var local))
            {
                return local;
            }

            // 3. Search explicitly imported modules (exported only)
            foreach (var impName in currentMod.Imports)
            {
                if (_modules.TryGetValue(impName, out var impMod) && impMod.Templates.TryGetValue(nameOrRef, out var impTmpl))
                {
                    if (impTmpl.IsExported)
                    {
                        return impTmpl;
                    }
                }
            }

            return null;
        }

        // 4. Standalone file match
        return _templatesById.Values.FirstOrDefault(t => string.Equals(GetUnqualifiedName(t.QualifiedName), nameOrRef, StringComparison.Ordinal) && (t.ModuleName == null || t.IsExported));
    }

    public ShaderSymbol? ResolveShaderReference(string? currentModuleName, string nameOrRef)
    {
        // 1. Direct qualified match
        if (_shadersByQualifiedName.TryGetValue(nameOrRef, out var direct))
        {
            return IsSymbolVisibleFrom(currentModuleName, direct.ModuleName, direct.IsExported) ? direct : null;
        }

        // 2. Same module match
        if (currentModuleName != null && _modules.TryGetValue(currentModuleName, out var currentMod))
        {
            if (currentMod.Shaders.TryGetValue(nameOrRef, out var local))
            {
                return local;
            }

            // 3. Search explicitly imported modules (exported only)
            foreach (var impName in currentMod.Imports)
            {
                if (_modules.TryGetValue(impName, out var impMod) && impMod.Shaders.TryGetValue(nameOrRef, out var impShdr))
                {
                    if (impShdr.IsExported)
                    {
                        return impShdr;
                    }
                }
            }

            return null;
        }

        // 4. Standalone file match
        return _shadersById.Values.FirstOrDefault(s => string.Equals(GetUnqualifiedName(s.QualifiedName), nameOrRef, StringComparison.Ordinal) && (s.ModuleName == null || s.IsExported));
    }

    private bool IsSymbolVisibleFrom(string? requestingModuleName, string? targetModuleName, bool isTargetExported)
    {
        if (targetModuleName == null || string.Equals(requestingModuleName, targetModuleName, StringComparison.Ordinal))
        {
            return true;
        }

        if (!isTargetExported)
        {
            return false;
        }

        if (requestingModuleName != null && _modules.TryGetValue(requestingModuleName, out var reqMod))
        {
            return reqMod.Imports.Contains(targetModuleName);
        }

        return true;
    }

    public IReadOnlyList<ImplementationSymbol> GetPackagedPipelineImplementations(ulong interfaceId)
    {
        return _packagedPipelineImplementations.TryGetValue(interfaceId, out var list)
            ? list
            : Array.Empty<ImplementationSymbol>();
    }

    public Result<ResolvedShaderComposition> ResolveShaderComposition(ShaderSymbol shader)
    {
        return SpecializationResolver.ResolveShaderComposition(this, shader);
    }

    public Result<ResolvedShaderComposition> ResolveShaderComposition(string qualifiedNameOrRef)
    {
        var shader = ResolveShaderReference(null, qualifiedNameOrRef);
        if (shader == null)
        {
            return Result.Failure($"Shader '{qualifiedNameOrRef}' not found in workspace.");
        }
        return SpecializationResolver.ResolveShaderComposition(this, shader);
    }

    private static string QualifyName(string? moduleName, string localName)
    {
        return string.IsNullOrEmpty(moduleName) ? localName : $"{moduleName}.{localName}";
    }

    private static string GetUnqualifiedName(string qualifiedName)
    {
        var dotIdx = qualifiedName.LastIndexOf('.');
        return dotIdx >= 0 ? qualifiedName.Substring(dotIdx + 1) : qualifiedName;
    }

    private Result BuildFailureResult()
    {
        var sb = new StringBuilder("ShaderWorkspace semantic resolution failed with errors:\n");
        foreach (var diag in _diagnostics)
        {
            sb.AppendLine($" - {diag.message}");
        }
        return Result.Failure(sb.ToString());
    }
}
