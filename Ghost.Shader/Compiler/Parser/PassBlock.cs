namespace Ghost.Shader.Compiler.Parser;

// TODO: Add pass template support.
//   Pass templates let user to inject their own custom code into the generated HLSL code.
//   This is useful for adding custom lighting models, custom shadowing techniques, or other advanced effects without touching the core shader code.
internal class PassBlock : IBlockParser<PassSyntax, PassSemantic>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.PASS);
    }

    public static PassSyntax Parse(TokenStreamSlice stream)
    {
        var pass = new PassSyntax();

        stream.Expect(TokenType.Keyword);
        pass.name = stream.Expect(TokenType.StringLiteral);
        stream.Expect(TokenType.LBrace);

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.TryPeek(out var nextToken))
        {
            if (DefinesBlock.ShouldEnter(nextToken))
            {
                pass.defines = DefinesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (IncludesBlock.ShouldEnter(nextToken))
            {
                pass.includes = IncludesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (KeywordsBlock.ShouldEnter(nextToken))
            {
                pass.keywords = KeywordsBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PipelineBlock.ShouldEnter(nextToken))
            {
                pass.localPipeline = PipelineBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PropertiesBlock.ShouldEnter(nextToken))
            {
                pass.localProperties = PropertiesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (nextToken.Match(TokenType.Identifier))
            {
                var func = ParseUtility.ParseFunction(ref bodyStream, TokenType.StringLiteral);

                pass.functionCalls ??= new();
                pass.functionCalls.Add(func);
            }
            else
            {
                throw new Exception($"Unexpected token '{nextToken}' in pass body.");
            }
        }

        stream.Expect(TokenType.RBrace);

        return pass;
    }

    public static PassSemantic? SemanticAnalysis(PassSyntax? syntax, List<ShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var model = new PassSemantic
        {
            name = syntax.name.lexeme,
            defines = DefinesBlock.SemanticAnalysis(syntax.defines, errors),
            includes = IncludesBlock.SemanticAnalysis(syntax.includes, errors),
            keywords = KeywordsBlock.SemanticAnalysis(syntax.keywords, errors),
            localProperties = PropertiesBlock.SemanticAnalysis(syntax.localProperties, errors),
            localPipeline = PipelineBlock.SemanticAnalysis(syntax.localPipeline, errors),
        };

        if (model.localProperties != null
            && model.localProperties.Any(p => p.scope == PropertyScope.Global))
        {
            errors.Add(new ShaderError
            {
                message = "Global properties cannot be declared inside a pass. Move them to the shader properties block.",
                line = syntax.name.line,
                column = syntax.name.column
            });
        }

        if (syntax.functionCalls != null)
        {
            foreach (var func in syntax.functionCalls)
            {
                switch (func.name.lexeme)
                {
                    case "vs":
                        if (func.arguments?.Count != 2)
                        {
                            errors.Add(new ShaderError
                            {
                                message = "Vertex shader declaration requires exactly two arguments: (shaderPath, entryPoint).",
                                line = func.name.line,
                                column = func.name.column
                            });
                        }
                        else
                        {
                            model.vertexShader = new ShaderEntryPoint
                            {
                                shader = func.arguments[0].lexeme,
                                entry = func.arguments[1].lexeme
                            };
                        }
                        break;

                    case "ps":
                        if (func.arguments?.Count != 2)
                        {
                            errors.Add(new ShaderError
                            {
                                message = "Pixel shader declaration requires exactly two arguments: (shaderPath, entryPoint).",
                                line = func.name.line,
                                column = func.name.column
                            });
                        }
                        else
                        {
                            model.pixelShader = new ShaderEntryPoint
                            {
                                shader = func.arguments[0].lexeme,
                                entry = func.arguments[1].lexeme
                            };
                        }
                        break;

                    default:
                        errors.Add(new ShaderError
                        {
                            message = $"Unknown function '{func.name.lexeme}' in pass.",
                            line = func.name.line,
                            column = func.name.column
                        });
                        break;
                }
            }
        }

        if (model.vertexShader.shader == null || model.pixelShader.shader == null)
        {
            // TODO: Inheritance from base pass.
            // TODO: Add mesh shader support.
            errors.Add(new ShaderError
            {
                message = "Pass must contain a vertex shader (vs) and a pixel shader (ps) declaration.",
                line = syntax.name.line,
                column = syntax.name.column
            });
        }

        return model;
    }
}
