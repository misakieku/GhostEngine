using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

public class ShaderVisitor : GhostShaderParserBaseVisitor<object>
{
    public override object VisitShaderFile([NotNull] GhostShaderParser.ShaderFileContext context)
    {
        var doc = new DSLDocumentSyntax();

        foreach (var child in context.children)
        {
            if (child is GhostShaderParser.ModuleDeclarationContext modCtx)
            {
                doc.Modules.Add((ModuleDeclarationSyntax)VisitModuleDeclaration(modCtx));
            }
            else if (child is GhostShaderParser.ShaderProjectDeclarationContext projCtx)
            {
                doc.Projects.Add((ShaderProjectDeclarationSyntax)VisitShaderProjectDeclaration(projCtx));
            }
            else if (child is GhostShaderParser.TopLevelDeclarationContext topCtx)
            {
                ProcessTopLevelDeclaration(topCtx, doc);
            }
        }

        return doc;
    }

    private void ProcessTopLevelDeclaration(GhostShaderParser.TopLevelDeclarationContext context, DSLDocumentSyntax doc)
    {
        if (context.importDeclaration() != null)
        {
            doc.Imports.Add((ImportDeclarationSyntax)VisitImportDeclaration(context.importDeclaration()));
        }
        else if (context.interfaceDeclaration() != null)
        {
            doc.Interfaces.Add((InterfaceDeclarationSyntax)VisitInterfaceDeclaration(context.interfaceDeclaration()));
        }
        else if (context.implementationDeclaration() != null)
        {
            doc.Implementations.Add((ImplementationDeclarationSyntax)VisitImplementationDeclaration(context.implementationDeclaration()));
        }
        else if (context.templateDeclaration() != null)
        {
            doc.Templates.Add((TemplateDeclarationSyntax)VisitTemplateDeclaration(context.templateDeclaration()));
        }
        else if (context.shaderDeclaration() != null)
        {
            doc.Shaders.Add((ShaderDeclarationSyntax)VisitShaderDeclaration(context.shaderDeclaration()));
        }
        else if (context.passBlock() != null)
        {
            doc.Passes.Add((PassBlockSyntax)VisitPassBlock(context.passBlock()));
        }
    }

    public override object VisitModuleDeclaration([NotNull] GhostShaderParser.ModuleDeclarationContext context)
    {
        var module = new ModuleDeclarationSyntax
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        foreach (var item in context.moduleItem())
        {
            if (item.importDeclaration() != null)
            {
                module.Imports.Add((ImportDeclarationSyntax)VisitImportDeclaration(item.importDeclaration()));
            }
            else if (item.interfaceDeclaration() != null)
            {
                module.Interfaces.Add((InterfaceDeclarationSyntax)VisitInterfaceDeclaration(item.interfaceDeclaration()));
            }
            else if (item.implementationDeclaration() != null)
            {
                module.Implementations.Add((ImplementationDeclarationSyntax)VisitImplementationDeclaration(item.implementationDeclaration()));
            }
            else if (item.templateDeclaration() != null)
            {
                module.Templates.Add((TemplateDeclarationSyntax)VisitTemplateDeclaration(item.templateDeclaration()));
            }
            else if (item.shaderDeclaration() != null)
            {
                module.Shaders.Add((ShaderDeclarationSyntax)VisitShaderDeclaration(item.shaderDeclaration()));
            }
        }

        return module;
    }

    public override object VisitShaderProjectDeclaration([NotNull] GhostShaderParser.ShaderProjectDeclarationContext context)
    {
        var proj = new ShaderProjectDeclarationSyntax
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        foreach (var item in context.projectItem())
        {
            if (item.MODULE() != null)
            {
                proj.Modules.Add(StripQuotes(item.STRING_LITERAL().GetText()));
            }
            else if (item.TARGET() != null)
            {
                proj.Targets.Add(StripQuotes(item.STRING_LITERAL().GetText()));
            }
        }

        return proj;
    }

    public override object VisitImportDeclaration([NotNull] GhostShaderParser.ImportDeclarationContext context)
    {
        return new ImportDeclarationSyntax
        {
            ModuleName = StripQuotes(context.STRING_LITERAL().GetText())
        };
    }

    public override object VisitInterfaceDeclaration([NotNull] GhostShaderParser.InterfaceDeclarationContext context)
    {
        var iface = new InterfaceDeclarationSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier()),
            IsExported = context.EXPORT() != null,
            IsClosed = context.CLOSED() != null,
            Scope = context.interfaceScope().PIPELINE() != null ? InterfaceScope.Pipeline : InterfaceScope.Shader
        };

        if (context.opaqueBracedBody() != null)
        {
            iface.Body = ExtractOpaqueBody(context.opaqueBracedBody());
        }

        return iface;
    }

    public override object VisitImplementationDeclaration([NotNull] GhostShaderParser.ImplementationDeclarationContext context)
    {
        var rawBody = ExtractOpaqueBody(context.opaqueBracedBody());
        var (cleanedBody, provider) = ExtractProviderFromBody(rawBody);

        var impl = new ImplementationDeclarationSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier(0)),
            InterfaceName = GetQualifiedIdentifierText(context.qualifiedIdentifier(1)),
            IsExported = context.EXPORT() != null,
            Provider = provider,
            Body = cleanedBody
        };

        return impl;
    }

    private static (string cleanedBody, string? provider) ExtractProviderFromBody(string body)
    {
        var match = System.Text.RegularExpressions.Regex.Match(body, @"provider\s*=\s*""([^""]+)""\s*;");
        if (match.Success)
        {
            var provider = match.Groups[1].Value;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(body, @"provider\s*=\s*""[^""]+""\s*;\r?\n?", string.Empty);
            return (cleaned, provider);
        }
        return (body, null);
    }

    public override object VisitTemplateDeclaration([NotNull] GhostShaderParser.TemplateDeclarationContext context)
    {
        var template = new TemplateDeclarationSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier()),
            IsExported = context.EXPORT() != null
        };
        var body = context.templateBody();
        if (body != null)
        {
            foreach (var slotBlock in body.slotBlock())
            {
                foreach (var slotItem in slotBlock.slotItem())
                {
                    template.Slots.Add(new TemplateSlotSyntax
                    {
                        InterfaceName = GetQualifiedIdentifierText(slotItem.qualifiedIdentifier(0)),
                        DefaultImplementationName = slotItem.qualifiedIdentifier().Length > 1
                            ? GetQualifiedIdentifierText(slotItem.qualifiedIdentifier(1))
                            : null
                    });
                }
            }

            foreach (var passBlock in body.passBlock())
            {
                template.Passes.Add((PassBlockSyntax)VisitPassBlock(passBlock));
            }

            foreach (var pipelineBlock in body.pipelineBlock())
            {
                template.Pipeline = (PipelineBlockSyntax)VisitPipelineBlock(pipelineBlock);
            }

            var sm = body.shaderModel();
            if (sm != null && sm.Length > 0)
            {
                template.ShaderModel = sm[0].shaderModelIdentifier().GetText();
            }

            foreach (var funcCall in body.functionCall())
            {
                template.FunctionCalls.Add((FunctionCallSyntax)VisitFunctionCall(funcCall));
            }
        }

        return template;
    }

    public override object VisitShaderDeclaration([NotNull] GhostShaderParser.ShaderDeclarationContext context)
    {
        var shaderName = GetQualifiedIdentifierText(context.qualifiedIdentifier(0));
        string? templateName = context.COLON() != null && context.qualifiedIdentifier().Length > 1
            ? GetQualifiedIdentifierText(context.qualifiedIdentifier(1))
            : null;

        var shader = new ShaderDeclarationSyntax
        {
            Name = shaderName,
            TemplateName = templateName,
            IsExported = context.EXPORT() != null
        };
        var body = context.shaderBody();
        if (body != null)
        {
            var payloadBlock = body.payloadBlock();
            if (payloadBlock != null && payloadBlock.Length > 0)
            {
                shader.Payload = new PayloadBlockSyntax
                {
                    Body = ExtractOpaqueBody(payloadBlock[0].opaqueBracedBody())
                };
            }

            foreach (var impl in body.implementationDeclaration())
            {
                shader.Implementations.Add((ImplementationDeclarationSyntax)VisitImplementationDeclaration(impl));
            }

            foreach (var bindBlock in body.bindBlock())
            {
                var bindSyntax = new BindBlockSyntax();
                foreach (var item in bindBlock.bindItem())
                {
                    bindSyntax.Bindings.Add(new BindingSyntax
                    {
                        InterfaceName = GetQualifiedIdentifierText(item.qualifiedIdentifier(0)),
                        ImplementationName = GetQualifiedIdentifierText(item.qualifiedIdentifier(1))
                    });
                }
                shader.Bind = bindSyntax;
            }

            foreach (var passBlock in body.passBlock())
            {
                shader.Passes.Add((PassBlockSyntax)VisitPassBlock(passBlock));
            }

            foreach (var pipelineBlock in body.pipelineBlock())
            {
                shader.Pipeline = (PipelineBlockSyntax)VisitPipelineBlock(pipelineBlock);
            }

            var sm = body.shaderModel();
            if (sm != null && sm.Length > 0)
            {
                shader.ShaderModel = sm[0].shaderModelIdentifier().GetText();
            }

            foreach (var funcCall in body.functionCall())
            {
                shader.FunctionCalls.Add((FunctionCallSyntax)VisitFunctionCall(funcCall));
            }
        }

        return shader;
    }


    public override object VisitPipelineBlock([NotNull] GhostShaderParser.PipelineBlockContext context)
    {
        var pipeline = new PipelineBlockSyntax();

        foreach (var statement in context.pipelineStatement())
        {
            var key = statement.identifier(0).GetText();
            var value = statement.identifier(1).GetText();
            pipeline.Statements[key] = value;
        }

        return pipeline;
    }

    public override object VisitPassBlock([NotNull] GhostShaderParser.PassBlockContext context)
    {
        var pass = new PassBlockSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier())
        };

        var passBody = context.passBody();
        if (passBody != null)
        {
            foreach (var composeBlock in passBody.composeBlock())
            {
                var compose = new ComposeBlockSyntax();
                foreach (var item in composeBlock.composeItem())
                {
                    compose.Interfaces.Add(GetQualifiedIdentifierText(item.qualifiedIdentifier()));
                }
                pass.Compose = compose;
            }

            foreach (var definesBlock in passBody.definesBlock())
            {
                pass.Defines = (DefinesBlockSyntax)VisitDefinesBlock(definesBlock);
            }

            foreach (var includesBlock in passBody.includesBlock())
            {
                pass.Includes = (IncludesBlockSyntax)VisitIncludesBlock(includesBlock);
            }

            foreach (var keywordsBlock in passBody.keywordsBlock())
            {
                pass.Keywords = (KeywordsBlockSyntax)VisitKeywordsBlock(keywordsBlock);
            }

            foreach (var pipelineBlock in passBody.pipelineBlock())
            {
                pass.LocalPipeline = (PipelineBlockSyntax)VisitPipelineBlock(pipelineBlock);
            }

            foreach (var hlslBlock in passBody.hlslBlock())
            {
                pass.Hlsl = (HlslBlockSyntax)VisitHlslBlock(hlslBlock);
            }

            foreach (var shaderEntry in passBody.shaderEntry())
            {
                pass.ShaderEntries.Add((ShaderEntrySyntax)VisitShaderEntry(shaderEntry));
            }
        }

        return pass;
    }

    public override object VisitDefinesBlock([NotNull] GhostShaderParser.DefinesBlockContext context)
    {
        var defines = new DefinesBlockSyntax();

        foreach (var defineStmt in context.defineStatement())
        {
            defines.Defines.Add(defineStmt.identifier().GetText());
        }

        return defines;
    }

    public override object VisitIncludesBlock([NotNull] GhostShaderParser.IncludesBlockContext context)
    {
        var includes = new IncludesBlockSyntax();

        foreach (var includeStmt in context.includeStatement())
        {
            includes.Includes.Add(StripQuotes(includeStmt.STRING_LITERAL().GetText()));
        }

        return includes;
    }

    public override object VisitKeywordsBlock([NotNull] GhostShaderParser.KeywordsBlockContext context)
    {
        var keywords = new KeywordsBlockSyntax();

        foreach (var keywordStmt in context.keywordStatement())
        {
            var group = new KeywordGroupSyntax();

            foreach (var identifier in keywordStmt.identifier())
            {
                group.Keywords.Add(identifier.GetText());
            }

            keywords.Groups.Add(group);
        }

        return keywords;
    }

    public override object VisitHlslBlock([NotNull] GhostShaderParser.HlslBlockContext context)
    {
        return new HlslBlockSyntax
        {
            Code = ExtractOpaqueBody(context.opaqueBracedBody())
        };
    }

    public override object VisitShaderEntry([NotNull] GhostShaderParser.ShaderEntryContext context)
    {
        return new ShaderEntrySyntax
        {
            EntryType = context.identifier().GetText(),
            ShaderPath = StripQuotes(context.STRING_LITERAL(0).GetText()),
            EntryPoint = StripQuotes(context.STRING_LITERAL(1).GetText())
        };
    }

    public override object VisitFunctionCall([NotNull] GhostShaderParser.FunctionCallContext context)
    {
        var funcCall = new FunctionCallSyntax
        {
            Name = context.identifier().GetText()
        };

        if (context.functionArguments() != null)
        {
            foreach (var arg in context.functionArguments().functionArgument())
            {
                var text = arg.GetText();
                if (text.StartsWith('"'))
                {
                    text = StripQuotes(text);
                }
                funcCall.Arguments.Add(text);
            }
        }

        return funcCall;
    }

    private static string ExtractOpaqueBody(GhostShaderParser.OpaqueBracedBodyContext context)
    {
        var start = context.LBRACE().Symbol.StopIndex + 1;
        var stop = context.RBRACE().Symbol.StartIndex - 1;

        if (stop >= start)
        {
            var input = context.Start.InputStream;
            return input.GetText(new Interval(start, stop));
        }

        return string.Empty;
    }

    private static string GetQualifiedIdentifierText(GhostShaderParser.QualifiedIdentifierContext context)
    {
        if (context.STRING_LITERAL() != null)
        {
            return StripQuotes(context.STRING_LITERAL().GetText());
        }

        return context.GetText();
    }

    private static string StripQuotes(string text)
    {
        if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }
}
