using Antlr4.Runtime.Misc;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

public class ShaderVisitor : GhostShaderParserBaseVisitor<object>
{
    public override object VisitShaderFile([NotNull] GhostShaderParser.ShaderFileContext context)
    {
        return VisitShader(context.shader(0));
    }

    public override object VisitShader([NotNull] GhostShaderParser.ShaderContext context)
    {
        var shader = new GraphicsShaderSyntax
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        var shaderBody = context.shaderBody();
        if (shaderBody != null)
        {
            shader.ShaderModel = shaderBody.shaderModel()?.GetText() ?? string.Empty;

            foreach (var pipelineBlock in shaderBody.pipelineBlock())
            {
                shader.Pipeline = (PipelineBlockSyntax)VisitPipelineBlock(pipelineBlock);
            }

            foreach (var passBlock in shaderBody.passBlock())
            {
                shader.Passes.Add((PassBlockSyntax)VisitPassBlock(passBlock));
            }

            foreach (var funcCall in shaderBody.functionCall())
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
            var key = statement.IDENTIFIER(0).GetText();
            var value = statement.IDENTIFIER(1).GetText();
            pipeline.Statements[key] = value;
        }

        return pipeline;
    }

    public override object VisitPassBlock([NotNull] GhostShaderParser.PassBlockContext context)
    {
        var pass = new PassBlockSyntax
        {
            Name = StripQuotes(context.STRING_LITERAL().GetText())
        };

        var passBody = context.passBody();
        if (passBody != null)
        {
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
            defines.Defines.Add(defineStmt.IDENTIFIER().GetText());
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
            
            foreach (var identifier in keywordStmt.IDENTIFIER())
            {
                group.Keywords.Add(identifier.GetText());
            }

            keywords.Groups.Add(group);
        }

        return keywords;
    }

    public override object VisitHlslBlock([NotNull] GhostShaderParser.HlslBlockContext context)
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

    public override object VisitShaderEntry([NotNull] GhostShaderParser.ShaderEntryContext context)
    {
        var entry = new ShaderEntrySyntax
        {
            EntryType = context.IDENTIFIER().GetText(),
            ShaderPath = StripQuotes(context.STRING_LITERAL(0).GetText()),
            EntryPoint = StripQuotes(context.STRING_LITERAL(1).GetText())
        };

        return entry;
    }

    public override object VisitFunctionCall([NotNull] GhostShaderParser.FunctionCallContext context)
    {
        var funcCall = new FunctionCallSyntax
        {
            Name = context.IDENTIFIER().GetText()
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

    private static string StripQuotes(string text)
    {
        if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }
}
