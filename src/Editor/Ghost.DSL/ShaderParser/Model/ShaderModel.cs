namespace Ghost.DSL.ShaderParser.Model;

public class GraphicsShaderModel
{
    public string Name { get; set; } = string.Empty;
    public string SM { get; set; } = string.Empty;
    public PipelineBlockModel? Pipeline { get; set; }
    public List<PassBlockModel> Passes { get; set; } = new();
    public List<FunctionCallModel> FunctionCalls { get; set; } = new();
}

public class ComputeShaderModel
{
    public string Name { get; set; } = string.Empty;
    public string SM { get; set; } = string.Empty;
    public DefinesBlockModel? Defines { get; set; }
    public IncludesBlockModel? Includes { get; set; }
    public KeywordsBlockModel? Keywords { get; set; }
    public HlslBlockModel? Hlsl { get; set; }
    public List<FunctionCallModel> FunctionCalls { get; set; } = new();
    public List<ShaderEntryModel> ShaderEntries { get; set; } = new();
}

public class PipelineBlockModel
{
    public Dictionary<string, string> Statements { get; set; } = new();
}

public class PassBlockModel
{
    public string Name { get; set; } = string.Empty;
    public PipelineBlockModel? LocalPipeline { get; set; }
    public DefinesBlockModel? Defines { get; set; }
    public IncludesBlockModel? Includes { get; set; }
    public KeywordsBlockModel? Keywords { get; set; }
    public HlslBlockModel? Hlsl { get; set; }
    public List<ShaderEntryModel> ShaderEntries { get; set; } = new();
}

public class DefinesBlockModel
{
    public List<string> Defines { get; set; } = new();
}

public class IncludesBlockModel
{
    public List<string> Includes { get; set; } = new();
}

public class KeywordsBlockModel
{
    public List<KeywordGroupModel> Groups { get; set; } = new();
}

public class KeywordGroupModel
{
    public string? Scope { get; set; }
    public List<string> Keywords { get; set; } = new();
}

public class HlslBlockModel
{
    public string Code { get; set; } = string.Empty;
}

public class ShaderEntryModel
{
    public string EntryType { get; set; } = string.Empty;  // "mesh", "pixel", "task", etc.
    public string ShaderPath { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
}

public class FunctionCallModel
{
    public string Name { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
}
