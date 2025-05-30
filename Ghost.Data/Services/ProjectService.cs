using Ghost.Data.Models;
using Ghost.Data.Repository;
using Ghost.Data.Resources;
using System.IO.Compression;
using System.Text.Json;

namespace Ghost.Data.Services;

internal partial class ProjectService
{
    private const string _ASSETS_FOLDER = "Assets";
    private const string _CONFIG_FOLDER = "ProjectConfig";
    private const string _TEMPLATE_CONTENT_FILE = "content.zip";

    public static void EnsureDefaultTemplate()
    {
        var templates = Directory.GetFiles(DataPath.s_projectTemplateFolder, "template.json", SearchOption.AllDirectories);
        if (templates.Length > 0)
        {
            return; // Default template already exists
        }

        var defaultTemplatePath = Path.Combine(AppContext.BaseDirectory, "Assets/ProjectTemplates/Empty.zip");
        ZipFile.ExtractToDirectory(defaultTemplatePath, DataPath.s_projectTemplateFolder, true);
    }

    public static async IAsyncEnumerable<(string path, TemplateInfo info)> GetProjectTemplatesAsync()
    {
        var templatesFolder = DataPath.s_projectTemplateFolder;
        if (!Directory.Exists(templatesFolder))
        {
            yield break;
        }

        var templates = Directory.GetFiles(DataPath.s_projectTemplateFolder, "template.json", SearchOption.AllDirectories);
        foreach (var templatePath in templates)
        {
            var fileStream = File.OpenRead(templatePath);
            var templateInfo = await JsonSerializer.DeserializeAsync<TemplateInfo>(fileStream, JsonContext.Default.TemplateInfo);
            if (templateInfo == null)
            {
                continue;
            }

            yield return (templatePath, templateInfo);
        }
    }

    public static async Task CreateMetadataFileAsync(string path, ProjectMetadata metadata)
    {
        await using var fileStream = File.Create(path);
        await JsonSerializer.SerializeAsync(fileStream, metadata, JsonContext.Default.ProjectMetadata);
    }

    public static async Task<ProjectMetadata?> LoadMetadataAsync(string ghostprojPath)
    {
        if (!File.Exists(ghostprojPath))
        {
            throw new FileNotFoundException("Project metadata file not found.", ghostprojPath);
        }

        await using var fileStream = File.OpenRead(ghostprojPath);
        return await JsonSerializer.DeserializeAsync<ProjectMetadata>(fileStream, JsonContext.Default.ProjectMetadata);
    }

    public static async Task<Result<ProjectMetadataInfo>> ValidateProjectDirectoryAsync(string? projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return Result<ProjectMetadataInfo>.Error("Project directory is invalid or does not exist.");
        }

        var projectAssetsPath = Path.Combine(projectDirectory, _ASSETS_FOLDER);
        var projectConfigPath = Path.Combine(projectDirectory, _CONFIG_FOLDER);
        if (!Directory.Exists(projectAssetsPath) || !Directory.Exists(projectConfigPath))
        {
            return Result<ProjectMetadataInfo>.Error("Project folder structure is invalid.");
        }

        var metadataPath = Directory.GetFiles(projectDirectory, $"*.{ProjectMetadata.PROJECT_EXTENSION}", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath))
        {
            return Result<ProjectMetadataInfo>.Error("Project metadata file not found.");
        }

        var metadata = await LoadMetadataAsync(metadataPath);
        if (metadata == null)
        {
            return Result<ProjectMetadataInfo>.Error("Project metadata file is corrupted or invalid.");
        }

        return Result<ProjectMetadataInfo>.OK(new(metadataPath, metadata));
    }

    private static async ValueTask SetupRequestFolderAsync(string projectDirectory, string templateDirectory)
    {
        var projectAssetsPath = Path.Combine(projectDirectory, _ASSETS_FOLDER);
        var projectConfigPath = Path.Combine(projectDirectory, _CONFIG_FOLDER);
        var templateContentPath = Path.Combine(templateDirectory, _TEMPLATE_CONTENT_FILE);

        Directory.CreateDirectory(projectAssetsPath);
        if (File.Exists(templateContentPath))
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(templateContentPath, projectAssetsPath);
            });
        }

        Directory.CreateDirectory(projectConfigPath);
    }
}

internal partial class ProjectService : IDisposable
{
    private readonly ProjectRepository _repository = new(DataPath.s_applicationDataFolder);

    public Task AddProjectAsync(ProjectInfo project)
    {
        return _repository.AddProjectAsync(project);
    }

    public async Task<ProjectInfo> AddProjectAsync(string name, string path)
    {
        var project = new ProjectInfo
        {
            Name = name,
            MetadataPath = path,
        };
        await _repository.AddProjectAsync(project);

        return project;
    }

    public Task RemoveProjectAsync(ProjectInfo project)
    {
        return _repository.RemoveProjectAsync(project);
    }

    public Task UpdateProjectAsync(ProjectInfo project)
    {
        return _repository.UpdateProjectAsync(project);
    }

    public IAsyncEnumerable<ProjectInfo> LoadAllProjectAsync()
    {
        return _repository.LoadProjectsAsync();
    }

    public async Task<Result<ProjectInfo>> CreateProjectAsync(string projectName, string projectDirectory, Version engineVersion, string templatePath)
    {
        try
        {
            var projectPath = Path.Combine(projectDirectory, projectName);
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }
            else
            {
                // Check if folder is empty
                if (Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories).Any())
                {
                    return new(false, null, "Directory is not empty");
                }
            }

            var metadata = new ProjectMetadata(projectName, engineVersion);
            var metadataPath = Path.Combine(projectPath, $"{projectName}.{ProjectMetadata.PROJECT_EXTENSION}");
            await CreateMetadataFileAsync(metadataPath, metadata);
            await SetupRequestFolderAsync(projectPath, templatePath);

            var info = await AddProjectAsync(projectName, metadataPath);
            return new(true, info);
        }
        catch (Exception e)
        {
            return Result<ProjectInfo>.Error($"Failed to create project: {e.Message}");
        }
    }

    public async Task<Result<ProjectMetadataInfo>> AddProjectFromDirectoryAsync(string projectDirectory)
    {
        var result = await ValidateProjectDirectoryAsync(projectDirectory);
        if (result.success)
        {
            await AddProjectAsync(result.data.Metadata.Name, result.data.Path);
        }

        return result;
    }

    public void Dispose()
    {
        _repository.Dispose();
    }
}