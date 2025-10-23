namespace Ghost.Shader.Compiler.Parser;

internal class ShaderBlock : IBlockParser<ShaderSyntax, ShaderSemantics>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.SHADER);
    }

    public static ShaderSyntax Parse(TokenStreamSlice stream)
    {
        var shader = new ShaderSyntax();

        stream.Expect(TokenType.Keyword);
        shader.name = stream.Expect(TokenType.StringLiteral);
        stream.Expect(TokenType.LBrace);

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.TryPeek(out var nextToken))
        {
            if (PropertiesBlock.ShouldEnter(nextToken))
            {
                shader.properties = PropertiesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PipelineBlock.ShouldEnter(nextToken))
            {
                shader.pipeline = PipelineBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PassBlock.ShouldEnter(nextToken))
            {
                shader.passes ??= new();
                shader.passes.Add(PassBlock.Parse(bodyStream.SliceNextBlock()));
            }
            else if (nextToken.Match(TokenType.Identifier))
            {
                var func = ParseUtility.ParseFunction(ref bodyStream, TokenType.StringLiteral | TokenType.Number | TokenType.Identifier);

                shader.functionCalls ??= new();
                shader.functionCalls.Add(func);
            }
            else
            {
                throw new Exception($"Unexpected token '{nextToken}' in shader body.");
            }
        }

        stream.Expect(TokenType.RBrace);

        return shader;
    }

    public static ShaderSemantics? SemanticAnalysis(ShaderSyntax? syntax, List<SDLError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var shaderModel = new ShaderSemantics
        {
            name = syntax.name.lexeme,
            properties = PropertiesBlock.SemanticAnalysis(syntax.properties, errors),
            pipeline = PipelineBlock.SemanticAnalysis(syntax.pipeline, errors)
        };

        if (syntax.passes != null)
        {
            foreach (var passSyntax in syntax.passes)
            {
                var passModel = PassBlock.SemanticAnalysis(passSyntax, errors);
                if (passModel != null)
                {
                    shaderModel.passes ??= new();
                    shaderModel.passes.Add(passModel);
                }
            }
        }

        if (syntax.functionCalls != null)
        {
            foreach (var func in syntax.functionCalls)
            {
                switch (func.name.lexeme)
                {
                    case TokenLexicon.KnownFunctions.FALLBACK:
                        if (func.arguments == null || func.arguments.Count != 1)
                        {
                            errors.Add(new SDLError
                            {
                                message = "Fallback declaration requires exactly one arguments: (fallback shader name).",
                                line = func.name.line,
                                column = func.name.column
                            });

                            continue;
                        }

                        shaderModel.fallback = func.arguments[0].lexeme;
                        break;
                    default:
                        errors.Add(new SDLError
                        {
                            message = $"Unknown function '{func.name.lexeme}' in shader.",
                            line = func.name.line,
                            column = func.name.column
                        });
                        break;
                }
            }
        }

        return shaderModel;
    }
}
