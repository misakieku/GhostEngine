using Ghost.Data.DataContext;
using Ghost.Data.Models;
using Ghost.Data.Resources;
using System.IO.Compression;
using System.Text.Json;

namespace Ghost.Data.Services;

public class ProjectService
{
    private const string _ASSETS_FOLDER = "Assets";
    private const string _TEMPLATE_CONTENT_FILE = "content.zip";

    public async IAsyncEnumerable<(string path, TemplateInfo info)> GetProjectTemplatesAsync()
    {
        var templatesFolder = DataPath.PROJECT_TEMPLATES_FOLDER;
        if (!Directory.Exists(templatesFolder))
        {
            yield break;
        }

        var templates = Directory.GetFiles(DataPath.PROJECT_TEMPLATES_FOLDER, "template.json", SearchOption.AllDirectories);
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

    private Task SetupAssetsFolder(string projectPath, string templatePath)
    {
        return Task.Run(() =>
        {
            var templateContentPath = Path.Combine(templatePath, _TEMPLATE_CONTENT_FILE);
            var projectAssetsPath = Path.Combine(projectPath, _ASSETS_FOLDER);

            Directory.CreateDirectory(projectAssetsPath);

            if (!File.Exists(templateContentPath))
            {
                return;
            }

            ZipFile.ExtractToDirectory(templateContentPath, projectAssetsPath);
        });
    }

    public IAsyncEnumerable<ProjectInfo> LoadAllProjectAsync()
    {
        return ProjectRepository.LoadProjectsAsync();
    }

    public async Task<string> CreateProjectAsync(string projectName, string projectDirectory, string templatePath)
    {
        var projectPath = Path.Combine(projectDirectory, projectName);
        if (!Directory.Exists(projectPath))
        {
            Directory.CreateDirectory(projectPath);
        }

        await SetupAssetsFolder(projectPath, templatePath);

        return projectPath;
    }

    public Task AddProjectAsync(ProjectInfo project)
    {
        return ProjectRepository.AddProjectAsync(project);
    }

    public async Task<ProjectInfo> AddProjectAsync(string name, string path, Version version)
    {
        var project = new ProjectInfo
        {
            Name = name,
            Path = path,
            EngineVersion = version,
            LastOpened = DateTime.Now
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
}