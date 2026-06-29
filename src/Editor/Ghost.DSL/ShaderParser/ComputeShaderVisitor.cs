using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

internal class ComputeShaderVisitor : GhostComputeShaderParserBaseVisitor<object>
{
    public override object VisitComputeFile([NotNull] GhostComputeShaderParser.ComputeFileContext context)
    {
        return VisitCompute(context.compute(0));
    }

    private static string StripQuotes(string text)
    {
        if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
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
            compute.ShaderModel = computeBody.shaderModel()?.GetText() ?? string.Empty;

            foreach (var definesBlock in computeBody.definesBlock())
            {
                compute.Defines = (DefinesBlockSyntax)VisitDefinesBlock(definesBlock);
            }

            foreach (var includesBlock in computeBody.includesBlock())
            {
                compute.Includes = (IncludesBlockSyntax)VisitIncludesBlock(includesBlock);
            }

            foreach (var keywordsBlock in computeBody.keywordsBlock())
            {
                compute.Keywords = (KeywordsBlockSyntax)VisitKeywordsBlock(keywordsBlock);
            }

            var hlslBlock = computeBody.hlslBlock().FirstOrDefault();
            if (hlslBlock != null)
            {
                compute.Hlsl = (HlslBlockSyntax)VisitHlslBlock(hlslBlock);
            }

            foreach (var computeEntry in computeBody.computeEntry())
            {
                compute.ShaderEntries.Add((ShaderEntrySyntax)VisitComputeEntry(computeEntry));
            }
        }

        return compute;
    }

    public override object VisitDefinesBlock([NotNull] GhostComputeShaderParser.DefinesBlockContext context)
    {
        var defines = new DefinesBlockSyntax();

        foreach (var defineStmt in context.defineStatement())
        {
            defines.Defines.Add(defineStmt.IDENTIFIER().GetText());
        }

        return defines;
    }

    public override object VisitIncludesBlock([NotNull] GhostComputeShaderParser.IncludesBlockContext context)
    {
        var includes = new IncludesBlockSyntax();

        foreach (var includeStmt in context.includeStatement())
        {
            includes.Includes.Add(StripQuotes(includeStmt.STRING_LITERAL().GetText()));
        }

        return includes;
    }

    public override object VisitKeywordsBlock([NotNull] GhostComputeShaderParser.KeywordsBlockContext context)
    {
        var keywords = new KeywordsBlockSyntax();

        foreach (var keywordStmt in context.keywordStatement())
        {
            var group = new KeywordGroupSyntax();

            if (keywordStmt.scope() != null)
            {
                group.Scope = keywordStmt.scope().GetText();
            }

            foreach (var identifier in keywordStmt.IDENTIFIER())
            {
                group.Keywords.Add(identifier.GetText());
            }

            keywords.Groups.Add(group);
        }

        return keywords;
    }

    public override object VisitHlslBlock([NotNull] GhostComputeShaderParser.HlslBlockContext context)
    {
        var hlsl = new HlslBlockSyntax();

        // Get the text between the braces
        var start = context.LBRACE().Symbol.StopIndex + 1;
        var stop = context.RBRACE().Symbol.StartIndex - 1;

        if (stop >= start)
        {
            var input = context.Start.InputStream;
            hlsl.Code = input.GetText(new Interval(start, stop));
        }

        return hlsl;
    }

    public override object VisitComputeEntry([NotNull] GhostComputeShaderParser.ComputeEntryContext context)
    {
        var entry = new ShaderEntrySyntax
        {
            EntryType = context.IDENTIFIER().GetText(),
            ShaderPath = StripQuotes(context.STRING_LITERAL(0).GetText()),
            EntryPoint = StripQuotes(context.STRING_LITERAL(1).GetText())
        };

        return entry;
    }
}
