using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

public class ShaderVisitor : GhostShaderParserBaseVisitor<object>
{
    public override object VisitShaderFile([NotNull] GhostShaderParser.ShaderFileContext context)
    {
        var doc = new DSLDocumentSyntax();

        foreach (var topLevel in context.topLevelDeclaration())
        {
            ProcessTopLevelDeclaration(topLevel, doc);
        }

        foreach (var moduleCtx in context.moduleDeclaration())
        {
            var module = (ModuleDeclarationSyntax)VisitModuleDeclaration(moduleCtx);
            doc.Modules.Add(module);
        }

        foreach (var projCtx in context.shaderProjectDeclaration())
        {
            var proj = (ShaderProjectDeclarationSyntax)VisitShaderProjectDeclaration(projCtx);
            doc.Projects.Add(proj);
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
        var scope = context.interfaceScope().PIPELINE() != null
            ? InterfaceScope.Pipeline
            : InterfaceScope.Shader;

        return new InterfaceDeclarationSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier()),
            Scope = scope,
            IsClosed = context.CLOSED() != null,
            IsExported = context.EXPORT() != null,
            Body = context.opaqueBracedBody() != null ? ExtractOpaqueBody(context.opaqueBracedBody()) : string.Empty
        };
    }

    public override object VisitImplementationDeclaration([NotNull] GhostShaderParser.ImplementationDeclarationContext context)
    {
        var body = ExtractOpaqueBody(context.opaqueBracedBody());
        var (cleanedBody, provider) = ExtractProviderFromBody(body);

        return new ImplementationDeclarationSyntax
        {
            Name = GetQualifiedIdentifierText(context.qualifiedIdentifier(0)),
            InterfaceName = GetQualifiedIdentifierText(context.qualifiedIdentifier(1)),
            IsExported = context.EXPORT() != null,
            Body = cleanedBody,
            Provider = provider
        };
    }

    private static (string cleanedBody, string? provider) ExtractProviderFromBody(string body)
    {
        var match = System.Text.RegularExpressions.Regex.Match(body, @"provider\s*=\s*""([^""]+)""\s*;");
        if (match.Success)
        {
            var provider = match.Groups[1].Value;
            var cleaned = body.Remove(match.Index, match.Length);
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
            var props = body.propertiesBlock();
            if (props != null && props.Length > 0)
            {
                template.Properties = (PropertiesBlockSyntax)VisitPropertiesBlock(props[0]);
            }

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
            var props = body.propertiesBlock();
            if (props != null && props.Length > 0)
            {
                shader.Properties = (PropertiesBlockSyntax)VisitPropertiesBlock(props[0]);
            }

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

    public override object VisitPropertiesBlock([NotNull] GhostShaderParser.PropertiesBlockContext context)
    {
        var block = new PropertiesBlockSyntax();
        foreach (var decl in context.propertyDeclaration())
        {
            int arrayLen = 0;
            if (decl.NUMBER() != null)
            {
                int.TryParse(decl.NUMBER().GetText(), out arrayLen);
            }
            block.Declarations.Add(new PropertyDeclarationSyntax
            {
                TypeName = decl.propertyType().GetText(),
                Name = decl.identifier().GetText(),
                ArrayLength = arrayLen,
                Line = decl.Start.Line,
                Column = decl.Start.Column
            });
        }
        return block;
    }

    public override object VisitPipelineBlock([NotNull] GhostShaderParser.PipelineBlockContext context)
    {
        var pipeline = new PipelineBlockSyntax();
        foreach (var stmt in context.pipelineStatement())
        {
            pipeline.Statements[stmt.identifier(0).GetText()] = stmt.identifier(1).GetText();
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
            var composeBlock = passBody.composeBlock();
            if (composeBlock != null && composeBlock.Length > 0)
            {
                var composeSyntax = new ComposeBlockSyntax();
                foreach (var item in composeBlock[0].composeItem())
                {
                    composeSyntax.Interfaces.Add(GetQualifiedIdentifierText(item.qualifiedIdentifier()));
                }
                pass.Compose = composeSyntax;
            }

            var definesBlock = passBody.definesBlock();
            if (definesBlock != null && definesBlock.Length > 0)
            {
                pass.Defines = (DefinesBlockSyntax)VisitDefinesBlock(definesBlock[0]);
            }

            var includesBlock = passBody.includesBlock();
            if (includesBlock != null && includesBlock.Length > 0)
            {
                pass.Includes = (IncludesBlockSyntax)VisitIncludesBlock(includesBlock[0]);
            }

            var keywordsBlock = passBody.keywordsBlock();
            if (keywordsBlock != null && keywordsBlock.Length > 0)
            {
                pass.Keywords = (KeywordsBlockSyntax)VisitKeywordsBlock(keywordsBlock[0]);
            }

            var hlslBlock = passBody.hlslBlock();
            if (hlslBlock != null && hlslBlock.Length > 0)
            {
                pass.Hlsl = (HlslBlockSyntax)VisitHlslBlock(hlslBlock[0]);
            }

            var pipelineBlock = passBody.pipelineBlock();
            if (pipelineBlock != null && pipelineBlock.Length > 0)
            {
                pass.LocalPipeline = (PipelineBlockSyntax)VisitPipelineBlock(pipelineBlock[0]);
            }

            foreach (var entry in passBody.shaderEntry())
            {
                pass.ShaderEntries.Add((ShaderEntrySyntax)VisitShaderEntry(entry));
            }
        }

        return pass;
    }

    public override object VisitDefinesBlock([NotNull] GhostShaderParser.DefinesBlockContext context)
    {
        var defines = new DefinesBlockSyntax();
        foreach (var stmt in context.defineStatement())
        {
            defines.Defines.Add(stmt.identifier().GetText());
        }
        return defines;
    }

    public override object VisitIncludesBlock([NotNull] GhostShaderParser.IncludesBlockContext context)
    {
        var includes = new IncludesBlockSyntax();
        foreach (var stmt in context.includeStatement())
        {
            includes.Includes.Add(StripQuotes(stmt.STRING_LITERAL().GetText()));
        }
        return includes;
    }

    public override object VisitKeywordsBlock([NotNull] GhostShaderParser.KeywordsBlockContext context)
    {
        var keywords = new KeywordsBlockSyntax();
        foreach (var stmt in context.keywordStatement())
        {
            var group = new KeywordGroupSyntax();
            foreach (var id in stmt.identifier())
            {
                group.Keywords.Add(id.GetText());
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
        var call = new FunctionCallSyntax
        {
            Name = context.identifier().GetText()
        };

        var args = context.functionArguments();
        if (args != null)
        {
            foreach (var arg in args.functionArgument())
            {
                if (arg.STRING_LITERAL() != null)
                {
                    call.Arguments.Add(StripQuotes(arg.STRING_LITERAL().GetText()));
                }
                else if (arg.NUMBER() != null)
                {
                    call.Arguments.Add(arg.NUMBER().GetText());
                }
                else if (arg.qualifiedIdentifier() != null)
                {
                    call.Arguments.Add(GetQualifiedIdentifierText(arg.qualifiedIdentifier()));
                }
            }
        }

        return call;
    }

    private static string ExtractOpaqueBody(GhostShaderParser.OpaqueBracedBodyContext context)
    {
        if (context == null) return string.Empty;
        var start = context.Start.StartIndex + 1;
        var stop = context.Stop.StopIndex - 1;
        if (start > stop) return string.Empty;
        var inputStream = context.Start.InputStream;
        return inputStream.GetText(new Antlr4.Runtime.Misc.Interval(start, stop)).Trim();
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
        if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }
}
