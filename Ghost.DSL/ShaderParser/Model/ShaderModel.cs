namespace Ghost.DSL.ShaderParser.Model;

public class ShaderModel
{
    public string Name { get; set; } = string.Empty;
    public PropertiesBlockModel? Properties { get; set; }
    public PipelineBlockModel? Pipeline { get; set; }
    public List<PassBlockModel> Passes { get; set; } = new();
    public List<FunctionCallModel> FunctionCalls { get; set; } = new();
}

public class PropertiesBlockModel
{
    public List<PropertyDeclarationModel> Properties { get; set; } = new();
}

public class PropertyDeclarationModel
{
    public string? Scope { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Initializer { get; set; } = new();
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
