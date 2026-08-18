using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Ghost.DSL.ShaderParser;
using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.Parser;

public static class DSLParser
{
    public static DSLDocumentSyntax? ParseDocument(string source, string filePath = "", List<DSLShaderError>? errors = null)
    {
        try
        {
            var inputStream = new AntlrInputStream(source);
            var lexer = new GhostShaderLexer(inputStream);
            var errorListener = new ErrorListener(filePath, errors ?? new List<DSLShaderError>());

            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new GhostShaderParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            var tree = parser.shaderFile();

            if (errors != null && errors.Count > 0)
            {
                return null;
            }

            var visitor = new ShaderVisitor();
            return (DSLDocumentSyntax)visitor.Visit(tree);
        }
        catch (Exception ex)
        {
            errors?.Add(new DSLShaderError
            {
                Message = ex.Message,
                FilePath = filePath
            });
            return null;
        }
    }

    public static ComputeShaderSyntax? ParseComputeShader(string source, string filePath = "", List<DSLShaderError>? errors = null)
    {
        try
        {
            var inputStream = new AntlrInputStream(source);
            var lexer = new GhostShaderLexer(inputStream);
            var errorListener = new ErrorListener(filePath, errors ?? new List<DSLShaderError>());

            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new GhostComputeShaderParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            var tree = parser.computeFile();

            if (errors != null && errors.Count > 0)
            {
                return null;
            }

            var visitor = new ComputeShaderVisitor();
            return (ComputeShaderSyntax)visitor.Visit(tree);
        }
        catch (Exception ex)
        {
            errors?.Add(new DSLShaderError
            {
                Message = ex.Message,
                FilePath = filePath
            });
            return null;
        }
    }

    private class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private readonly string _filePath;
        private readonly List<DSLShaderError> _errors;

        public ErrorListener(string filePath, List<DSLShaderError> errors)
        {
            _filePath = filePath;
            _errors = errors;
        }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add(new DSLShaderError
            {
                Message = msg,
                Line = line,
                Column = charPositionInLine,
                FilePath = _filePath
            });
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add(new DSLShaderError
            {
                Message = msg,
                Line = line,
                Column = charPositionInLine,
                FilePath = _filePath
            });
        }
    }
}
