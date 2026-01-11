using Antlr4.Runtime;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser.Model;

namespace Ghost.DSL.ShaderParser;

public class AntlrShaderCompiler
{
    public static List<ShaderModel> ParseShaders(string source, out List<DSLShaderError> errors)
    {
        errors = new List<DSLShaderError>();

        try
        {
            var inputStream = new AntlrInputStream(source);
            var lexer = new GhostShaderLexer(inputStream);

            // Capture lexer errors
            lexer.RemoveErrorListeners();
            var lexerErrorListener = new ErrorListener(errors);
            lexer.AddErrorListener(lexerErrorListener);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new GhostShaderParser(tokenStream);

            // Capture parser errors
            parser.RemoveErrorListeners();
            var parserErrorListener = new ErrorListener(errors);
            parser.AddErrorListener(parserErrorListener);

            var tree = parser.shaderFile();

            if (errors.Count > 0)
            {
                return new List<ShaderModel>();
            }

            var visitor = new ShaderVisitor();
            visitor.Visit(tree);

            return visitor.Shaders;
        }
        catch (Exception ex)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Unexpected error during parsing: {ex.Message}",
                line = -1,
                column = -1
            });
            return new List<ShaderModel>();
        }
    }

    public static DSLShaderSemantics? ConvertToSemantics(ShaderModel model, out List<DSLShaderError> errors)
    {
        errors = new List<DSLShaderError>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            errors.Add(new DSLShaderError
            {
                message = "Shader name cannot be empty.",
                line = 0,
                column = 0
            });
            return null;
        }

        var semantics = new DSLShaderSemantics
        {
            name = model.Name,
            properties = ConvertProperties(model.Properties, errors),
            pipeline = ConvertPipeline(model.Pipeline, errors)
        };

        foreach (var pass in model.Passes)
        {
            var passSemantic = ConvertPass(pass, errors);
            if (passSemantic != null)
            {
                semantics.passes ??= new List<PassSemantic>();
                semantics.passes.Add(passSemantic);
            }
        }

        return semantics;
    }

    private static List<PropertySemantic>? ConvertProperties(PropertiesBlockModel? properties, List<DSLShaderError> errors)
    {
        if (properties == null || properties.Properties.Count == 0)
        {
            return null;
        }

        var result = new List<PropertySemantic>();
        var usedNames = new HashSet<string>();

        foreach (var prop in properties.Properties)
        {
            if (usedNames.Contains(prop.Name))
            {
                errors.Add(new DSLShaderError
                {
                    message = $"Duplicate property name '{prop.Name}'.",
                    line = 0,
                    column = 0
                });
                continue;
            }

            var semantic = new PropertySemantic
            {
                name = prop.Name,
                scope = prop.Scope?.ToLower() == "global" ? PropertyScope.Global : PropertyScope.Local,
                type = ParsePropertyType(prop.Type, errors)
            };

            if (prop.Initializer.Count > 0)
            {
                semantic.defaultValue = ParsePropertyValue(semantic.type, prop.Initializer, errors);
            }

            usedNames.Add(prop.Name);
            result.Add(semantic);
        }

        return result;
    }

    private static ShaderPropertyType ParsePropertyType(string type, List<DSLShaderError> errors)
    {
        return type.ToLower() switch
        {
            "float" => ShaderPropertyType.Float,
            "float2" => ShaderPropertyType.Float2,
            "float3" => ShaderPropertyType.Float3,
            "float4" => ShaderPropertyType.Float4,
            "float4x4" => ShaderPropertyType.Float4x4,
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
            "tex2d" => ShaderPropertyType.Texture2D,
            "tex3d" => ShaderPropertyType.Texture3D,
            "texcube" => ShaderPropertyType.TextureCube,
            "texcube_arr" => ShaderPropertyType.TextureCubeArray,
            "tex2d_arr" => ShaderPropertyType.Texture2DArray,
            "sampler" => ShaderPropertyType.Sampler,
            _ => ShaderPropertyType.None
        };
    }

    private static object? ParsePropertyValue(ShaderPropertyType type, List<string> values, List<DSLShaderError> errors)
    {
        // For textures, the value is an identifier (e.g., "white", "black")
        if (type is ShaderPropertyType.Texture2D or ShaderPropertyType.Texture3D or ShaderPropertyType.TextureCube)
        {
            return values.Count > 0 ? values[0] : null;
        }

        // For samplers, no default value
        if (type == ShaderPropertyType.Sampler)
        {
            return null;
        }

        // For numeric types, parse the values
        try
        {
            return type switch
            {
                ShaderPropertyType.Float => values.Count > 0 ? float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture) : 0f,
                ShaderPropertyType.Float2 => values.Count >= 2 ? new Misaki.HighPerformance.Mathematics.float2(
                    float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.Float3 => values.Count >= 3 ? new Misaki.HighPerformance.Mathematics.float3(
                    float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.Float4 => values.Count >= 4 ? new Misaki.HighPerformance.Mathematics.float4(
                    float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.Int => values.Count > 0 ? int.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture) : 0,
                ShaderPropertyType.Int2 => values.Count >= 2 ? new Misaki.HighPerformance.Mathematics.int2(
                    int.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.Int3 => values.Count >= 3 ? new Misaki.HighPerformance.Mathematics.int3(
                    int.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.Int4 => values.Count >= 4 ? new Misaki.HighPerformance.Mathematics.int4(
                    int.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture)) : default,
                ShaderPropertyType.UInt => values.Count > 0 ? uint.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture) : 0u,
                ShaderPropertyType.Bool => values.Count > 0 && (values[0] == "1" || values[0].ToLower() == "true"),
                _ => null
            };
        }
        catch (Exception ex)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Failed to parse property value: {ex.Message}",
                line = 0,
                column = 0
            });
            return null;
        }
    }

    private static PipelineSemantic? ConvertPipeline(PipelineBlockModel? pipeline, List<DSLShaderError> errors)
    {
        if (pipeline == null || pipeline.Statements.Count == 0)
        {
            return null;
        }

        var semantic = new PipelineSemantic();

        foreach (var (key, value) in pipeline.Statements)
        {
            switch (key.ToLower())
            {
                case "ztest":
                    semantic.zTest = value.ToLower() switch
                    {
                        "disabled" => ZTest.Disabled,
                        "less" => ZTest.Less,
                        "lessequal" => ZTest.LessEqual,
                        "equal" => ZTest.Equal,
                        "greaterequal" => ZTest.GreaterEqual,
                        "greater" => ZTest.Greater,
                        "notequal" => ZTest.NotEqual,
                        "always" => ZTest.Always,
                        _ => ZTest.Disabled
                    };
                    break;
                case "zwrite":
                    semantic.zWrite = value.ToLower() == "on" ? ZWrite.On : ZWrite.Off;
                    break;
                case "cull":
                    semantic.cull = value.ToLower() switch
                    {
                        "off" => Cull.Off,
                        "front" => Cull.Front,
                        "back" => Cull.Back,
                        _ => Cull.Off
                    };
                    break;
                case "blend":
                    semantic.blend = value.ToLower() switch
                    {
                        "opaque" => Blend.Opaque,
                        "alpha" => Blend.Alpha,
                        "additive" => Blend.Additive,
                        "multiply" => Blend.Multiply,
                        "premultipliedalpha" => Blend.PremultipliedAlpha,
                        _ => Blend.Opaque
                    };
                    break;
                case "color_mask":
                    semantic.colorMask = value.ToLower() == "all" ? ColorWriteMask.All : ColorWriteMask.None;
                    break;
            }
        }

        return semantic;
    }

    private static PassSemantic? ConvertPass(PassBlockModel pass, List<DSLShaderError> errors)
    {
        var semantic = new PassSemantic
        {
            name = pass.Name,
            hlsl = pass.Hlsl?.Code,
            defines = pass.Defines?.Defines,
            includes = pass.Includes?.Includes,
            localPipeline = ConvertPipeline(pass.LocalPipeline, errors)
        };

        if (pass.Keywords != null)
        {
            semantic.keywords = new List<KeywordsGroup>();
            foreach (var group in pass.Keywords.Groups)
            {
                var keywordGroup = new KeywordsGroup
                {
                    space = group.Scope?.ToLower() == "global" ? KeywordSpace.Global : KeywordSpace.Local,
                    keywords = group.Keywords
                };
                semantic.keywords.Add(keywordGroup);
            }
        }

        foreach (var entry in pass.ShaderEntries)
        {
            var entryType = entry.EntryType.ToLower();
            var shaderEntry = new ShaderEntryPoint
            {
                shader = entry.ShaderPath,
                entry = entry.EntryPoint
            };

            switch (entryType)
            {
                case "mesh" or "ms":
                    semantic.meshShader = shaderEntry;
                    break;
                case "pixel" or "ps":
                    semantic.pixelShader = shaderEntry;
                    break;
                case "task" or "ts":
                    semantic.taskShader = shaderEntry;
                    break;
                default:
                    errors.Add(new DSLShaderError
                    {
                        message = $"Unknown shader entry type '{entry.EntryType}'.",
                        line = 0,
                        column = 0
                    });
                    break;
            }
        }

        if (semantic.meshShader.shader == null || semantic.pixelShader.shader == null)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Pass '{pass.Name}' must contain a mesh/ms shader and a pixel/ps shader declaration.",
                line = 0,
                column = 0
            });
        }

        return semantic;
    }

    private class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private readonly List<DSLShaderError> _errors;

        public ErrorListener(List<DSLShaderError> errors)
        {
            _errors = errors;
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add(new DSLShaderError
            {
                message = msg,
                line = line,
                column = charPositionInLine
            });
        }

        public new void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add(new DSLShaderError
            {
                message = msg,
                line = line,
                column = charPositionInLine
            });
        }
    }
}
