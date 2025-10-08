using Ghost.Shader.Compiler.Parser;
using Misaki.HighPerformance.Mathematics;
using System.Globalization;

namespace Ghost.Shader.Compiler.Parser;

internal class PropertiesBlock : IBlockParser<PropertiesSyntax, List<PropertySemantic>>
{
    private delegate object? PropertyValueBuilder(List<Token> tokens, List<ShaderError> errors);

    private sealed record PropTypeInfo(int ArgCount, TokenType ArgTokenType, PropertyValueBuilder? Builder);

    private static readonly Dictionary<ShaderPropertyType, PropTypeInfo> s_propTypeInfo = new()
    {
        // Floats
        [ShaderPropertyType.Float] = new(1, TokenType.Number, (syntax, errors) => ParseFloatValue(syntax[0], errors)),
        [ShaderPropertyType.Float2] = new(2, TokenType.Number, (syntax, errors) => new float2(
            ParseFloatValue(syntax[0], errors),
            ParseFloatValue(syntax[1], errors))),
        [ShaderPropertyType.Float3] = new(3, TokenType.Number, (syntax, errors) => new float3(
            ParseFloatValue(syntax[0], errors),
            ParseFloatValue(syntax[1], errors),
            ParseFloatValue(syntax[2], errors))),
        [ShaderPropertyType.Float4] = new(4, TokenType.Number, (syntax, errors) => new float4(
            ParseFloatValue(syntax[0], errors),
            ParseFloatValue(syntax[1], errors),
            ParseFloatValue(syntax[2], errors),
            ParseFloatValue(syntax[3], errors))),

        // Ints
        [ShaderPropertyType.Int] = new(1, TokenType.Number, (syntax, errors) => ParseIntValue(syntax[0], errors)),
        [ShaderPropertyType.Int2] = new(2, TokenType.Number, (syntax, errors) => new int2(
            ParseIntValue(syntax[0], errors),
            ParseIntValue(syntax[1], errors))),
        [ShaderPropertyType.Int3] = new(3, TokenType.Number, (syntax, errors) => new int3(
            ParseIntValue(syntax[0], errors),
            ParseIntValue(syntax[1], errors),
            ParseIntValue(syntax[2], errors))),
        [ShaderPropertyType.Int4] = new(4, TokenType.Number, (syntax, errors) => new int4(
            ParseIntValue(syntax[0], errors),
            ParseIntValue(syntax[1], errors),
            ParseIntValue(syntax[2], errors),
            ParseIntValue(syntax[3], errors))),

        // UInts
        [ShaderPropertyType.UInt] = new(1, TokenType.Number, (syntax, errors) => ParseUIntValue(syntax[0], errors)),
        [ShaderPropertyType.UInt2] = new(2, TokenType.Number, (syntax, errors) => new uint2(
            ParseUIntValue(syntax[0], errors),
            ParseUIntValue(syntax[1], errors))),
        [ShaderPropertyType.UInt3] = new(3, TokenType.Number, (syntax, errors) => new uint3(
            ParseUIntValue(syntax[0], errors),
            ParseUIntValue(syntax[1], errors),
            ParseUIntValue(syntax[2], errors))),
        [ShaderPropertyType.UInt4] = new(4, TokenType.Number, (syntax, errors) => new uint4(
            ParseUIntValue(syntax[0], errors),
            ParseUIntValue(syntax[1], errors),
            ParseUIntValue(syntax[2], errors),
            ParseUIntValue(syntax[3], errors))),

        // Bools (numbers 0/1)
        [ShaderPropertyType.Bool] = new(1, TokenType.Number, (syntax, errors) => ParseBoolValue(syntax[0], errors)),
        [ShaderPropertyType.Bool2] = new(2, TokenType.Number, (syntax, errors) => new bool2(
            ParseBoolValue(syntax[0], errors),
            ParseBoolValue(syntax[1], errors))),
        [ShaderPropertyType.Bool3] = new(3, TokenType.Number, (syntax, errors) => new bool3(
            ParseBoolValue(syntax[0], errors),
            ParseBoolValue(syntax[1], errors),
            ParseBoolValue(syntax[2], errors))),
        [ShaderPropertyType.Bool4] = new(4, TokenType.Number, (syntax, errors) => new bool4(
            ParseBoolValue(syntax[0], errors),
            ParseBoolValue(syntax[1], errors),
            ParseBoolValue(syntax[2], errors),
            ParseBoolValue(syntax[3], errors))),

        // Textures (single identifier argument)
        [ShaderPropertyType.Texture2D] = new(1, TokenType.Identifier, (syntax, errors) => ParseTextureDefault(syntax[0], errors)),
        [ShaderPropertyType.Texture3D] = new(1, TokenType.Identifier, (syntax, errors) => ParseTextureDefault(syntax[0], errors)),
        [ShaderPropertyType.TextureCube] = new(1, TokenType.Identifier, (syntax, errors) => ParseTextureDefault(syntax[0], errors)),
    };

    private static float ParseFloatValue(Token token, List<ShaderError> errors)
    {
        if (!float.TryParse(token.lexeme, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add(new ShaderError
            {
                message = $"Failed to parse float value '{token.lexeme}'.",
                line = token.line,
                column = token.column
            });
        }

        return result;
    }

    private static int ParseIntValue(Token token, List<ShaderError> errors)
    {
        if (!int.TryParse(token.lexeme, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add(new ShaderError
            {
                message = $"Failed to parse int value '{token.lexeme}'.",
                line = token.line,
                column = token.column
            });
        }

        return result;
    }

    private static uint ParseUIntValue(Token token, List<ShaderError> errors)
    {
        if (!uint.TryParse(token.lexeme, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add(new ShaderError
            {
                message = $"Failed to parse uint value '{token.lexeme}'.",
                line = token.line,
                column = token.column
            });
        }

        return result;
    }

    private static bool ParseBoolValue(Token token, List<ShaderError> errors)
    {
        if (!bool.TryParse(token.lexeme, out var result))
        {
            errors.Add(new ShaderError
            {
                message = $"Failed to parse bool value '{token.lexeme}'.",
                line = token.line,
                column = token.column
            });
        }

        return result;
    }

    private static string ParseTextureDefault(Token token, List<ShaderError> errors)
    {
        if (!TokenLexicon.IsTextureDefaultValue(token.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = $"Texture default value '{token.lexeme}' is not valid.",
                line = token.line,
                column = token.column
            });
        }

        return token.lexeme;
    }

    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.PROPERTIES);
    }

    private static ShaderPropertyType FromString(string type)
    {
        return type.ToLower() switch
        {
            "float" => ShaderPropertyType.Float,
            "float2" => ShaderPropertyType.Float2,
            "float3" => ShaderPropertyType.Float3,
            "float4" => ShaderPropertyType.Float4,
            "int" => ShaderPropertyType.Int,
            "int2" => ShaderPropertyType.Int2,
            "int3" => ShaderPropertyType.Int3,
            "int4" => ShaderPropertyType.Int4,
            "uint" => ShaderPropertyType.UInt,
            "uint2" => ShaderPropertyType.UInt2,
            "uint3" => ShaderPropertyType.UInt3,
            "uint4" => ShaderPropertyType.UInt4,
            "bool" => ShaderPropertyType.Bool,
            "bool2" => ShaderPropertyType.Bool2,
            "bool3" => ShaderPropertyType.Bool3,
            "bool4" => ShaderPropertyType.Bool4,
            "texture2d" => ShaderPropertyType.Texture2D,
            "texture3d" => ShaderPropertyType.Texture3D,
            "texturecube" => ShaderPropertyType.TextureCube,
            _ => ShaderPropertyType.None,
        };
    }

    public static PropertiesSyntax Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var syntax = new PropertiesSyntax();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var shaderProperty = new PropertyDeclaration();
            if (bodyStream.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.GLOBAL)
                || bodyStream.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.LOCAL))
            {
                var scopeToken = bodyStream.Consume();
                shaderProperty.scope = scopeToken;
            }

            var typeToken = bodyStream.Expect(TokenType.Identifier);
            var nameToken = bodyStream.Expect(TokenType.Identifier);

            shaderProperty.type = typeToken;
            shaderProperty.name = nameToken;

            var nextToken = bodyStream.Consume();
            switch (nextToken.type)
            {
                case TokenType.Equals:
                {
                    var constructorTypeToken = bodyStream.Expect(TokenType.Identifier);
                    var args = ParseUtility.ParseFunctionArguments(ref bodyStream, TokenType.Identifier | TokenType.Number);
                    shaderProperty.propertyConstructor = new FunctionCallDeclaration
                    {
                        name = constructorTypeToken,
                        arguments = args
                    };

                    bodyStream.Expect(TokenType.Semicolon);

                    break;
                }

                case TokenType.Semicolon:
                    break;
                default:
                    throw new Exception($"Unexpected token '{nextToken.lexeme}' in property declaration.");
            }

            syntax.properties ??= new();
            syntax.properties.Add(shaderProperty);
        }

        stream.Expect(TokenType.RBrace);

        return syntax;
    }

    public static List<PropertySemantic>? SemanticAnalysis(PropertiesSyntax? syntax, List<ShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var models = new List<PropertySemantic>();
        var usedPropertyNames = new HashSet<string>();

        if (syntax.properties != null)
        {
            foreach (var property in syntax.properties)
            {
                var model = new PropertySemantic
                {
                    scope = property.scope.lexeme switch
                    {
                        TokenLexicon.KnownKeywords.GLOBAL => PropertyScope.Global,
                        TokenLexicon.KnownKeywords.LOCAL => PropertyScope.Local,
                        _ => PropertyScope.Local,
                    }
                };

                var flowControl = ValidatePropertyType(errors, property, model);
                if (!flowControl)
                {
                    continue;
                }

                flowControl = ValidatePropertyName(errors, usedPropertyNames, property, model);
                if (!flowControl)
                {
                    continue;
                }

                if (property.propertyConstructor != null)
                {
                    flowControl = ValidatePropertyConstructor(errors, property, model);
                    if (!flowControl)
                    {
                        continue;
                    }
                }

                usedPropertyNames.Add(property.name.lexeme);
                models.Add(model);
            }
        }

        return models;
    }

    private static bool ValidatePropertyType(List<ShaderError> errors, PropertyDeclaration property, PropertySemantic model)
    {
        if (!TokenLexicon.IsType(property.type.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property type '{property.type.lexeme}' is not a valid type.",
                line = property.type.line,
                column = property.type.column
            });

            return false;
        }

        model.type = FromString(property.type.lexeme);
        return true;
    }

    private static bool ValidatePropertyName(List<ShaderError> errors, HashSet<string> usedPropertyNames, PropertyDeclaration property, PropertySemantic model)
    {
        if (string.IsNullOrWhiteSpace(property.name.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = "Shader property has an empty name.",
                line = property.name.line,
                column = property.name.column
            });

            return false;
        }
        else if (usedPropertyNames.Contains(property.name.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property name '{property.name.lexeme}' is duplicated.",
                line = property.name.line,
                column = property.name.column
            });

            return false;
        }

        model.name = property.name.lexeme;
        return true;
    }

    private static bool ValidatePropertyConstructor(List<ShaderError> errors, PropertyDeclaration property, PropertySemantic model)
    {
        var constructor = property.propertyConstructor;
        if (!constructor.HasValue)
        {
            errors.Add(new ShaderError
            {
                message = "Shader property constructor is null.",
                line = property.name.line,
                column = property.name.column
            });

            return false;
        }

        var constructorValue = constructor.Value;
        if (string.IsNullOrWhiteSpace(constructorValue.name.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = "Shader property constructor has an empty name.",
                line = constructorValue.name.line,
                column = constructorValue.name.column
            });

            return false;
        }

        if (constructorValue.name.lexeme != property.type.lexeme)
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property constructor name '{constructorValue.name.lexeme}' does not match property type '{property.type.lexeme}'.",
                line = constructorValue.name.line,
                column = constructorValue.name.column
            });

            return false;
        }

        if (!s_propTypeInfo.TryGetValue(model.type, out var info))
        {
            errors.Add(new ShaderError
            {
                message = $"No constructor metadata registered for property type '{model.type}'.",
                line = constructorValue.name.line,
                column = constructorValue.name.column
            });

            return false;
        }

        // Count check
        if (constructorValue.arguments == null)
        {
            errors.Add(new ShaderError
            {
                message = "Shader property constructor arguments are null.",
                line = constructorValue.name.line,
                column = constructorValue.name.column
            });

            return false;
        }

        if (constructorValue.arguments.Count != info.ArgCount)
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property constructor for type '{property.type.lexeme}' expects {info.ArgCount} argument(s), but got {constructorValue.arguments.Count}.",
                line = constructorValue.name.line,
                column = constructorValue.name.column
            });

            return false;
        }

        // Type check (uniform requirement for all args)
        var hasError = false;
        for (var i = 0; i < constructorValue.arguments.Count; i++)
        {
            var arg = constructorValue.arguments[i];
            if (!arg.Match(info.ArgTokenType))
            {
                errors.Add(new ShaderError
                {
                    message = $"Shader property constructor argument {i} expects token kind '{info.ArgTokenType}', but got '{arg.type}'.",
                    line = arg.line,
                    column = arg.column
                });

                hasError = true;
            }
        }

        if (hasError)
        {
            return false;
        }

        // Build default value if we have a builder (textures currently null / TODO)
        if (info.Builder != null)
        {
            try
            {
                model.defaultValue = info.Builder(constructorValue.arguments, errors);
            }
            catch (Exception ex)
            {
                errors.Add(new ShaderError
                {
                    message = $"Failed to construct default value for property '{property.name.lexeme}': {ex.Message}",
                    line = constructorValue.name.line,
                    column = constructorValue.name.column
                });

                return false;
            }
        }

        return true;
    }
}