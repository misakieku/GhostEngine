using static Ghost.Shader.TokenLexicon;

namespace Ghost.Shader.ParserBlock;

internal class PipelineBlock : IBlockParser<PipelineStateSyntax, PipelineStateModel>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.PIPELINE);
    }

    public static PipelineStateSyntax Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var pipeline = new PipelineStateSyntax();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var stateToken = bodyStream.Expect(TokenType.Identifier);
            bodyStream.Expect(TokenType.Equals);
            var valueToken = bodyStream.Expect(TokenType.Identifier | TokenType.Number);

            switch (stateToken.lexeme)
            {
                case KnownPipelineProperties.ZTEST:
                    pipeline.zTest = valueToken;
                    break;
                case KnownPipelineProperties.ZWRITE:
                    pipeline.zWrite = valueToken;
                    break;
                case KnownPipelineProperties.CULL:
                    pipeline.cull = valueToken;
                    break;
                case KnownPipelineProperties.BLEND:
                    pipeline.blend = valueToken;
                    break;
                case KnownPipelineProperties.COLORMASK:
                    pipeline.colorMask = valueToken;
                    break;
                default:
                    throw new InvalidDataException($"Unknown pipeline state: {stateToken.lexeme}");
            }

            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return pipeline;
    }

    public PipelineStateModel SemanticAnalysis(PipelineStateSyntax syntax, List<ShaderError> errors)
    {
        var model = new PipelineStateModel();

        // ZTest
        if (!syntax.zTest.Match(TokenType.None))
        {
            switch (syntax.zTest.lexeme)
            {
                case "disable":
                    model.zTest = ZTestOptions.Disabled;
                    break;
                case "less":
                    model.zTest = ZTestOptions.Less;
                    break;
                case "less_equal":
                    model.zTest = ZTestOptions.LessEqual;
                    break;
                case "equal":
                    model.zTest = ZTestOptions.Equal;
                    break;
                case "greater_equal":
                    model.zTest = ZTestOptions.GreaterEqual;
                    break;
                case "greater":
                    model.zTest = ZTestOptions.Greater;
                    break;
                case "not_equal":
                    model.zTest = ZTestOptions.NotEqual;
                    break;
                case "always":
                    model.zTest = ZTestOptions.Always;
                    break;
                default:
                    errors.Add(new ShaderError
                    {
                        message = $"Invalid ZTest option: {syntax.zTest.lexeme}",
                        line = syntax.zTest.line,
                        column = syntax.zTest.column
                    });
                    break;
            }
        }
        else
        {
            model.zTest = ZTestOptions.LessEqual;
        }

        // ZWrite
        if (!syntax.zWrite.Match(TokenType.None))
        {
            switch (syntax.zWrite.lexeme)
            {
                case "on":
                    model.zWrite = ZWriteOptions.On;
                    break;
                case "off":
                    model.zWrite = ZWriteOptions.Off;
                    break;
                default:
                    errors.Add(new ShaderError
                    {
                        message = $"Invalid ZWrite option: {syntax.zWrite.lexeme}",
                        line = syntax.zWrite.line,
                        column = syntax.zWrite.column
                    });
                    break;
            }
        }
        else
        {
            model.zWrite = ZWriteOptions.On;
        }

        // Cull
        if (!syntax.cull.Match(TokenType.None))
        {
            switch (syntax.cull.lexeme)
            {
                case "off":
                    model.cull = CullOptions.Off;
                    break;
                case "front":
                    model.cull = CullOptions.Front;
                    break;
                case "back":
                    model.cull = CullOptions.Back;
                    break;
                default:
                    errors.Add(new ShaderError
                    {
                        message = $"Invalid Cull option: {syntax.cull.lexeme}",
                        line = syntax.cull.line,
                        column = syntax.cull.column
                    });
                    break;
            }
        }
        else
        {
            model.cull = CullOptions.Back;
        }

        // Blend
        if (!syntax.blend.Match(TokenType.None))
        {
            switch (syntax.blend.lexeme)
            {
                case "opaque":
                    model.blend = BlendOptions.Opaque;
                    break;
                case "alpha":
                    model.blend = BlendOptions.Alpha;
                    break;
                case "additive":
                    model.blend = BlendOptions.Additive;
                    break;
                case "multiply":
                    model.blend = BlendOptions.Multiply;
                    break;
                case "premultiplied":
                    model.blend = BlendOptions.PremultipliedAlpha;
                    break;
                default:
                    errors.Add(new ShaderError
                    {
                        message = $"Invalid Blend option: {syntax.blend.lexeme}",
                        line = syntax.blend.line,
                        column = syntax.blend.column
                    });
                    break;
            }
        }
        else
        {
            model.blend = BlendOptions.Opaque;
        }

        // Color Mask
        if (!syntax.colorMask.Match(TokenType.None))
        {
            if (uint.TryParse(syntax.colorMask.lexeme, out var colorMask))
            {
                model.colorMask = colorMask;
            }
            else
            {
                errors.Add(new ShaderError
                {
                    message = $"Invalid Color Mask value: {syntax.colorMask.lexeme}",
                    line = syntax.colorMask.line,
                    column = syntax.colorMask.column
                });
            }
        }
        else
        {
            model.colorMask = 0xF; // Default to RGBA
        }

        return model;
    }
}
