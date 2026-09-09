using Ghost.Core;
using Ghost.DSL.Models;
using Ghost.DSL.ShaderCompiler;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.AssetForge.Core.Services;

/// <summary>
/// Recursively discovers all direct and transitive file dependencies for shaders (.gshdr, .gcomp, .hlsl),
/// including DSL <c>includes</c> blocks and HLSL <c>#include</c> directives to arbitrary depth.
/// </summary>
public static partial class ShaderIncludeResolver
{
    private static readonly Regex s_includeRegex = new(
        @"^[ \t]*#[ \t]*include[ \t]*[""<]([^"">]+)["">]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex s_dslIncludesBlockRegex = new(
        @"includes\s*\{([^}]*)\}",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex s_dslStringLiteralRegex = new(
        @"""([^""]+)""",
        RegexOptions.Compiled);

    /// <summary>
    /// Resolves all physical file dependencies for a shader source file and its code.
    /// </summary>
    /// <param name="sourceFile">The absolute or relative path to the primary shader source file.</param>
    /// <param name="shaderCode">The text content of the shader source file.</param>
    /// <param name="assetDirectories">Search roots for include resolution (-I directories).</param>
    /// <param name="virtualShaders">In-memory virtual shader table from metadata (virtual includes will be skipped).</param>
    /// <returns>A collection of normalized full paths of all resolved dependency files on disk.</returns>
    public static HashSet<string> ResolveDependencies(
        string sourceFile,
        string shaderCode,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders = null)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var currentFileDir = Path.GetDirectoryName(Path.GetFullPath(sourceFile)) ?? string.Empty;
        var ext = Path.GetExtension(sourceFile).ToLowerInvariant();

        if (ext == ".gshdr")
        {
            ResolveGraphicsShaderDsl(sourceFile, shaderCode, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
        }
        else if (ext == ".gcomp")
        {
            ResolveComputeShaderDsl(sourceFile, shaderCode, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
        }
        else
        {
            // Raw HLSL / other text
            ResolveHlslFile(shaderCode, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
        }

        return dependencies;
    }

    private static void ResolveGraphicsShaderDsl(
        string sourceFile,
        string shaderCode,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        // Try structured DSL AST parsing first
        var syntaxResult = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderCode);
        if (syntaxResult.IsSuccess)
        {
            var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntaxResult.Value);
            if (semanticsResult.IsSuccess && semanticsResult.Value != null)
            {
                var semantics = semanticsResult.Value;

                // DSL top-level or pass includes
                if (semantics.passes != null)
                {
                    foreach (var pass in semantics.passes)
                    {
                        if (pass.includes != null)
                        {
                            foreach (var includePath in pass.includes)
                            {
                                ResolveAndRecurse(includePath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                            }
                        }

                        if (!string.IsNullOrEmpty(pass.hlsl))
                        {
                            ResolveHlslFile(pass.hlsl, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                        }

                        ResolvePassShaderPath(pass.amplificationShader.shaderPath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                        ResolvePassShaderPath(pass.meshShader.shaderPath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                        ResolvePassShaderPath(pass.pixelShader.shaderPath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                    }
                }

                if (!string.IsNullOrEmpty(semantics.hlsl))
                {
                    ResolveHlslFile(semantics.hlsl, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                }

                return;
            }
        }

        // Fallback if AST parse fails (e.g. transient syntax errors in other blocks while editing)
        ScanDslFallback(shaderCode, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
    }

    private static void ResolveComputeShaderDsl(
        string sourceFile,
        string shaderCode,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        var syntaxResult = DSLShaderCompiler.ParseComputeShaderSyntax(shaderCode);
        if (syntaxResult.IsSuccess)
        {
            var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntaxResult.Value);
            if (semanticsResult.IsSuccess && semanticsResult.Value != null)
            {
                var semantics = semanticsResult.Value;

                if (semantics.includes != null)
                {
                    foreach (var includePath in semantics.includes)
                    {
                        ResolveAndRecurse(includePath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                    }
                }

                if (!string.IsNullOrEmpty(semantics.hlsl))
                {
                    ResolveHlslFile(semantics.hlsl, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                }

                if (semantics.entryPoints != null)
                {
                    foreach (var ep in semantics.entryPoints)
                    {
                        ResolvePassShaderPath(ep.shaderPath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
                    }
                }

                return;
            }
        }

        ScanDslFallback(shaderCode, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
    }

    private static void ResolvePassShaderPath(
        string? shaderPath,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        if (!string.IsNullOrEmpty(shaderPath) && shaderPath != "hlsl_block")
        {
            ResolveAndRecurse(shaderPath, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
        }
    }

    private static void ScanDslFallback(
        string text,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        // Extract includes { ... } blocks
        foreach (Match match in s_dslIncludesBlockRegex.Matches(text))
        {
            var blockContent = match.Groups[1].Value;
            foreach (Match strMatch in s_dslStringLiteralRegex.Matches(blockContent))
            {
                ResolveAndRecurse(strMatch.Groups[1].Value, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
            }
        }

        // Also scan any #include directives in the raw text
        ResolveHlslFile(text, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
    }

    private static void ResolveHlslFile(
        string hlslCode,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        var cleanCode = StripComments(hlslCode);

        foreach (Match match in s_includeRegex.Matches(cleanCode))
        {
            var includeTarget = match.Groups[1].Value.Trim();
            ResolveAndRecurse(includeTarget, currentFileDir, assetDirectories, virtualShaders, visited, dependencies);
        }
    }

    private static void ResolveAndRecurse(
        string includeTarget,
        string currentFileDir,
        IReadOnlyList<string> assetDirectories,
        IReadOnlyDictionary<string, string>? virtualShaders,
        HashSet<string> visited,
        HashSet<string> dependencies)
    {
        if (string.IsNullOrWhiteSpace(includeTarget))
        {
            return;
        }

        var normalizedTarget = includeTarget.Replace('\\', '/');

        // Check if this targets a virtual shader in memory
        if (virtualShaders != null)
        {
            var stripped = normalizedTarget.TrimStart('/');
            if (virtualShaders.ContainsKey("/" + stripped) ||
                virtualShaders.ContainsKey(stripped) ||
                virtualShaders.ContainsKey(normalizedTarget))
            {
                // Inlined from metadata; no disk dependency
                return;
            }
        }

        var resolvedFilePath = ResolvePath(normalizedTarget, currentFileDir, assetDirectories);
        if (resolvedFilePath == null)
        {
            return;
        }

        var fullPath = Path.GetFullPath(resolvedFilePath).Replace('\\', '/');
        if (!visited.Add(fullPath))
        {
            // Already scanned; prevent infinite cycles
            return;
        }

        dependencies.Add(fullPath);

        // Recurse into included file if it exists
        if (File.Exists(fullPath))
        {
            try
            {
                var content = File.ReadAllText(fullPath);
                var nextDir = Path.GetDirectoryName(fullPath) ?? currentFileDir;
                ResolveHlslFile(content, nextDir, assetDirectories, virtualShaders, visited, dependencies);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to read included shader file '{fullPath}': {ex.Message}");
            }
        }
    }

    private static string? ResolvePath(string includeTarget, string currentFileDir, IReadOnlyList<string> assetDirectories)
    {
        // 1. If target is rooted with leading slash: resolve against assetDirectories
        if (includeTarget.StartsWith('/'))
        {
            var relative = includeTarget.TrimStart('/');
            foreach (var assetDir in assetDirectories)
            {
                var candidate = Path.Combine(assetDir, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        else
        {
            // 2. Relative include: first check current file directory
            if (!string.IsNullOrEmpty(currentFileDir))
            {
                var candidate = Path.Combine(currentFileDir, includeTarget);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // 3. Fallback: search each asset directory
            foreach (var assetDir in assetDirectories)
            {
                var candidate = Path.Combine(assetDir, includeTarget);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Strips single-line (//) and multi-line (/* ... */) comments from HLSL code,
    /// preserving line breaks so #include regex line anchors remain accurate.
    /// </summary>
    public static string StripComments(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        var len = input.Length;
        var i = 0;

        while (i < len)
        {
            var c = input[i];

            // Check for string literal
            if (c == '"')
            {
                sb.Append(c);
                i++;
                while (i < len)
                {
                    var sc = input[i];
                    sb.Append(sc);
                    if (sc == '\\' && i + 1 < len)
                    {
                        i++;
                        sb.Append(input[i]);
                    }
                    else if (sc == '"')
                    {
                        break;
                    }
                    i++;
                }
                i++;
                continue;
            }

            // Check for comment start
            if (c == '/' && i + 1 < len)
            {
                var next = input[i + 1];
                if (next == '/')
                {
                    // Single-line comment: skip until newline
                    i += 2;
                    while (i < len && input[i] != '\n' && input[i] != '\r')
                    {
                        i++;
                    }
                    continue;
                }
                if (next == '*')
                {
                    // Multi-line block comment: skip until */
                    i += 2;
                    while (i + 1 < len && !(input[i] == '*' && input[i + 1] == '/'))
                    {
                        if (input[i] == '\n')
                        {
                            sb.Append('\n'); // Preserve line breaks
                        }
                        i++;
                    }
                    i += 2; // Skip */
                    continue;
                }
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }
}
