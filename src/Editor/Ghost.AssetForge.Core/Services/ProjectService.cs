using System.Text.Json;
using System.Text.Json.Serialization;
using Ghost.Core;
using Ghost.AssetForge.Core.Models;

namespace Ghost.AssetForge.Core.Services;

public class ProjectService
{
    private static readonly Lazy<ProjectService> s_instance = new(() => new ProjectService());
    public static ProjectService Instance => s_instance.Value;
    
    public Project? CurrentProject { get; private set; }
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public event Action? OnProjectLoaded;

    public void CreateProject(string folderPath, string projectName)
    {
        var project = new Project
        {
            Name = projectName,
            RootPath = folderPath
        };
        
        Directory.CreateDirectory(folderPath);
        var assetDir = Path.Combine(folderPath, "Asset");
        Directory.CreateDirectory(assetDir);
        Directory.CreateDirectory(Path.Combine(folderPath, "Cache"));
        Directory.CreateDirectory(Path.Combine(folderPath, "Build"));
        
        // Create dummy subfolders and assets to populate the explorer tree
        var texDir = Path.Combine(assetDir, "Textures");
        var modelDir = Path.Combine(assetDir, "Models");
        var shaderDir = Path.Combine(assetDir, "Shaders");
        Directory.CreateDirectory(texDir);
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(shaderDir);

        File.WriteAllBytes(Path.Combine(texDir, "skybox_hdr.png"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(modelDir, "player_run.fbx"), new byte[2048]);
        File.WriteAllBytes(Path.Combine(shaderDir, "lit_opaque.hlsl"), new byte[512]);

        var projectFilePath = Path.Combine(folderPath, "project.json");
        File.WriteAllText(projectFilePath, JsonSerializer.Serialize(project, _jsonOptions));
        
        CurrentProject = project;
        OnProjectLoaded?.Invoke();
    }
    
    public void OpenProject(string folderPath)
    {
        var projectFilePath = Path.Combine(folderPath, "project.json");
        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException($"project.json not found in {folderPath}");
            
        var json = File.ReadAllText(projectFilePath);
        var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);
        if (project == null) throw new InvalidOperationException("Failed to deserialize project.");
        
        project.RootPath = folderPath;
        CurrentProject = project;
        
        Directory.CreateDirectory(Path.Combine(folderPath, "Asset"));
        Directory.CreateDirectory(Path.Combine(folderPath, "Cache"));
        Directory.CreateDirectory(Path.Combine(folderPath, "Build"));
        
        OnProjectLoaded?.Invoke();
    }
    
    public void SaveProject()
    {
        if (CurrentProject == null) return;
        
        var projectFilePath = Path.Combine(CurrentProject.RootPath, "project.json");
        File.WriteAllText(projectFilePath, JsonSerializer.Serialize(CurrentProject, _jsonOptions));
    }
    
    public void ImportAsset(string sourceFilePath, string targetVirtualPath)
    {
        if (CurrentProject == null) return;
        
        var assetDir = Path.Combine(CurrentProject.RootPath, "Asset", targetVirtualPath);
        Directory.CreateDirectory(assetDir);
        
        var fileName = Path.GetFileName(sourceFilePath);
        var destFilePath = Path.Combine(assetDir, fileName);
        
        File.Copy(sourceFilePath, destFilePath, overwrite: true);
        
        var metaFilePath = destFilePath + ".meta";
        if (!File.Exists(metaFilePath))
        {
            var extension = Path.GetExtension(fileName);
            var type = BakerRegistry.Instance.DetectAssetType(extension);
            
            var meta = new AssetMetadata
            {
                Id = Guid.NewGuid(),
                Type = type,
                Settings = BakerRegistry.Instance.CreateDefaultSettings(type)
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
