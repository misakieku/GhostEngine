using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Ghost.AssetForge.ViewModels;

public class FolderItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public ObservableCollection<FolderItem> Children { get; } = new();

    public override string ToString() => Name;
}

public partial class AssetItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string MetaPath => FullPath + ".meta";
    public string Format => Path.GetExtension(FullPath).TrimStart('.').ToUpper();
    public string FileSizeFormatted
    {
        get
        {
            try
            {
                var info = new FileInfo(FullPath);
                double bytes = info.Length;
                if (bytes >= 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
                if (bytes >= 1024) return $"{bytes / 1024:F0} KB";
                return $"{bytes} Bytes";
            }
            catch
            {
                return "Unknown Size";
            }
        }
    }

    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }

    public Visibility ThumbnailVisibility => Thumbnail != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FallbackVisibility => Thumbnail == null ? Visibility.Visible : Visibility.Collapsed;

    partial void OnThumbnailChanged(BitmapImage? value)
    {
        OnPropertyChanged(nameof(ThumbnailVisibility));
        OnPropertyChanged(nameof(FallbackVisibility));
    }
}

public partial class ProjectExplorerViewModel : ObservableObject
{
    private readonly ProjectService _projectService;
    private readonly BakerRegistry _bakerRegistry;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    public partial FolderItem? SelectedFolderNode { get; set; }

    [ObservableProperty]
    public partial AssetItem? SelectedAsset { get; set; }

    [ObservableProperty]
    public partial IBakeSettings? SelectedSettings { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GridItemSize { get; set; } = 150;

    public TextureBakeSettings? SelectedTextureSettings => SelectedSettings as TextureBakeSettings;

    partial void OnSelectedSettingsChanged(IBakeSettings? value)
    {
        OnPropertyChanged(nameof(IsTextureSettingsVisible));
        OnPropertyChanged(nameof(SelectedTextureSettings));
    }

    public ObservableCollection<FolderItem> Folders { get; } = new();
    public ObservableCollection<AssetItem> Assets { get; } = new();
    public ObservableCollection<string> Breadcrumbs { get; } = new();

    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private string _currentFolderPath = string.Empty;

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;

    public bool IsTextureSettingsVisible => SelectedSettings is TextureBakeSettings;

    public ProjectExplorerViewModel(ProjectService projectService, BakerRegistry bakerRegistry, MainViewModel mainViewModel)
    {
        _projectService = projectService;
        _bakerRegistry = bakerRegistry;
        _mainViewModel = mainViewModel;

        _mainViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentProject))
            {
                OnProjectChanged();
            }
        };

        OnProjectChanged();
    }

    private void OnProjectChanged()
    {
        Folders.Clear();
        Assets.Clear();
        Breadcrumbs.Clear();
        _backStack.Clear();
        _forwardStack.Clear();
        SelectedAsset = null;
        SelectedSettings = null;
        _currentFolderPath = string.Empty;

        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));

        if (_mainViewModel.CurrentProject != null)
        {
            LoadDirectoryTree();
            if (Folders.Count > 0)
            {
                SelectedFolderNode = Folders[0];
            }
        }
    }

    private void LoadDirectoryTree()
    {
        Folders.Clear();
        if (_mainViewModel.CurrentProject == null) return;

        var assetDir = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Asset");
        if (!Directory.Exists(assetDir)) Directory.CreateDirectory(assetDir);

        var rootNode = new FolderItem
        {
            Name = "Assets",
            FullPath = assetDir
        };
        BuildTree(assetDir, rootNode);
        Folders.Add(rootNode);
    }

    private void BuildTree(string dirPath, FolderItem parentNode)
    {
        try
        {
            var dirs = Directory.GetDirectories(dirPath);
            foreach (var dir in dirs)
            {
                var folderName = Path.GetFileName(dir);
                var childNode = new FolderItem
                {
                    Name = folderName,
                    FullPath = dir
                };
                BuildTree(dir, childNode);
                parentNode.Children.Add(childNode);
            }
        }
        catch { }
    }

    partial void OnSelectedFolderNodeChanged(FolderItem? value)
    {
        if (value != null)
        {
            NavigateTo(value.FullPath);
        }
    }

    private void NavigateTo(string folderPath, bool recordHistory = true)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        if (recordHistory && !string.IsNullOrEmpty(_currentFolderPath))
        {
            _backStack.Push(_currentFolderPath);
            _forwardStack.Clear();
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        _currentFolderPath = folderPath;
        UpdateBreadcrumbs(folderPath);
        LoadAssets(folderPath);
    }

    [RelayCommand]
    public void GoBack()
    {
        if (_backStack.Count > 0)
        {
            var prev = _backStack.Pop();
            _forwardStack.Push(_currentFolderPath);
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            NavigateTo(prev, recordHistory: false);
        }
    }

    [RelayCommand]
    public void GoForward()
    {
        if (_forwardStack.Count > 0)
        {
            var next = _forwardStack.Pop();
            _backStack.Push(_currentFolderPath);
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            NavigateTo(next, recordHistory: false);
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        if (!string.IsNullOrEmpty(_currentFolderPath))
        {
            LoadDirectoryTree();
            LoadAssets(_currentFolderPath);
        }
    }

    [RelayCommand]
    public void ImportFile(string sourceFilePath)
    {
        if (_mainViewModel.CurrentProject == null || string.IsNullOrEmpty(_currentFolderPath)) return;

        try
        {
            var assetRoot = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Asset");
            var relativeFolder = Path.GetRelativePath(assetRoot, _currentFolderPath);
            if (relativeFolder == "." || relativeFolder == "Asset") relativeFolder = string.Empty;

            _projectService.ImportAsset(sourceFilePath, relativeFolder);

            LoadDirectoryTree();
            LoadAssets(_currentFolderPath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import file: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ImportFiles(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        if (_mainViewModel.CurrentProject == null || string.IsNullOrEmpty(_currentFolderPath)) return;

        try
        {
            var assetRoot = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Asset");
            var relativeFolder = Path.GetRelativePath(assetRoot, _currentFolderPath);
            if (relativeFolder == "." || relativeFolder == "Asset") relativeFolder = string.Empty;

            foreach (var file in filePaths)
            {
                _projectService.ImportAsset(file, relativeFolder);
            }

            LoadDirectoryTree();
            LoadAssets(_currentFolderPath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import files: {ex.Message}");
        }
    }

    private void UpdateBreadcrumbs(string folderPath)
    {
        Breadcrumbs.Clear();
        if (_mainViewModel.CurrentProject == null) return;

        var assetRoot = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Asset");
        var relative = Path.GetRelativePath(assetRoot, folderPath);

        Breadcrumbs.Add("Assets");
        if (relative == "." || string.IsNullOrEmpty(relative)) return;

        var parts = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            Breadcrumbs.Add(part);
        }
    }

    private void LoadAssets(string folderPath)
    {
        Assets.Clear();
        SelectedAsset = null;
        SelectedSettings = null;

        try
        {
            var files = Directory.GetFiles(folderPath, "*.*")
                .Where(f => !f.EndsWith(".meta"))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var item = new AssetItem
                {
                    Name = Path.GetFileName(file),
                    FullPath = file
                };

                LoadThumbnailAsync(item);

                Assets.Add(item);
            }
        }
        catch { }
    }

    private async void LoadThumbnailAsync(AssetItem item)
    {
        try
        {
            var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(item.FullPath);
            using var thumbnail = await storageFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 96);
            if (thumbnail != null)
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(thumbnail);
                item.Thumbnail = bitmap;
            }
        }
        catch
        {
            // Keep default fallback icon visible
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        LoadAssets(_currentFolderPath);

        if (!string.IsNullOrEmpty(value))
        {
            var filtered = Assets.Where(a => a.Name.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
            Assets.Clear();
            foreach (var item in filtered)
            {
                Assets.Add(item);
            }
        }
    }

    partial void OnSelectedAssetChanged(AssetItem? value)
    {
        if (value == null)
        {
            SelectedSettings = null;
            return;
        }

        var metaFile = value.MetaPath;
        var metadata = _projectService.LoadMetadata(metaFile);

        if (metadata == null)
        {
            var ext = Path.GetExtension(value.FullPath);
            var type = _bakerRegistry.DetectAssetType(ext);
            var defaultSettings = _bakerRegistry.CreateDefaultSettings(ext);

            metadata = new AssetMetadata
            {
                Id = Guid.NewGuid(),
                Type = type,
                Settings = defaultSettings
            };

            _projectService.SaveMetadata(metaFile, metadata);
        }

        SelectedSettings = CloneSettings(metadata.Settings);
    }

    [RelayCommand]
    public void ApplySettings()
    {
        if (SelectedAsset == null || SelectedSettings == null) return;

        var metadata = _projectService.LoadMetadata(SelectedAsset.MetaPath);
        if (metadata != null)
        {
            var updatedMetadata = metadata with { Settings = SelectedSettings };
            _projectService.SaveMetadata(SelectedAsset.MetaPath, updatedMetadata);
        }
    }

    public Array TextureTypes => Enum.GetValues(typeof(TextureType));
    public Array TextureShapes => Enum.GetValues(typeof(TextureShape));
    public Array TextureSizes => Enum.GetValues(typeof(TextureSize));
    public Array MipmapFilters => Enum.GetValues(typeof(MipmapFilter));
    public Array CompressionLevels => Enum.GetValues(typeof(TextureCompressionLevel));

    private IBakeSettings? CloneSettings(IBakeSettings? source)
    {
        if (source == null) return null;
        try
        {
            var json = JsonSerializer.Serialize(source, source.GetType());
            return (IBakeSettings?)JsonSerializer.Deserialize(json, source.GetType());
        }
        catch
        {
            return null;
        }
    }
}
