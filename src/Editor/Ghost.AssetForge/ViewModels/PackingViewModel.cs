using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ghost.AssetForge.ViewModels;

public partial class PackingViewModel : ObservableObject
{
    private readonly BakeService _bakeService;
    private readonly PackService _packService;
    private readonly ProjectService _projectService;
    private readonly MainViewModel _mainViewModel;
    private readonly DispatcherQueue _dispatcher;

    private CancellationTokenSource? _cts;
    private Stopwatch? _stopwatch;
    private DispatcherQueueTimer? _timer;

    [ObservableProperty]
    public partial string OutputDirectory { get; set; } = string.Empty;

    public BakeSettings? BakeSettings => _mainViewModel.CurrentProject?.BakeSettings;

    [ObservableProperty]
    public partial bool IsBuilding { get; set; }

    public bool CanBuild => !IsBuilding;

    partial void OnIsBuildingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanBuild));
    }

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    public partial int CookedCount { get; set; }

    [ObservableProperty]
    public partial int PackedCount { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial string ElapsedTimeString { get; set; } = "00:00";

    [ObservableProperty]
    public partial string RemainingTimeString { get; set; } = "00:00";

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    public ObservableCollection<LogMessage> LogMessages { get; } = new();

    public PackingViewModel(BakeService bakeService, PackService packService, ProjectService projectService, MainViewModel mainViewModel)
    {
        _bakeService = bakeService;
        _packService = packService;
        _projectService = projectService;
        _mainViewModel = mainViewModel;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        // Subscribe to Core logger
        Logger.Impl.OnLogAdded += OnSystemLogAdded;
        Logger.Impl.OnLogsCleared += OnSystemLogsCleared;

        // Register bake & pack progress events
        _bakeService.OnProgress += OnBakeProgress;
        _packService.OnProgress += OnPackProgress;

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
        if (_mainViewModel.CurrentProject != null)
        {
            OutputDirectory = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Build");
            RefreshAssetStatistics();
        }
        else
        {
            OutputDirectory = string.Empty;
            TotalCount = 0;
        }

        OnPropertyChanged(nameof(BakeSettings));
    }

    public void RefreshAssetStatistics()
    {
        if (_mainViewModel.CurrentProject == null) return;
        var assetDir = Path.Combine(_mainViewModel.CurrentProject.RootPath, "Asset");
        if (!Directory.Exists(assetDir)) return;

        try
        {
            var files = Directory.GetFiles(assetDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta")).ToList();

            TotalCount = files.Count;
        }
        catch { }
    }

    private void OnBakeProgress(int completed, int total)
    {
        _dispatcher.TryEnqueue(() =>
        {
            CookedCount = completed;
            UpdateProgress();
        });
    }

    private void OnPackProgress(int completed, int total)
    {
        _dispatcher.TryEnqueue(() =>
        {
            PackedCount = completed;
            UpdateProgress();
        });
    }

    private void UpdateProgress()
    {
        if (TotalCount == 0) return;

        // baking is 50% of the build, packing is 50% of the build
        var bakeProgress = (double)CookedCount / TotalCount * 50;
        var packProgress = (double)PackedCount / TotalCount * 50;
        ProgressPercent = Math.Clamp(bakeProgress + packProgress, 0, 100);

        if (_stopwatch != null && ProgressPercent > 0)
        {
            double elapsedMs = _stopwatch.ElapsedMilliseconds;
            var totalEstimateMs = elapsedMs / (ProgressPercent / 100.0);
            var remainingMs = Math.Max(0, totalEstimateMs - elapsedMs);
            var remaining = TimeSpan.FromMilliseconds(remainingMs);
            RemainingTimeString = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }

    [RelayCommand]
    public async Task RunBuild()
    {
        if (_mainViewModel.CurrentProject == null || IsBuilding) return;

        IsBuilding = true;
        ProgressPercent = 0;
        CookedCount = 0;
        PackedCount = 0;
        ElapsedTimeString = "00:00";
        RemainingTimeString = "00:00";
        StatusText = "Baking assets...";

        Logger.Impl.Clear();
        Logger.Info("Starting build process...");

        // Save Project settings before build
        _projectService.SaveProject();

        _cts = new CancellationTokenSource();
        _stopwatch = Stopwatch.StartNew();

        _timer = _dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            if (_stopwatch != null)
            {
                ElapsedTimeString = $"{_stopwatch.Elapsed.Minutes:D2}:{_stopwatch.Elapsed.Seconds:D2}";
            }
        };
        _timer.Start();

        try
        {
            // 1. Bake
            await _bakeService.BakeProjectAsync(_cts.Token);

            // 2. Pack
            _dispatcher.TryEnqueue(() => StatusText = "Packing assets...");
            await _packService.PackProjectAsync(_cts.Token);

            Logger.Info("Build completed successfully!");
            StatusText = "Build Succeeded";
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Build was cancelled by the user.");
            StatusText = "Build Cancelled";
        }
        catch (Exception ex)
        {
            Logger.Error($"Build failed: {ex.Message}");
            StatusText = "Build Failed";
        }
        finally
        {
            _stopwatch.Stop();
            _timer.Stop();
            IsBuilding = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    public void StopBuild()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    public void ClearConsole()
    {
        Logger.Impl.Clear();
    }

    private void OnSystemLogAdded(LogMessage msg)
    {
        _dispatcher.TryEnqueue(() =>
        {
            LogMessages.Add(msg);
            if (LogMessages.Count > 1000) LogMessages.RemoveAt(0);
        });
    }

    private void OnSystemLogsCleared()
    {
        _dispatcher.TryEnqueue(() =>
        {
            LogMessages.Clear();
        });
    }
}
