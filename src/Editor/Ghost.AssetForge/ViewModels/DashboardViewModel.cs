using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.AssetForge.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Ghost.AssetForge.ViewModels;

public class ProjectItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string RelativeTime
    {
        get
        {
            var span = DateTime.Now - LastModified;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"Updated {(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"Updated {(int)span.TotalHours}h ago";
            return $"Updated {(int)span.TotalDays}d ago";
        }
    }
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly ProjectService _projectService;
    private readonly MainViewModel _mainViewModel;
    private static readonly string RECENT_PROJECTS_FILE =
        Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "recent_projects.json");

    public ObservableCollection<ProjectItem> RecentProjects { get; } = new();

    public DashboardViewModel(ProjectService projectService, MainViewModel mainViewModel)
    {
        _projectService = projectService;
        _mainViewModel = mainViewModel;
        LoadRecentProjects();
    }

    [RelayCommand]
    public void OpenProject(string folderPath)
    {
        try
        {
            _projectService.OpenProject(folderPath);
            if (_projectService.CurrentProject != null)
            {
                _mainViewModel.CurrentProject = _projectService.CurrentProject;
                SaveToRecent(_projectService.CurrentProject.Name, folderPath);
                _mainViewModel.CurrentPageName = "ProjectExplorer";
            }
        }
        catch
        {
            throw;
        }
    }

    [RelayCommand]
    public void CreateProject(KeyValuePair<string, string> args)
    {
        // args.Key = folderPath, args.Value = projectName
        try
        {
            var folderPath = args.Key;
            var projectName = args.Value;
            var fullPath = Path.Combine(folderPath, projectName);
            _projectService.CreateProject(fullPath, projectName);
            if (_projectService.CurrentProject != null)
            {
                _mainViewModel.CurrentProject = _projectService.CurrentProject;
                SaveToRecent(projectName, fullPath);
                _mainViewModel.CurrentPageName = "ProjectExplorer";
            }
        }
        catch
        {
            throw;
        }
    }

    private void LoadRecentProjects()
    {
        RecentProjects.Clear();
        if (!File.Exists(RECENT_PROJECTS_FILE)) return;

        try
        {
            var json = File.ReadAllText(RECENT_PROJECTS_FILE);
            var list = JsonSerializer.Deserialize<List<ProjectItem>>(json);
            if (list != null)
            {
                list.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
                foreach (var item in list)
                {
                    RecentProjects.Add(item);
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private void SaveToRecent(string name, string path)
    {
        var list = new List<ProjectItem>();
        if (File.Exists(RECENT_PROJECTS_FILE))
        {
            try
            {
                var json = File.ReadAllText(RECENT_PROJECTS_FILE);
                var existing = JsonSerializer.Deserialize<List<ProjectItem>>(json);
                if (existing != null) list = existing;
            }
            catch { }
        }

        list.RemoveAll(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        list.Add(new ProjectItem
        {
            Name = name,
            Path = path,
            LastModified = DateTime.Now
        });

        if (list.Count > 10)
        {
            list.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
            list = list.GetRange(0, 10);
        }

        try
        {
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RECENT_PROJECTS_FILE, json);
            LoadRecentProjects();
        }
        catch { }
    }
}
