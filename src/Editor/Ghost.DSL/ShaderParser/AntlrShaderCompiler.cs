using Antlr4.Runtime;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser.Model;

namespace Ghost.DSL.ShaderParser;

public class AntlrShaderCompiler
{
    public static List<GraphicsShaderModel> ParseShaders(string source, out List<DSLShaderError> errors)
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
                return new List<GraphicsShaderModel>();
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
            return new List<GraphicsShaderModel>();
        }
    }

    public static List<ComputeShaderModel> ParseComputeShaders(string source, out List<DSLShaderError> errors)
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
                return new List<ComputeShaderModel>();
            }

            var visitor = new ComputeShaderVisitor();
            visitor.Visit(tree);

            return visitor.ComputeShaders;
        }
        catch (Exception ex)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Unexpected error during parsing: {ex.Message}",
                line = -1,
                column = -1
            });
            return new List<ComputeShaderModel>();
        }
    }

    public static DSLComputeShaderSemantics? ConvertToComputeSemantics(ComputeShaderModel model, out List<DSLShaderError> errors)
    {
        errors = new List<DSLShaderError>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            errors.Add(new DSLShaderError
            {
                message = "Compute shader name cannot be empty.",
                line = 0,
                column = 0
            });
            return null;
        }

        var semantics = new DSLComputeShaderSemantics
        {
            name = model.Name,
            defines = model.Defines?.Defines,
            includes = model.Includes?.Includes,
            hlsl = model.Hlsl?.Code
        };

        if (string.IsNullOrEmpty(model.SM))
        {
            semantics.shaderModel = ShaderModel.SM_6_8; // Default to highest supported shader model
        }
        else
        {
            semantics.shaderModel = model.SM.ToLower() switch
            {
                "6_6" => ShaderModel.SM_6_6,
                "6_7" => ShaderModel.SM_6_7,
                "6_8" => ShaderModel.SM_6_8,
                _ => ShaderModel.Invalid
            };

            if (semantics.shaderModel == ShaderModel.Invalid)
            {
                errors.Add(new DSLShaderError
                {
                    message = $"Unknown shader model '{model.SM}'.",
                    line = 0,
                    column = 0
                });
            }
        }

        if (model.Keywords != null)
        {
            semantics.keywords = new List<KeywordsGroup>();
            foreach (var group in model.Keywords.Groups)
            {
                var keywordGroup = new KeywordsGroup
                {
                    space = group.Scope?.ToLower() == "global" ? KeywordSpace.Global : KeywordSpace.Local,
                    keywords = group.Keywords
                };
                semantics.keywords.Add(keywordGroup);
            }
        }

        foreach (var entry in model.ShaderEntries)
        {
            var entryType = entry.EntryType.ToLower();
            if (entryType == "cs")
            {
                semantics.entryPoints ??= new List<ShaderEntryPoint>();
                semantics.entryPoints.Add(new ShaderEntryPoint
                {
                    shader = entry.ShaderPath,
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
                message = $"Compute shader '{model.Name}' must contain a compute/cs entry declaration.",
                line = 0,
                column = 0
            });
        }

        if (semantics.entryPoints != null && semantics.entryPoints.Count > 8)
        {
            errors.Add(new DSLShaderError
            {
                message = $"Compute shader '{model.Name}' cannot have more than 8 entry points.",
                line = 0,
                column = 0
            });
        }

        return semantics;
    }

    public static DSLShaderSemantics? ConvertToSemantics(GraphicsShaderModel model, out List<DSLShaderError> errors)
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
            pipeline = ConvertPipeline(model.Pipeline, errors)
        };

        if (string.IsNullOrEmpty(model.SM))
        {
            semantics.shaderModel = ShaderModel.SM_6_8; // Default to highest supported shader model
        }
        else
        {
            semantics.shaderModel = model.SM.ToLower() switch
            {
                "6_6" => ShaderModel.SM_6_6,
                "6_7" => ShaderModel.SM_6_7,
                "6_8" => ShaderModel.SM_6_8,
                _ => ShaderModel.Invalid
            };

            if (semantics.shaderModel == ShaderModel.Invalid)
            {
                errors.Add(new DSLShaderError
                {
                    message = $"Unknown shader model '{model.SM}'.",
                    line = 0,
                    column = 0
                });
            }
        }

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
                case "ms":
                    semantic.meshShader = shaderEntry;
                    break;
                case "ps":
                    semantic.pixelShader = shaderEntry;
                    break;
                case "as":
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
