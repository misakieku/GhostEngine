using System.Collections.Generic;

namespace Ghost.DSL.ShaderParser.Syntax;

public enum InterfaceScope
{
    Pipeline,
    Shader
}

public class DSLDocumentSyntax
{
    public List<ModuleDeclarationSyntax> Modules { get; set; } = new();
    public List<ImportDeclarationSyntax> Imports { get; set; } = new();
    public List<InterfaceDeclarationSyntax> Interfaces { get; set; } = new();
    public List<ImplementationDeclarationSyntax> Implementations { get; set; } = new();
    public List<TemplateDeclarationSyntax> Templates { get; set; } = new();
    public List<ShaderDeclarationSyntax> Shaders { get; set; } = new();
    public List<ShaderProjectDeclarationSyntax> Projects { get; set; } = new();
    public List<PassBlockSyntax> Passes { get; set; } = new();
}

public class ModuleDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public List<ImportDeclarationSyntax> Imports { get; set; } = new();
    public List<InterfaceDeclarationSyntax> Interfaces { get; set; } = new();
    public List<ImplementationDeclarationSyntax> Implementations { get; set; } = new();
    public List<TemplateDeclarationSyntax> Templates { get; set; } = new();
    public List<ShaderDeclarationSyntax> Shaders { get; set; } = new();
}

public class ImportDeclarationSyntax
{
    public string ModuleName { get; set; } = string.Empty;
}

public class InterfaceDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public InterfaceScope Scope { get; set; }
    public bool IsClosed { get; set; }
    public bool IsExported { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class ImplementationDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public string InterfaceName { get; set; } = string.Empty;
    public bool IsExported { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Provider { get; set; }
}

public class TemplateDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public bool IsExported { get; set; }
    public PropertiesBlockSyntax? Properties { get; set; }
    public List<TemplateSlotSyntax> Slots { get; set; } = new();
    public List<PassBlockSyntax> Passes { get; set; } = new();
    public PipelineBlockSyntax? Pipeline { get; set; }
    public string ShaderModel { get; set; } = string.Empty;
    public List<FunctionCallSyntax> FunctionCalls { get; set; } = new();
}

public class TemplateSlotSyntax
{
    public string InterfaceName { get; set; } = string.Empty;
    public string? DefaultImplementationName { get; set; }
}

public class ShaderDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public bool IsExported { get; set; }
    public PropertiesBlockSyntax? Properties { get; set; }
    public PayloadBlockSyntax? Payload { get; set; }
    public List<ImplementationDeclarationSyntax> Implementations { get; set; } = new();
    public BindBlockSyntax? Bind { get; set; }
    public List<PassBlockSyntax> Passes { get; set; } = new();
    public PipelineBlockSyntax? Pipeline { get; set; }
    public string ShaderModel { get; set; } = string.Empty;
    public List<FunctionCallSyntax> FunctionCalls { get; set; } = new();
}

public class PropertiesBlockSyntax
{
    public List<PropertyDeclarationSyntax> Declarations { get; set; } = new();
}

public class PropertyDeclarationSyntax
{
    public string TypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ArrayLength { get; set; } = 0;
    public int Line { get; set; }
    public int Column { get; set; }
}

public class PayloadBlockSyntax
{
    public string Body { get; set; } = string.Empty;
}

public class BindBlockSyntax
{
    public List<BindingSyntax> Bindings { get; set; } = new();
}

public class BindingSyntax
{
    public string InterfaceName { get; set; } = string.Empty;
    public string ImplementationName { get; set; } = string.Empty;
}

public class ShaderProjectDeclarationSyntax
{
    public string Name { get; set; } = string.Empty;
    public List<string> Modules { get; set; } = new();
    public List<string> Targets { get; set; } = new();
}

public class ComposeBlockSyntax
{
    public List<string> Interfaces { get; set; } = new();
}

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
    public PropertiesBlockSyntax? Properties { get; set; }
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
    public ComposeBlockSyntax? Compose { get; set; }
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
    public string EntryType { get; set; } = string.Empty;
    public string ShaderPath { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
}

public class FunctionCallSyntax
{
    public string Name { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
}
