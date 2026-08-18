using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

public class ComputeShaderVisitor : GhostComputeShaderParserBaseVisitor<object>
{
    public override object VisitComputeFile([NotNull] GhostComputeShaderParser.ComputeFileContext context)
    {
        return VisitCompute(context.compute(0));
    }

    private static string StripQuotes(string text)
    {
        if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }

    public override object VisitCompute([NotNull] GhostComputeShaderParser.ComputeContext context)
    {
        var compute = new ComputeShaderSyntax
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        var computeBody = context.computeBody();
        if (computeBody != null)
        {
            var props = computeBody.propertiesBlock();
            if (props != null && props.Length > 0)
            {
                compute.Properties = (PropertiesBlockSyntax)VisitPropertiesBlock(props[0]);
            }

            var sm = computeBody.shaderModel();
            if (sm != null && sm.Length > 0)
            {
                compute.ShaderModel = sm[0].shaderModelIdentifier().GetText();
            }

            var definesBlock = computeBody.definesBlock();
            if (definesBlock != null && definesBlock.Length > 0)
            {
                compute.Defines = (DefinesBlockSyntax)VisitDefinesBlock(definesBlock[0]);
            }

            var includesBlock = computeBody.includesBlock();
            if (includesBlock != null && includesBlock.Length > 0)
            {
                compute.Includes = (IncludesBlockSyntax)VisitIncludesBlock(includesBlock[0]);
            }

            var keywordsBlock = computeBody.keywordsBlock();
            if (keywordsBlock != null && keywordsBlock.Length > 0)
            {
                compute.Keywords = (KeywordsBlockSyntax)VisitKeywordsBlock(keywordsBlock[0]);
            }

            var hlslBlock = computeBody.hlslBlock();
            if (hlslBlock != null && hlslBlock.Length > 0)
            {
                compute.Hlsl = (HlslBlockSyntax)VisitHlslBlock(hlslBlock[0]);
            }

            foreach (var entry in computeBody.computeEntry())
            {
                compute.ShaderEntries.Add((ShaderEntrySyntax)VisitComputeEntry(entry));
            }
        }

        return compute;
    }

    public override object VisitPropertiesBlock([NotNull] GhostComputeShaderParser.PropertiesBlockContext context)
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
                Name = decl.IDENTIFIER().GetText(),
                ArrayLength = arrayLen,
                Line = decl.Start.Line,
                Column = decl.Start.Column
            });
        }
        return block;
    }

    public override object VisitDefinesBlock([NotNull] GhostComputeShaderParser.DefinesBlockContext context)
    {
        var defines = new DefinesBlockSyntax();
        foreach (var stmt in context.defineStatement())
        {
            defines.Defines.Add(stmt.IDENTIFIER().GetText());
        }
        return defines;
    }

    public override object VisitIncludesBlock([NotNull] GhostComputeShaderParser.IncludesBlockContext context)
    {
        var includes = new IncludesBlockSyntax();
        foreach (var stmt in context.includeStatement())
        {
            includes.Includes.Add(StripQuotes(stmt.STRING_LITERAL().GetText()));
        }
        return includes;
    }

    public override object VisitKeywordsBlock([NotNull] GhostComputeShaderParser.KeywordsBlockContext context)
    {
        var keywords = new KeywordsBlockSyntax();
        foreach (var stmt in context.keywordStatement())
        {
            var group = new KeywordGroupSyntax();
            foreach (var id in stmt.IDENTIFIER())
            {
                group.Keywords.Add(id.GetText());
            }
            keywords.Groups.Add(group);
        }
        return keywords;
    }

    public override object VisitHlslBlock([NotNull] GhostComputeShaderParser.HlslBlockContext context)
    {
        return new HlslBlockSyntax
        {
            Code = ExtractOpaqueBody(context.opaqueBracedBody())
        };
    }

    public override object VisitComputeEntry([NotNull] GhostComputeShaderParser.ComputeEntryContext context)
    {
        return new ShaderEntrySyntax
        {
            EntryType = context.IDENTIFIER().GetText(),
            ShaderPath = StripQuotes(context.STRING_LITERAL(0).GetText()),
            EntryPoint = StripQuotes(context.STRING_LITERAL(1).GetText())
        };
    }

    private static string ExtractOpaqueBody(GhostComputeShaderParser.OpaqueBracedBodyContext context)
    {
        if (context == null) return string.Empty;
        var start = context.Start.StartIndex + 1;
        var stop = context.Stop.StopIndex - 1;
        if (start > stop) return string.Empty;
        var inputStream = context.Start.InputStream;
        return inputStream.GetText(new Antlr4.Runtime.Misc.Interval(start, stop)).Trim();
    }
}
