using Antlr4.Runtime;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.ShaderParser;

public class AntlrShaderCompiler
{
    public static GraphicsShaderSyntax? ParseShaders(string source, List<DSLShaderError> errors)
    {
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
                return null;
            }

            var visitor = new ShaderVisitor();
            return (GraphicsShaderSyntax)visitor.Visit(tree);
        }
        catch (Exception ex)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Unexpected error during parsing: {ex.Message}",
                line = -1,
                column = -1
            });

            return null;
        }
    }

    public static ComputeShaderSyntax? ParseComputeShaders(string source, List<DSLShaderError> errors)
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
            var parser = new GhostComputeShaderParser(tokenStream);

            // Capture parser errors
            parser.RemoveErrorListeners();
            var parserErrorListener = new ErrorListener(errors);
            parser.AddErrorListener(parserErrorListener);

            var tree = parser.computeFile();

            if (errors.Count > 0)
            {
                return null;
            }

            var visitor = new ComputeShaderVisitor();
            return (ComputeShaderSyntax)visitor.Visit(tree);
        }
        catch (Exception ex)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Unexpected error during parsing: {ex.Message}",
                line = -1,
                column = -1
            });
            return null;
        }
    }

    private static bool TryGetShaderModel(string model, List<DSLShaderError> errors, out ShaderModel shaderModel)
    {
        if (string.IsNullOrEmpty(model))
        {
            shaderModel = ShaderModel.SM_6_6; // Default to lowest supported shader model for compute shaders
        }
        else
        {
            switch (model)
            {
                case "6_6":
                    shaderModel = ShaderModel.SM_6_6;
                    break;
                case "6_7":
                    shaderModel = ShaderModel.SM_6_7;
                    break;
                case "6_8":
                    shaderModel = ShaderModel.SM_6_8;
                    break;
                default:
                    shaderModel = default;
                    errors.Add(new DSLShaderError
                    {
                        message = $"Unknown shader model '{model}'.",
                        line = 0,
                        column = 0
                    });
                    return false;
            }
        }

        return true;
    }

    public static ComputeShaderSemantics? ConvertToComputeSemantics(ComputeShaderSyntax syntax, out List<DSLShaderError> errors)
    {
        errors = new List<DSLShaderError>();

        if (string.IsNullOrWhiteSpace(syntax.Name))
        {
            errors.Add(new DSLShaderError
            {
                message = "Compute shader name cannot be empty.",
                line = 0,
                column = 0
            });
            return null;
        }

        var semantics = new ComputeShaderSemantics
        {
            name = syntax.Name,
            defines = syntax.Defines?.Defines ?? new List<string>(),
            includes = syntax.Includes?.Includes ?? new List<string>(),
            hlsl = syntax.Hlsl?.Code
        };

        if (TryGetShaderModel(syntax.ShaderModel, errors, out var shaderModel))
        {
            semantics.shaderModel = shaderModel;
        }


        foreach (var entry in syntax.ShaderEntries)
        {
            var entryType = entry.EntryType.ToLower();
            if (entryType == "cs")
            {
                semantics.entryPoints ??= new List<ShaderEntryPoint>();
                semantics.entryPoints.Add(new ShaderEntryPoint
                {
                    shaderPath = entry.ShaderPath,
                    entry = entry.EntryPoint
                });
            }
            else
            {
                errors.Add(new DSLShaderError
                {
                    message = $"Unknown compute shader entry type '{entry.EntryType}'. Expected 'compute' or 'cs'.",
                    line = 0,
                    column = 0
                });
            }
        }

        if (semantics.entryPoints == null)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Compute shader '{syntax.Name}' must contain a compute/cs entry declaration.",
                line = 0,
                column = 0
            });
        }

        if (semantics.entryPoints != null && semantics.entryPoints.Count > 8)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Compute shader '{syntax.Name}' cannot have more than 8 entry points.",
                line = 0,
                column = 0
            });
        }

        return semantics;
    }

    private static PipelineSemantic? ConvertPipeline(PipelineBlockSyntax? pipeline, List<DSLShaderError> errors)
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
                        "less_equal" => ZTest.LessEqual,
                        "equal" => ZTest.Equal,
                        "greater_equal" => ZTest.GreaterEqual,
                        "greater" => ZTest.Greater,
                        "not_equal" => ZTest.NotEqual,
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
                        "premultiplied_alpha" => Blend.PremultipliedAlpha,
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

    private static ShaderPassSemantic? ConvertPass(PassBlockSyntax pass, List<DSLShaderError> errors)
    {
        var semantic = new ShaderPassSemantic
        {
            name = pass.Name,
            hlsl = pass.Hlsl?.Code,
            defines = pass.Defines?.Defines,
            includes = pass.Includes?.Includes,
            localPipeline = ConvertPipeline(pass.LocalPipeline, errors)
        };


        foreach (var entry in pass.ShaderEntries)
        {
            var entryType = entry.EntryType.ToLower();
            var shaderEntry = new ShaderEntryPoint
            {
                shaderPath = entry.ShaderPath,
                entry = entry.EntryPoint
            };

            switch (entryType)
            {
                case "ms":
                    semantic.meshShader = shaderEntry;
                    break;
                case "ps":
                    semantic.pixelShader = shaderEntry;
                    break;
                case "as":
                    semantic.amplificationShader = shaderEntry;
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

        if (semantic.meshShader.shaderPath == null || semantic.pixelShader.shaderPath == null)
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

    public static GraphicsShaderSemantics? ConvertToSemantics(GraphicsShaderSyntax syntax, out List<DSLShaderError> errors)
    {
        errors = new List<DSLShaderError>();

        if (string.IsNullOrWhiteSpace(syntax.Name))
        {
            errors.Add(new DSLShaderError
            {
                message = "Shader name cannot be empty.",
                line = 0,
                column = 0
            });
            return null;
        }

        var semantics = new GraphicsShaderSemantics
        {
            name = syntax.Name,
            templateName = syntax.TemplateName,
            payload = syntax.Payload?.Code,
            hlsl = syntax.Hlsl?.Code,
            includes = syntax.Includes?.Includes ?? new List<string>(),
            pipeline = ConvertPipeline(syntax.Pipeline, errors)
        };

        if (syntax.Properties != null)
        {
            foreach (var prop in syntax.Properties.Properties)
            {
                semantics.properties.Add(new PropertySemantic
                {
                    type = prop.Type,
                    name = prop.Name,
                    defaultValue = prop.DefaultValue
                });
            }
        }

        if (string.IsNullOrEmpty(syntax.TemplateName) && syntax.Passes.Count == 0)
        {
            errors.Add(new DSLShaderError
            {
                message = "Shader must either inherit from a template (e.g. : \"Lit\") or contain pass definitions.",
                line = 0,
                column = 0
            });
        }
        else if (TryGetShaderModel(syntax.ShaderModel, errors, out var shaderModel))
        {
            semantics.shaderModel = shaderModel;
        }

        foreach (var pass in syntax.Passes)
        {
            var passSemantic = ConvertPass(pass, errors);
            if (passSemantic != null)
            {
                semantics.passes.Add(passSemantic);
            }
        }

        return semantics;
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
