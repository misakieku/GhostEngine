using Ghost.Core;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Symbols;
using System.Text;

namespace Ghost.DSL.Composition;

public static class HLSLCodeGenerator
{
    /// <summary>
    /// Assembles the complete HLSL source code for a specific pass specialization,
    /// injecting virtual shaders, material properties, payload struct, mangled implementation structs,
    /// and pass-specific shader code.
    /// </summary>
    public static Result<string> GeneratePassHLSL(
        PassBlockSyntax pass,
        PassSpecialization specialization,
        string? payloadBody,
        string? propertiesCode,
        IReadOnlyDictionary<string, string>? virtualShaders,
        string? specificShaderPath = null,
        IReadOnlyList<string>? assetDirectories = null)
    {
        var sb = new StringBuilder();

        // 1. Virtual Shaders & Pre-defined Headers
        if (virtualShaders != null)
        {
            foreach (var kvp in virtualShaders)
            {
                sb.AppendLine(kvp.Value);
            }
        }

        // 2. Material Property Reflection Struct / ConstantBuffer
        if (!string.IsNullOrWhiteSpace(propertiesCode))
        {
            sb.AppendLine("#line 0 \"properties\"");
            sb.AppendLine(propertiesCode);
        }

        // 3. Payload Struct
        sb.AppendLine("#ifndef GHOST_PAYLOAD_DEFINED");
        sb.AppendLine("#define GHOST_PAYLOAD_DEFINED");
        if (!string.IsNullOrWhiteSpace(payloadBody))
        {
            sb.AppendLine("struct Payload");
            sb.AppendLine("{");
            sb.AppendLine(payloadBody);
            sb.AppendLine("};");
        }
        else
        {
            sb.AppendLine("struct Payload {};");
        }
        sb.AppendLine("#endif // GHOST_PAYLOAD_DEFINED");

        // 4. Mangled Concrete Implementation Structs
        foreach (var impl in specialization.Implementations)
        {
            var mangledName = SpecializationResolver.MangleSymbolName(impl.QualifiedName);
            var guardMacro = $"{mangledName.ToUpperInvariant()}_IMPL_DEFINED";

            sb.AppendLine($"#ifndef {guardMacro}");
            sb.AppendLine($"#define {guardMacro}");
            sb.AppendLine($"struct {mangledName}");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(impl.SourceFile))
            {
                sb.AppendLine($"#line 1 \"{impl.SourceFile.Replace('\\', '/')}\"");
            }
            sb.AppendLine(impl.Body);
            sb.AppendLine("};");
            sb.AppendLine($"#endif // {guardMacro}");
        }

        // 5. Pass Includes
        if (pass.Includes != null)
        {
            foreach (var inc in pass.Includes.Includes)
            {
                var trimmed = inc.TrimStart('/', '\\').Replace('\\', '/');
                sb.AppendLine($"#include \"{trimmed}\"");
            }
        }

        // 6. External or Inline Shader Code
        if (!string.IsNullOrEmpty(specificShaderPath))
        {
            var resolvedFile = ResolveShaderFilePath(specificShaderPath, assetDirectories);
            if (resolvedFile != null && File.Exists(resolvedFile))
            {
                var code = File.ReadAllText(resolvedFile);
                sb.AppendLine($"#line 1 \"{resolvedFile.Replace('\\', '/')}\"");
                sb.AppendLine(code);
            }
            else
            {
                // Fallback: emit an include directive if not resolved locally
                sb.AppendLine($"#include \"{specificShaderPath.TrimStart('/', '\\').Replace('\\', '/')}\"");
            }
        }
        else if (pass.Hlsl != null && !string.IsNullOrWhiteSpace(pass.Hlsl.Code))
        {
            sb.AppendLine("#line 1 \"inline_hlsl\"");
            sb.AppendLine(pass.Hlsl.Code);
        }

        return Result.Success(sb.ToString());
    }

    private static string? ResolveShaderFilePath(string relativeOrAbsPath, IReadOnlyList<string>? assetDirectories)
    {
        if (File.Exists(relativeOrAbsPath))
        {
            return relativeOrAbsPath;
        }

        if (assetDirectories != null)
        {
            var cleanRelative = relativeOrAbsPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            foreach (var dir in assetDirectories)
            {
                var full = Path.Combine(dir, cleanRelative);
                if (File.Exists(full))
                {
                    return full;
                }
            }
        }

        return null;
    }
}
