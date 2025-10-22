using Ghost.Core.Graphics;

namespace Ghost.Shader.Compiler.Parser;

internal class PipelineBlock : IBlockParser<PipelineSyntax, PipelineSemantic>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.PIPELINE);
    }

    public static PipelineSyntax Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var pipeline = new PipelineSyntax();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var stateToken = bodyStream.Expect(TokenType.Identifier);
            bodyStream.Expect(TokenType.Equals);
            var valueToken = bodyStream.Expect(TokenType.Identifier | TokenType.Number);

            pipeline.values ??= new();
            pipeline.values.Add(new ValueDeclaration
            {
                name = stateToken,
                value = valueToken
            });

            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return pipeline;
    }

    public static PipelineSemantic? SemanticAnalysis(PipelineSyntax? syntax, List<ShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var semantic = new PipelineSemantic();
        if (syntax.values != null)
        {
            foreach (var valueDecl in syntax.values)
            {
                switch (valueDecl.name.lexeme)
                {
                    case TokenLexicon.KnownPipelineProperties.ZTEST:
                        switch (valueDecl.value.lexeme)
                        {
                            case "disable":
                                semantic.zTest = ZTestOptions.Disabled;
                                break;
                            case "less":
                                semantic.zTest = ZTestOptions.Less;
                                break;
                            case "less_equal":
                                semantic.zTest = ZTestOptions.LessEqual;
                                break;
                            case "equal":
                                semantic.zTest = ZTestOptions.Equal;
                                break;
                            case "greater_equal":
                                semantic.zTest = ZTestOptions.GreaterEqual;
                                break;
                            case "greater":
                                semantic.zTest = ZTestOptions.Greater;
                                break;
                            case "not_equal":
                                semantic.zTest = ZTestOptions.NotEqual;
                                break;
                            case "always":
                                semantic.zTest = ZTestOptions.Always;
                                break;
                            default:
                                errors.Add(new ShaderError
                                {
                                    message = $"Invalid ZTest option: {valueDecl.value.lexeme}",
                                    line = valueDecl.value.line,
                                    column = valueDecl.value.column
                                });
                                break;
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.ZWRITE:
                        switch (valueDecl.value.lexeme)
                        {
                            case "on":
                                semantic.zWrite = ZWriteOptions.On;
                                break;
                            case "off":
                                semantic.zWrite = ZWriteOptions.Off;
                                break;
                            default:
                                errors.Add(new ShaderError
                                {
                                    message = $"Invalid ZWrite option: {valueDecl.value.lexeme}",
                                    line = valueDecl.value.line,
                                    column = valueDecl.value.column
                                });
                                break;
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.CULL:
                        switch (valueDecl.value.lexeme)
                        {
                            case "off":
                                semantic.cull = CullOptions.Off;
                                break;
                            case "front":
                                semantic.cull = CullOptions.Front;
                                break;
                            case "back":
                                semantic.cull = CullOptions.Back;
                                break;
                            default:
                                errors.Add(new ShaderError
                                {
                                    message = $"Invalid Cull option: {valueDecl.value.lexeme}",
                                    line = valueDecl.value.line,
                                    column = valueDecl.value.column
                                });
                                break;
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.BLEND:
                        switch (valueDecl.value.lexeme)
                        {
                            case "opaque":
                                semantic.blend = BlendOptions.Opaque;
                                break;
                            case "alpha":
                                semantic.blend = BlendOptions.Alpha;
                                break;
                            case "additive":
                                semantic.blend = BlendOptions.Additive;
                                break;
                            case "multiply":
                                semantic.blend = BlendOptions.Multiply;
                                break;
                            case "premultiplied":
                                semantic.blend = BlendOptions.PremultipliedAlpha;
                                break;
                            default:
                                errors.Add(new ShaderError
                                {
                                    message = $"Invalid Blend option: {valueDecl.value.lexeme}",
                                    line = valueDecl.value.line,
                                    column = valueDecl.value.column
                                });
                                break;
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.COLORMASK:
                        if (uint.TryParse(valueDecl.value.lexeme, out var colorMask))
                        {
                            semantic.colorMask = colorMask;
                        }
                        else
                        {
                            errors.Add(new ShaderError
                            {
                                message = $"Invalid Color Mask value: {valueDecl.value.lexeme}",
                                line = valueDecl.value.line,
                                column = valueDecl.value.column
                            });
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        return semantic;
    }
}
