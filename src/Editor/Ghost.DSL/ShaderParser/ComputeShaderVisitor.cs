using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Model;
using TerraFX.Interop.Windows;

namespace Ghost.DSL.ShaderParser;

internal class ComputeShaderVisitor : GhostComputeShaderParserBaseVisitor<object>
{
    public List<ComputeShaderModel> ComputeShaders { get; } = new();

    public override object VisitComputeFile([NotNull] GhostComputeShaderParser.ComputeFileContext context)
    {
        foreach (var shaderContext in context.compute())
        {
            var shader = (ComputeShaderModel)VisitCompute(shaderContext);
            ComputeShaders.Add(shader);
        }

        return ComputeShaders;
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
        var compute = new ComputeShaderModel
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        var computeBody = context.computeBody();
        if (computeBody != null)
        {
            compute.ShaderModel = computeBody.shaderModel()?.GetText() ?? string.Empty;

            foreach (var definesBlock in computeBody.definesBlock())
            {
                compute.Defines = (DefinesBlockModel)VisitDefinesBlock(definesBlock);
            }

            foreach (var includesBlock in computeBody.includesBlock())
            {
                compute.Includes = (IncludesBlockModel)VisitIncludesBlock(includesBlock);
            }

            foreach (var keywordsBlock in computeBody.keywordsBlock())
            {
                compute.Keywords = (KeywordsBlockModel)VisitKeywordsBlock(keywordsBlock);
            }

            var hlslBlock = computeBody.hlslBlock().FirstOrDefault();
            if (hlslBlock != null)
            {
                compute.Hlsl = (HlslBlockModel)VisitHlslBlock(hlslBlock);
            }

            foreach (var computeEntry in computeBody.computeEntry())
            {
                compute.ShaderEntries.Add((ShaderEntryModel)VisitComputeEntry(computeEntry));
            }
        }

        return compute;
    }

    public override object VisitDefinesBlock([NotNull] GhostComputeShaderParser.DefinesBlockContext context)
    {
        var defines = new DefinesBlockModel();

        foreach (var defineStmt in context.defineStatement())
        {
            defines.Defines.Add(defineStmt.IDENTIFIER().GetText());
        }

        return defines;
    }

    public override object VisitIncludesBlock([NotNull] GhostComputeShaderParser.IncludesBlockContext context)
    {
        var includes = new IncludesBlockModel();

        foreach (var includeStmt in context.includeStatement())
        {
            includes.Includes.Add(StripQuotes(includeStmt.STRING_LITERAL().GetText()));
        }

        return includes;
    }

    public override object VisitKeywordsBlock([NotNull] GhostComputeShaderParser.KeywordsBlockContext context)
    {
        var keywords = new KeywordsBlockModel();

        foreach (var keywordStmt in context.keywordStatement())
        {
            var group = new KeywordGroupModel();

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
        var hlsl = new HlslBlockModel();

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
        var entry = new ShaderEntryModel
        {
            EntryType = context.IDENTIFIER().GetText(),
            ShaderPath = StripQuotes(context.STRING_LITERAL(0).GetText()),
            EntryPoint = StripQuotes(context.STRING_LITERAL(1).GetText())
        };

        return entry;
    }
}
