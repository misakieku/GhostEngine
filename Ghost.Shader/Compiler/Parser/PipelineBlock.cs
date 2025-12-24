using Ghost.Core.Graphics;

namespace Ghost.SDL.Compiler.Parser;

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

    public static PipelineSemantic? SemanticAnalysis(PipelineSyntax? syntax, List<SDLError> errors)
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
                        if (Enum.TryParse<ZTest>(valueDecl.value.lexeme, true, out var zTest))
                        {
                            semantic.zTest = zTest;
                        }
                        else
                        {
                            errors.Add(new SDLError
                            {
                                message = $"Invalid ZTest option: {valueDecl.value.lexeme}",
                                line = valueDecl.value.line,
                                column = valueDecl.value.column
                            });
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.ZWRITE:
                        if (Enum.TryParse<ZWrite>(valueDecl.value.lexeme, true, out var zWrite))
                        {
                            semantic.zWrite = zWrite;
                        }
                        else
                        {
                            errors.Add(new SDLError
                            {
                                message = $"Invalid ZWrite option: {valueDecl.value.lexeme}",
                                line = valueDecl.value.line,
                                column = valueDecl.value.column
                            });
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.CULL:
                        if (Enum.TryParse<Cull>(valueDecl.value.lexeme, true, out var cull))
                        {
                            semantic.cull = cull;
                        }
                        else
                        {
                            errors.Add(new SDLError
                            {
                                message = $"Invalid Cull option: {valueDecl.value.lexeme}",
                                line = valueDecl.value.line,
                                column = valueDecl.value.column
                            });
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.BLEND:
                        if (Enum.TryParse<Blend>(valueDecl.value.lexeme, true, out var blend))
                        {
                            semantic.blend = blend;
                        }
                        else
                        {
                            errors.Add(new SDLError
                            {
                                message = $"Invalid Blend option: {valueDecl.value.lexeme}",
                                line = valueDecl.value.line,
                                column = valueDecl.value.column
                            });
                        }
                        break;

                    case TokenLexicon.KnownPipelineProperties.COLORMASK:
                        if (Enum.TryParse<ColorWriteMask>(valueDecl.value.lexeme, true, out var colorMask))
                        {
                            semantic.colorMask = colorMask;
                        }
                        else
                        {
                            errors.Add(new SDLError
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
