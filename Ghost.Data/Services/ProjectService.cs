using Ghost.Core;
using Ghost.Data.Models;
using Ghost.Data.Repository;
using Ghost.Data.Resources;
using System.IO.Compression;
using System.Text.Json;

namespace Ghost.Data.Services;

internal partial class ProjectService
{
    private const string _TEMPLATE_CONTENT_FILE = "content.zip";

    public const string ASSETS_FOLDER = "Assets";
    public const string CONFIG_FOLDER = "ProjectConfig";

    public static ProjectMetadataInfo CurrentProject
    {
        get;
        set;
    }

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
            return Result<ProjectMetadataInfo>.Failure("Project directory is invalid or does not exist.");
        }

        var projectAssetsPath = Path.Combine(projectDirectory, ASSETS_FOLDER);
        var projectConfigPath = Path.Combine(projectDirectory, CONFIG_FOLDER);
        if (!Directory.Exists(projectAssetsPath) || !Directory.Exists(projectConfigPath))
        {
            return Result<ProjectMetadataInfo>.Failure("Project folder structure is invalid.");
        }

        var metadataPath = Directory.GetFiles(projectDirectory, $"*.{ProjectMetadata.PROJECT_EXTENSION}", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath))
        {
            return Result<ProjectMetadataInfo>.Failure("Project metadata file not found.");
        }

        var metadata = await LoadMetadataAsync(metadataPath);
        if (metadata == null)
        {
            return Result<ProjectMetadataInfo>.Failure("Project metadata file is corrupted or invalid.");
        }

        return Result<ProjectMetadataInfo>.Success(new(metadataPath, metadata));
    }

    private static async ValueTask SetupRequestFolderAsync(string projectDirectory, string templateDirectory)
    {
        var projectAssetsPath = Path.Combine(projectDirectory, ASSETS_FOLDER);
        var projectConfigPath = Path.Combine(projectDirectory, CONFIG_FOLDER);
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

internal partial class ProjectService
{
    public Task AddProjectAsync(ProjectInfo project)
    {
        return ProjectRepository.AddProjectAsync(project);
    }

    public async Task<ProjectInfo> AddProjectAsync(string name, string path)
    {
        var project = new ProjectInfo
        {
            Name = name,
            MetadataPath = path,
        };
        await ProjectRepository.AddProjectAsync(project);

        return project;
    }

    public Task RemoveProjectAsync(ProjectInfo project)
    {
        return ProjectRepository.RemoveProjectAsync(project);
    }

    public Task UpdateProjectAsync(ProjectInfo project)
    {
        return ProjectRepository.UpdateProjectAsync(project);
    }

    public async Task<bool> HasProjectAsync(string path)
    {
        return await ProjectRepository.GetProjectByMetadataPathAsync(path) != null;
    }

    public async IAsyncEnumerable<ProjectInfo> GetAllProjectAsync()
    {
        var badProjectList = new List<ProjectInfo>();
        await foreach (var project in ProjectRepository.GetAllProjectsAsync())
        {
            if (string.IsNullOrWhiteSpace(project.MetadataPath) || !File.Exists(project.MetadataPath))
            {
                badProjectList.Add(project);
                continue;
            }

            yield return project;
        }

        foreach (var badProject in badProjectList)
        {
            await ProjectRepository.RemoveProjectAsync(badProject);
        }
    }

    public async Task<Result<ProjectMetadataInfo>> CreateProjectAsync(string projectName, string projectDirectory, Version engineVersion, string templatePath)
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
                    return new(false, default, "Directory is not empty");
                }
            }

            var metadata = new ProjectMetadata(projectName, engineVersion);
            var metadataPath = Path.Combine(projectPath, $"{projectName}.{ProjectMetadata.PROJECT_EXTENSION}");
            await CreateMetadataFileAsync(metadataPath, metadata);
            await SetupRequestFolderAsync(projectPath, templatePath);

            var info = await AddProjectAsync(projectName, metadataPath);
            return new(true, new(metadataPath, metadata));
        }
        catch (Exception e)
        {
            return Result<ProjectMetadataInfo>.Failure($"Failed to create project: {e.Message}");
        }
    }

    public async Task<Result<ProjectMetadataInfo>> AddProjectFromDirectoryAsync(string projectDirectory)
    {
        var result = await ValidateProjectDirectoryAsync(projectDirectory);
        if (!result.success)
        {
            return result;
        }

        if (await HasProjectAsync(result.value.Path))
        {
            return Result<ProjectMetadataInfo>.Failure("Project already exists.");
        }

        await AddProjectAsync(result.value.Metadata.Name, result.value.Path);

        return result;
    }
}