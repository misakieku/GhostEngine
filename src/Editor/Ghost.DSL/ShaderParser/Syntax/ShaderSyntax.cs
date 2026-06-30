namespace Ghost.DSL.ShaderParser.Syntax;

public class GraphicsShaderSyntax
{
    public string Name { get; set; } = string.Empty;
    public string ShaderModel { get; set; } = string.Empty;
    public PipelineBlockSyntax? Pipeline { get; set; }
    public List<PassBlockSyntax> Passes { get; set; } = new();
    public List<FunctionCallSyntax> FunctionCalls { get; set; } = new();
}

public class ComputeShaderSyntax
{
    public string Name { get; set; } = string.Empty;
    public string ShaderModel { get; set; } = string.Empty;
    public DefinesBlockSyntax? Defines { get; set; }
    public IncludesBlockSyntax? Includes { get; set; }
    public KeywordsBlockSyntax? Keywords { get; set; }
    public HlslBlockSyntax? Hlsl { get; set; }
    public List<FunctionCallSyntax> FunctionCalls { get; set; } = new();
    public List<ShaderEntrySyntax> ShaderEntries { get; set; } = new();
}

public class PipelineBlockSyntax
{
    public Dictionary<string, string> Statements { get; set; } = new();
}

public class PassBlockSyntax
{
    public string Name { get; set; } = string.Empty;
    public PipelineBlockSyntax? LocalPipeline { get; set; }
    public DefinesBlockSyntax? Defines { get; set; }
    public IncludesBlockSyntax? Includes { get; set; }
    public KeywordsBlockSyntax? Keywords { get; set; }
    public HlslBlockSyntax? Hlsl { get; set; }
    public List<ShaderEntrySyntax> ShaderEntries { get; set; } = new();
}

public class DefinesBlockSyntax
{
    public List<string> Defines { get; set; } = new();
}

public class IncludesBlockSyntax
{
    public List<string> Includes { get; set; } = new();
}

public class KeywordsBlockSyntax
{
    public List<KeywordGroupSyntax> Groups { get; set; } = new();
}

public class KeywordGroupSyntax
{
    public List<string> Keywords { get; set; } = new();
}

public class HlslBlockSyntax
{
    public string Code { get; set; } = string.Empty;
}

public class ShaderEntrySyntax
{
    public string EntryType { get; set; } = string.Empty;  // "mesh", "pixel", "task", etc.
    public string ShaderPath { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
}

public class FunctionCallSyntax
{
    public string Name { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
}
