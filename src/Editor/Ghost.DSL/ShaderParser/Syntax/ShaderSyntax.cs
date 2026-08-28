namespace Ghost.DSL.ShaderParser.Syntax;

public class GraphicsShaderSyntax
{
    public string Name { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public PropertiesBlockSyntax? Properties { get; set; }
    public PayloadBlockSyntax? Payload { get; set; }
    public IncludesBlockSyntax? Includes { get; set; }
    public HlslBlockSyntax? Hlsl { get; set; }
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
    public HlslBlockSyntax? Hlsl { get; set; }
    public List<FunctionCallSyntax> FunctionCalls { get; set; } = new();
    public List<ShaderEntrySyntax> ShaderEntries { get; set; } = new();
}

public class PropertiesBlockSyntax
{
    public List<PropertyStatementSyntax> Properties { get; set; } = new();
}

public class PropertyStatementSyntax
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

public class PayloadBlockSyntax
{
    public string Code { get; set; } = string.Empty;
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
