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

    public BakerRegistry BakerRegistry => _bakerRegistry;

    public Project? CurrentProject { get; private set; }

    public IReadOnlyList<string> AssetDirectories { get; private set; } = Array.Empty<string>();
    public string CacheDirectory { get; private set; } = string.Empty;
    public string BuildDirectory { get; private set; } = string.Empty;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public event Action? OnProjectLoaded;

    public void SetDefaultScene(string defaultScene)
    {
        if (CurrentProject == null)
        {
            return;
        }

        CurrentProject = CurrentProject with { DefaultScene = defaultScene ?? string.Empty };
    }

    public void InitializeFromArgs(IEnumerable<string> assetDirs, string cacheDir, string buildDir)
    {
        AssetDirectories = assetDirs.ToArray();
        CacheDirectory = cacheDir;
        BuildDirectory = buildDir;

        foreach (var dir in AssetDirectories)
        {
            Directory.CreateDirectory(dir);
            Logger.Info($"Including asset directory: {dir}");
        }

        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        CurrentProject = new Project
        {
            Name = "CLI_Project",
            RootPath = Path.GetDirectoryName(AssetDirectories.FirstOrDefault()) ?? string.Empty
        };

        OnProjectLoaded?.Invoke();
    }

    public void CreateProject(string folderPath, string projectName, string? engineRoot = null)
    {
        folderPath = Path.GetFullPath(folderPath);
        var defaultAssetDir = Path.Combine(folderPath, "Assets");
        var cacheRelDir = Path.Combine("obj", "AssetCache");
        var buildRelDir = Path.Combine("bin", "Assets");
        var ghostProjPath = Path.Combine(folderPath, $"{projectName}{Project.EXTENSION}");
        var csprojFileName = $"{projectName}.csproj";

        var project = new Project
        {
            SchemaVersion = 1,
            Name = projectName,
            CsprojPath = csprojFileName,
            AssetDirectories = new[] { "Assets" },
            CacheDirectory = cacheRelDir.Replace('\\', '/'),
            BuildDirectory = buildRelDir.Replace('\\', '/'),
            RootPath = folderPath,
            ProjectFilePath = ghostProjPath
        };

        CurrentProject = project;
        AssetDirectories = new[] { defaultAssetDir };
        CacheDirectory = Path.Combine(folderPath, cacheRelDir);
        BuildDirectory = Path.Combine(folderPath, buildRelDir);

        Directory.CreateDirectory(folderPath);
        Directory.CreateDirectory(defaultAssetDir);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        SaveProject();
        ScaffoldGameProjectFiles(folderPath, projectName, csprojFileName, ResolveEngineRoot(engineRoot));

        OnProjectLoaded?.Invoke();
    }

    public void OpenProject(string path, string configuration = "Debug")
    {
        path = Path.GetFullPath(path);

        if (File.Exists(path) && path.EndsWith(Project.EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            LoadGhostProjectFile(path);
            return;
        }

        if (Directory.Exists(path))
        {
            var ghostProjFiles = Directory.GetFiles(path, $"*{Project.EXTENSION}");
            if (ghostProjFiles.Length > 0)
            {
                LoadGhostProjectFile(ghostProjFiles[0]);
                return;
            }
        }

        var csprojPath = path;
        var folderPath = path;

        if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            folderPath = Path.GetDirectoryName(path) ?? string.Empty;
        }
        else if (Directory.Exists(path))
        {
            var csprojFiles = Directory.GetFiles(path, "*.csproj");
            if (csprojFiles.Length > 0)
                csprojPath = csprojFiles[0];
            else
                throw new FileNotFoundException($"No {Project.EXTENSION} or .csproj found in {path}");
        }
        else
        {
            throw new DirectoryNotFoundException($"Path {path} does not exist");
        }

        var project = new Project
        {
            Name = Path.GetFileNameWithoutExtension(csprojPath),
            CsprojPath = Path.GetFileName(csprojPath),
            AssetDirectories = new[] { "Asset" },
            RootPath = folderPath
        };

        CurrentProject = project;

        var defaultAssetDir = Path.Combine(folderPath, "Asset");
        CacheDirectory = Path.Combine(folderPath, "obj", "AssetCache");
        BuildDirectory = Path.Combine(folderPath, "bin", "Assets");

        if (File.Exists(csprojPath))
        {
            try
            {
                var doc = XDocument.Load(csprojPath);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var targetFramework = doc.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value ?? "net11.0";

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

        OnProjectLoaded?.Invoke();
    }

    public void SaveProject()
    {
        if (CurrentProject == null)
        {
            return;
        }

        var filePath = CurrentProject.ProjectFilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            if (string.IsNullOrWhiteSpace(CurrentProject.RootPath))
            {
                return;
            }

            filePath = Path.Combine(CurrentProject.RootPath, $"{CurrentProject.Name}{Project.EXTENSION}");
            CurrentProject.ProjectFilePath = filePath;
        }

        var parentDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        File.WriteAllText(filePath, JsonSerializer.Serialize(CurrentProject, _jsonOptions));
    }

    private void LoadGhostProjectFile(string ghostProjPath)
    {
        var folderPath = Path.GetDirectoryName(ghostProjPath) ?? string.Empty;
        var json = File.ReadAllText(ghostProjPath);
        var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions)
            ?? throw new InvalidDataException($"Failed to parse project file: {ghostProjPath}");

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            project = project with { Name = Path.GetFileNameWithoutExtension(ghostProjPath) };
        }

        project.RootPath = folderPath;
        project.ProjectFilePath = ghostProjPath;

        var rawAssetDirs = project.AssetDirectories is { Length: > 0 } ? project.AssetDirectories : new[] { "Assets" };
        var resolvedAssetDirs = new string[rawAssetDirs.Length];
        for (var i = 0; i < rawAssetDirs.Length; i++)
        {
            var resolved = ResolveProjectPath(folderPath, rawAssetDirs[i]);
            resolvedAssetDirs[i] = resolved;
            Directory.CreateDirectory(resolved);
        }

        var relCache = string.IsNullOrWhiteSpace(project.CacheDirectory) ? Path.Combine("obj", "AssetCache") : project.CacheDirectory;
        var relBuild = string.IsNullOrWhiteSpace(project.BuildDirectory) ? Path.Combine("bin", "Assets") : project.BuildDirectory;

        CurrentProject = project;
        AssetDirectories = resolvedAssetDirs;
        CacheDirectory = ResolveProjectPath(folderPath, relCache);
        BuildDirectory = ResolveProjectPath(folderPath, relBuild);

        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(BuildDirectory);

        OnProjectLoaded?.Invoke();
    }

    private static string ResolveProjectPath(string rootPath, string relativeOrAbsolute)
    {
        var normalized = relativeOrAbsolute.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(rootPath, normalized));
    }

    private static string ResolveEngineRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot) && File.Exists(Path.Combine(explicitRoot, "src", "GhostEngine.targets")))
        {
            return Path.GetFullPath(explicitRoot);
        }

        var envRoot = Environment.GetEnvironmentVariable("GHOST_ENGINE_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot) && File.Exists(Path.Combine(envRoot, "src", "GhostEngine.targets")))
        {
            return Path.GetFullPath(envRoot);
        }

        string?[] searchRoots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetDirectoryName(typeof(ProjectService).Assembly.Location),
        };

        foreach (var start in searchRoots)
        {
            var dir = start;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "src", "GhostEngine.targets")))
                {
                    return Path.GetFullPath(dir);
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
        }

        return string.Empty;
    }

    private static void ScaffoldGameProjectFiles(string folderPath, string projectName, string csprojFileName, string engineRoot)
    {
        var rootNamespace = SanitizeIdentifier(projectName);
        var csprojPath = Path.Combine(folderPath, csprojFileName);
        if (!File.Exists(csprojPath))
        {
            var escapedRoot = System.Security.SecurityElement.Escape(engineRoot) ?? string.Empty;
            var csprojContent = $"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Configurations>Debug;Release;Debug_Editor;Release_Editor</Configurations>
    <OutputType>WinExe</OutputType>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <RootNamespace>{rootNamespace}</RootNamespace>
    <IsGameProject>true</IsGameProject>
    <GhostAssetDir>$(MSBuildProjectDirectory)\Assets</GhostAssetDir>
    <GhostEngineRoot Condition="'$(GhostEngineRoot)' == ''">{escapedRoot}</GhostEngineRoot>
  </PropertyGroup>

  <ItemGroup>
    <AdditionalFiles Include="**\*.gshdr" />
    <AdditionalFiles Include="**\*.gcomp" />
    <AdditionalFiles Include="**\*.ggraph" />
  </ItemGroup>

  <ItemGroup Condition="'$(GhostEngineRoot)' != '' and Exists('$(GhostEngineRoot)\src\Runtime\Ghost.Engine\Ghost.Engine.csproj')">
    <ProjectReference Include="$(GhostEngineRoot)\src\Runtime\Ghost.Engine\Ghost.Engine.csproj" />
    <ProjectReference Include="$(GhostEngineRoot)\src\Runtime\Ghost.Graphics.D3D12\Ghost.Graphics.D3D12.csproj" />
    <ProjectReference Include="$(GhostEngineRoot)\src\Runtime\Ghost.Generator\Ghost.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>

  <Import Project="$(GhostEngineRoot)\src\GhostEngine.targets" Condition="'$(GhostEngineRoot)' != '' and Exists('$(GhostEngineRoot)\src\GhostEngine.targets')" />

</Project>
""";
            File.WriteAllText(csprojPath, csprojContent);
        }

        var programPath = Path.Combine(folderPath, "Program.cs");
        if (!File.Exists(programPath))
        {
            var programContent = $$"""
using Ghost.Engine;

namespace {{rootNamespace}};

internal static class Program
{
    private static void Main()
    {
        EngineRunner.Run<LaunchProfile>();
    }
}
""";
            File.WriteAllText(programPath, programContent);
        }

        var launchProfilePath = Path.Combine(folderPath, "LaunchProfile.cs");
        if (!File.Exists(launchProfilePath))
        {
            var escapedTitle = projectName.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var launchProfileContent = $$"""
using Ghost.Engine;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace {{rootNamespace}};

internal sealed class LaunchProfile : IEngineLaunchProfile
{
    private World? _world;

    private readonly GhostRenderPipelineSettings _renderPipelineSettings = new GhostRenderPipelineSettings
    {
        EnableRayTracing = false,
        MaxVisibleMeshletsOnScreen = 524_288,
        MeshletLodErrorThreshold = 1.0f,
        InstanceCullingThreshold = 2.0f,
        DebugMode = RenderPipelineDebugMode.None,
    };

    public EngineDesc GetEngineDesc()
    {
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc
            {
                Width = 1280,
                Height = 720,
                Title = "{{escapedTitle}}",
            },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Math.Max(2, Environment.ProcessorCount - 2),
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 16384,
            },
        };
    }

    public GraphicsDesc GetGraphicsDesc()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
        {
            throw new PlatformNotSupportedException("Direct3D 12 requires Windows 10 version 19041 or later.");
        }

#if DEBUG
        const bool enableDebugLayer = true;
#else
        const bool enableDebugLayer = false;
#endif

        var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc(), enableDebugLayer);
        return new GraphicsDesc
        {
            FrameBufferCount = 2,
            GraphicsEngine = graphicsEngine,
            RenderPipelineSettings = _renderPipelineSettings,
        };
    }

    public IContentProvider GetContentProvider()
    {
        return new RuntimeContentProvider("Assets/manifest.json");
    }

    public void OnEngineInitialized(EngineCore engine)
    {
        _world = World.Create(engine.JobScheduler, 1024);
    }

    public void ProcessEvent(SDL_Event e)
    {
    }

    public void OnEngineShutdown(EngineCore engine)
    {
        if (_world != null)
        {
            World.Destroy(_world);
            _world = null;
        }
    }
}
""";
            File.WriteAllText(launchProfilePath, launchProfileContent);
        }

        var gitignorePath = Path.Combine(folderPath, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, "bin/\nobj/\n.vs/\n.ghoststudio/\n");
        }
    }

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "GhostGame";
        }

        Span<char> buffer = stackalloc char[name.Length];
        var len = 0;
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                buffer[len++] = c;
            }
            else if (len == 0 || buffer[len - 1] != '_')
            {
                buffer[len++] = '_';
            }
        }

        var result = new string(buffer[..len]).Trim('_');
        if (string.IsNullOrEmpty(result))
        {
            return "GhostGame";
        }

        return char.IsDigit(result[0]) ? "_" + result : result;
    }

    /// <summary>
    /// Returns an immutable snapshot of the currently loaded project's configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">No project is currently loaded.</exception>
    public readonly record struct ImportedAssetInfo(string DestinationPath, string RelativePath, bool HasBaker);

    /// <summary>
    /// Resolves the engine's shared <c>Assets</c> directory when running from a source checkout, if present.
    /// </summary>
    public static string ResolveEngineAssetDirectory()
    {
        var engineRoot = ResolveEngineRoot(null);
        if (string.IsNullOrEmpty(engineRoot))
        {
            return string.Empty;
        }

        var candidate = Path.Combine(engineRoot, "src", "Runtime", "Ghost.Engine", "Assets");
        return Directory.Exists(candidate) ? Path.GetFullPath(candidate) : string.Empty;
    }

    public ProjectContext GetContext()
    {
        var project = CurrentProject ?? throw new InvalidOperationException("No project loaded.");
        return new ProjectContext(project, AssetDirectories, CacheDirectory, BuildDirectory);
    }

    public void ImportAsset(string sourceFilePath, string targetVirtualPath)
    {
        ImportAssetFile(sourceFilePath, targetVirtualPath);
    }

    /// <summary>
    /// Copies a source file into the primary asset directory under <paramref name="targetVirtualPath"/>
    /// and creates default <c>.meta</c> settings when a baker is registered for its extension.
    /// </summary>
    public ImportedAssetInfo? ImportAssetFile(string sourceFilePath, string targetVirtualPath)
    {
        if (CurrentProject == null)
        {
            return null;
        }

        var primaryAssetDir = AssetDirectories.LastOrDefault();
        if (string.IsNullOrEmpty(primaryAssetDir))
        {
            return null;
        }

        var normalizedSubdir = string.IsNullOrWhiteSpace(targetVirtualPath) || targetVirtualPath == "."
            ? string.Empty
            : targetVirtualPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var assetDir = string.IsNullOrEmpty(normalizedSubdir) ? primaryAssetDir : Path.Combine(primaryAssetDir, normalizedSubdir);
        Directory.CreateDirectory(assetDir);

        var fileName = Path.GetFileName(sourceFilePath);
        var destFilePath = Path.GetFullPath(Path.Combine(assetDir, fileName));
        var fullSourcePath = Path.GetFullPath(sourceFilePath);

        if (!string.Equals(fullSourcePath, destFilePath, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(fullSourcePath, destFilePath, overwrite: true);
        }

        var extension = Path.GetExtension(fileName);
        var hasBaker = _bakerRegistry.HasBaker(extension);

        if (hasBaker)
        {
            var metaFilePath = destFilePath + ".meta";
            if (!File.Exists(metaFilePath))
            {
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

        var relativePath = Path.GetRelativePath(primaryAssetDir, destFilePath).Replace('\\', '/');
        return new ImportedAssetInfo(destFilePath, relativePath, hasBaker);
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

    public string SerializeMetadata(AssetMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata, _jsonOptions);
    }

    public AssetMetadata? DeserializeMetadata(string json)
    {
        return JsonSerializer.Deserialize<AssetMetadata>(json, _jsonOptions);
    }

    /// <summary>
    /// Loads existing metadata for <paramref name="assetFilePath"/> or constructs default metadata
    /// when a baker is registered for its extension.
    /// </summary>
    public AssetMetadata? GetOrCreateMetadata(string assetFilePath)
    {
        var metaFilePath = assetFilePath + ".meta";
        var existing = LoadMetadata(metaFilePath);
        if (existing != null && existing.Settings != null)
        {
            return existing;
        }

        var ext = Path.GetExtension(assetFilePath);
        if (!_bakerRegistry.HasBaker(ext))
        {
            return existing;
        }

        return new AssetMetadata
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            Type = _bakerRegistry.DetectAssetType(ext),
            Settings = _bakerRegistry.CreateDefaultSettings(ext)
        };
    }
}
