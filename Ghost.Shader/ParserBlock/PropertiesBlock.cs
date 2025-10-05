using Misaki.HighPerformance.Mathematics;
using System.Globalization;

namespace Ghost.Shader.ParserBlock;

internal class PropertiesBlock : IBlockParser<List<PropertySyntax>, List<PropertyModel>>
{
    private sealed record PropTypeInfo(int ArgCount, TokenType ArgTokenType, Func<List<Token>, object?>? Builder);

    private static readonly Dictionary<ShaderPropertyType, PropTypeInfo> s_propTypeInfo = new()
    {
        // Floats
        [ShaderPropertyType.Float]  = new(1, TokenType.Number, a => float.Parse(a[0].lexeme, CultureInfo.InvariantCulture)),
        [ShaderPropertyType.Float2] = new(2, TokenType.Number, a => new float2(
            float.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[1].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.Float3] = new(3, TokenType.Number, a => new float3(
            float.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[2].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.Float4] = new(4, TokenType.Number, a => new float4(
            float.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[2].lexeme, CultureInfo.InvariantCulture),
            float.Parse(a[3].lexeme, CultureInfo.InvariantCulture))),

        // Ints
        [ShaderPropertyType.Int]  = new(1, TokenType.Number, a => int.Parse(a[0].lexeme, CultureInfo.InvariantCulture)),
        [ShaderPropertyType.Int2] = new(2, TokenType.Number, a => new int2(
            int.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[1].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.Int3] = new(3, TokenType.Number, a => new int3(
            int.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[2].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.Int4] = new(4, TokenType.Number, a => new int4(
            int.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[2].lexeme, CultureInfo.InvariantCulture),
            int.Parse(a[3].lexeme, CultureInfo.InvariantCulture))),

        // UInts
        [ShaderPropertyType.UInt]  = new(1, TokenType.Number, a => uint.Parse(a[0].lexeme, CultureInfo.InvariantCulture)),
        [ShaderPropertyType.UInt2] = new(2, TokenType.Number, a => new uint2(
            uint.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[1].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.UInt3] = new(3, TokenType.Number, a => new uint3(
            uint.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[2].lexeme, CultureInfo.InvariantCulture))),
        [ShaderPropertyType.UInt4] = new(4, TokenType.Number, a => new uint4(
            uint.Parse(a[0].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[1].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[2].lexeme, CultureInfo.InvariantCulture),
            uint.Parse(a[3].lexeme, CultureInfo.InvariantCulture))),

        // Bools (numbers 0/1)
        [ShaderPropertyType.Bool]  = new(1, TokenType.Number, a => a[0].lexeme != "0"),
        [ShaderPropertyType.Bool2] = new(2, TokenType.Number, a => new bool2(a[0].lexeme != "0", a[1].lexeme != "0")),
        [ShaderPropertyType.Bool3] = new(3, TokenType.Number, a => new bool3(a[0].lexeme != "0", a[1].lexeme != "0", a[2].lexeme != "0")),
        [ShaderPropertyType.Bool4] = new(4, TokenType.Number, a => new bool4(a[0].lexeme != "0", a[1].lexeme != "0", a[2].lexeme != "0", a[3].lexeme != "0")),

        // Textures (single identifier argument – currently no default object built)
        [ShaderPropertyType.Texture2D]   = new(1, TokenType.Identifier, null),
        [ShaderPropertyType.Texture3D]   = new(1, TokenType.Identifier, null),
        [ShaderPropertyType.TextureCube] = new(1, TokenType.Identifier, null),
    };

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

    public static List<PropertySyntax> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var properties = new List<PropertySyntax>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var shaderProperty = new PropertySyntax();

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
                    shaderProperty.propertyConstructor = new FunctionCall
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

            properties.Add(shaderProperty);
        }

        stream.Expect(TokenType.RBrace);

        return properties;
    }

    public List<PropertyModel> SemanticAnalysis(List<PropertySyntax> syntax, List<ShaderError> errors)
    {
        var models = new List<PropertyModel>();
        var usedPropertyNames = new HashSet<string>();

        foreach (var property in syntax)
        {
            var model = new PropertyModel();

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

        return models;
    }

    private static bool ValidatePropertyType(List<ShaderError> errors, PropertySyntax property, PropertyModel model)
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

    private static bool ValidatePropertyName(List<ShaderError> errors, HashSet<string> usedPropertyNames, PropertySyntax property, PropertyModel model)
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

    private static bool ValidatePropertyConstructor(List<ShaderError> errors, PropertySyntax property, PropertyModel model)
    {
        var constructor = property.propertyConstructor;
        if (constructor == null)
        {
            errors.Add(new ShaderError
            {
                message = "Shader property constructor is null.",
                line = property.name.line,
                column = property.name.column
            });
            return false;
        }

        if (string.IsNullOrWhiteSpace(constructor.name.lexeme))
        {
            errors.Add(new ShaderError
            {
                message = "Shader property constructor has an empty name.",
                line = constructor.name.line,
                column = constructor.name.column
            });
            return false;
        }

        if (constructor.name.lexeme != property.type.lexeme)
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property constructor name '{constructor.name.lexeme}' does not match property type '{property.type.lexeme}'.",
                line = constructor.name.line,
                column = constructor.name.column
            });
            return false;
        }

        if (!s_propTypeInfo.TryGetValue(model.type, out var info))
        {
            errors.Add(new ShaderError
            {
                message = $"No constructor metadata registered for property type '{model.type}'.",
                line = constructor.name.line,
                column = constructor.name.column
            });
            return false;
        }

        // Count check
        if (constructor.arguments.Count != info.ArgCount)
        {
            errors.Add(new ShaderError
            {
                message = $"Shader property constructor for type '{property.type.lexeme}' expects {info.ArgCount} argument(s), but got {constructor.arguments.Count}.",
                line = constructor.name.line,
                column = constructor.name.column
            });
            return false;
        }

        // Type check (uniform requirement for all args)
        var hasError = false;
        for (var i = 0; i < constructor.arguments.Count; i++)
        {
            var arg = constructor.arguments[i];
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
                model.defaultValue = info.Builder(constructor.arguments);
            }
            catch (Exception ex)
            {
                errors.Add(new ShaderError
                {
                    message = $"Failed to construct default value for property '{property.name.lexeme}': {ex.Message}",
                    line = constructor.name.line,
                    column = constructor.name.column
                });
                return false;
            }
        }

        return true;
    }
}