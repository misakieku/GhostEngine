using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Ghost.AssetForge.Core.Services;

public class ProjectService
{
    private readonly BakerRegistry _bakerRegistry;

    public ProjectService(BakerRegistry bakerRegistry)
    {
        _bakerRegistry = bakerRegistry;
    }

    public Project? CurrentProject { get; private set; }

    public IReadOnlyList<string> AssetDirectories { get; private set; } = Array.Empty<string>();
    public string CacheDirectory { get; private set; } = string.Empty;
    public string BuildDirectory { get; private set; } = string.Empty;
    public IReadOnlyList<string> ShaderMetadataPaths { get; private set; } = Array.Empty<string>();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public event Action? OnProjectLoaded;

    public void InitializeFromArgs(IEnumerable<string> assetDirs, string cacheDir, string buildDir, IEnumerable<string> shaderMetadataPaths)
    {
        AssetDirectories = assetDirs.ToArray();
        CacheDirectory = cacheDir;
        BuildDirectory = buildDir;
        ShaderMetadataPaths = shaderMetadataPaths.ToArray();

        foreach (var dir in AssetDirectories)
        {
            Directory.CreateDirectory(dir);
            Logger.Info($"Including asset directory: {dir}");
        }

        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        foreach (var path in ShaderMetadataPaths)
        {
            var shaderDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(shaderDir) && !Directory.Exists(shaderDir))
            {
                Directory.CreateDirectory(shaderDir);
            }

            Logger.Info($"Including shader metadata path: {path}");
        }

        CurrentProject = new Project
        {
            Name = "CLI_Project",
            RootPath = Path.GetDirectoryName(AssetDirectories.FirstOrDefault()) ?? string.Empty
        };

        OnProjectLoaded?.Invoke();
    }

    public void CreateProject(string folderPath, string projectName)
    {
        var project = new Project
        {
            Name = projectName,
            RootPath = folderPath
        };

        CurrentProject = project;

        var defaultAssetDir = Path.Combine(folderPath, "Asset");
        AssetDirectories = new[] { defaultAssetDir };
        CacheDirectory = Path.Combine(folderPath, "obj", "AssetCache");
        BuildDirectory = Path.Combine(folderPath, "bin", "Assets");
        var singleShaderPath = Path.Combine(folderPath, "obj", "shader_properties.json");
        ShaderMetadataPaths = new[] { singleShaderPath };

        Directory.CreateDirectory(folderPath);
        Directory.CreateDirectory(defaultAssetDir);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        var csprojPath = Path.Combine(folderPath, $"{projectName}.csproj");
        if (!File.Exists(csprojPath))
        {
            // Create a dummy csproj just to mark the directory
            File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>");
        }

        OnProjectLoaded?.Invoke();
    }

    public void OpenProject(string path, string configuration = "Debug")
    {
        var csprojPath = path;
        var folderPath = path;

        if (File.Exists(path) && path.EndsWith(".csproj"))
        {
            folderPath = Path.GetDirectoryName(path) ?? string.Empty;
        }
        else if (Directory.Exists(path))
        {
            var csprojFiles = Directory.GetFiles(path, "*.csproj");
            if (csprojFiles.Length > 0)
                csprojPath = csprojFiles[0];
            else
                throw new FileNotFoundException($"No .csproj found in {path}");
        }
        else
        {
            throw new DirectoryNotFoundException($"Path {path} does not exist");
        }

        var project = new Project
        {
            Name = Path.GetFileNameWithoutExtension(csprojPath),
            RootPath = folderPath
        };

        CurrentProject = project;

        var defaultAssetDir = Path.Combine(folderPath, "Asset");
        CacheDirectory = Path.Combine(folderPath, "obj", "AssetCache");
        BuildDirectory = Path.Combine(folderPath, "bin", "Assets");
        var singleShaderPathOpen = Path.Combine(folderPath, "obj", "shader_properties.json");
        ShaderMetadataPaths = new[] { singleShaderPathOpen };

        if (File.Exists(csprojPath))
        {
            try
            {
                var doc = XDocument.Load(csprojPath);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var targetFramework = doc.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value ?? "net10.0";

                string ReplaceMacros(string value)
                {
                    if (string.IsNullOrEmpty(value)) return value;
                    value = value.Replace("$(MSBuildProjectDirectory)", folderPath);
                    value = value.Replace("$(Configuration)", configuration);
                    value = value.Replace("$(TargetFramework)", targetFramework);
                    value = value.Replace("$(IntermediateOutputPath)", $@"obj\{configuration}\{targetFramework}\");
                    value = value.Replace("$(TargetDir)", $@"bin\{configuration}\{targetFramework}\");
                    return value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                }

                var assetDirNode = doc.Descendants(ns + "GhostAssetDir").FirstOrDefault();
                if (assetDirNode != null) defaultAssetDir = Path.GetFullPath(Path.Combine(folderPath, ReplaceMacros(assetDirNode.Value)));

                var cacheDirNode = doc.Descendants(ns + "GhostAssetCacheDir").FirstOrDefault();
                if (cacheDirNode != null) CacheDirectory = Path.GetFullPath(Path.Combine(folderPath, ReplaceMacros(cacheDirNode.Value)));

                var buildDirNode = doc.Descendants(ns + "GhostAssetBuildDir").FirstOrDefault();
                if (buildDirNode != null) BuildDirectory = Path.GetFullPath(Path.Combine(folderPath, ReplaceMacros(buildDirNode.Value)));

                var shaderMetaNode = doc.Descendants(ns + "GhostShaderMetadataPath").FirstOrDefault();
                if (shaderMetaNode != null)
                {
                    singleShaderPathOpen = Path.GetFullPath(Path.Combine(folderPath, ReplaceMacros(shaderMetaNode.Value)));
                    ShaderMetadataPaths = new[] { singleShaderPathOpen };
                }
            }
            catch (Exception ex)
            {
                Ghost.Core.Logger.Warning($"Failed to parse csproj for paths, using defaults. {ex.Message}");
            }
        }

        AssetDirectories = new[] { defaultAssetDir };

        Directory.CreateDirectory(defaultAssetDir);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        var shaderDir = Path.GetDirectoryName(singleShaderPathOpen);
        if (!string.IsNullOrEmpty(shaderDir) && !Directory.Exists(shaderDir))
        {
            Directory.CreateDirectory(shaderDir);
        }

        OnProjectLoaded?.Invoke();
    }

    public void SaveProject()
    {
        // Settings are now stored in .csproj or standard MSBuild properties,
        // so we don't save a proprietary project.json anymore.
    }

    /// <summary>
    /// Returns an immutable snapshot of the currently loaded project's configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">No project is currently loaded.</exception>
    public ProjectContext GetContext()
    {
        var project = CurrentProject ?? throw new InvalidOperationException("No project loaded.");
        return new ProjectContext(project, AssetDirectories, CacheDirectory, BuildDirectory, ShaderMetadataPaths);
    }

    public void ImportAsset(string sourceFilePath, string targetVirtualPath)
    {
        if (CurrentProject == null)
        {
            return;
        }

        var primaryAssetDir = AssetDirectories.LastOrDefault();
        if (string.IsNullOrEmpty(primaryAssetDir)) return;

        var assetDir = Path.Combine(primaryAssetDir, targetVirtualPath);
        Directory.CreateDirectory(assetDir);

        var fileName = Path.GetFileName(sourceFilePath);
        var destFilePath = Path.Combine(assetDir, fileName);

        File.Copy(sourceFilePath, destFilePath, overwrite: true);

        var metaFilePath = destFilePath + ".meta";
        if (!File.Exists(metaFilePath))
        {
            var extension = Path.GetExtension(fileName);
            var type = _bakerRegistry.DetectAssetType(extension);

            var meta = new AssetMetadata
            {
                Id = Guid.NewGuid(),
                Type = type,
                Settings = _bakerRegistry.CreateDefaultSettings(extension)
            };

            File.WriteAllText(metaFilePath, JsonSerializer.Serialize(meta, _jsonOptions));
        }
    }

    public void SaveMetadata(string metaFilePath, AssetMetadata metadata)
    {
        File.WriteAllText(metaFilePath, JsonSerializer.Serialize(metadata, _jsonOptions));
    }

    public AssetMetadata? LoadMetadata(string metaFilePath)
    {
        if (!File.Exists(metaFilePath)) return null;
        var json = File.ReadAllText(metaFilePath);
        return JsonSerializer.Deserialize<AssetMetadata>(json, _jsonOptions);
    }
}
