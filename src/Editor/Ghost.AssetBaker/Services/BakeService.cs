using Ghost.AssetBaker.Models;
using Ghost.Core;
using System.Runtime.InteropServices;

namespace Ghost.AssetBaker.Services;

public class BakeService
{
    private static readonly Lazy<BakeService> s_instance = new(() => new BakeService());
    public static BakeService Instance => s_instance.Value;

    private readonly List<QueuedAsset> _queue = new();
    private readonly List<string> _logs = new();
    private bool _isBaking;

    public IReadOnlyList<QueuedAsset> Queue => _queue;
    public IReadOnlyList<string> Logs => _logs;
    public bool IsBaking => _isBaking;

    public event Action? OnStateChanged;

    private BakeService()
    {
        Log("Ghost.AssetBaker initialized.");
        Log("Ready to accept asset inputs.");
    }

    public void AddFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Log($"Error: File not found at '{filePath}'", isError: true);
            return;
        }

        var fileInfo = new FileInfo(filePath);
        if (_queue.Any(a => a.FilePath.Equals(fileInfo.FullName, StringComparison.OrdinalIgnoreCase)))
        {
            Log($"Warning: File '{fileInfo.Name}' is already in the queue.");
            return;
        }

        var type = BakerRegistry.Instance.DetectAssetType(fileInfo.Extension);
        var asset = new QueuedAsset
        {
            FilePath = fileInfo.FullName,
            Name = fileInfo.Name,
            SizeInBytes = fileInfo.Length,
            Type = type,
            Status = AssetState.Pending,
            Settings = new BakeSettings
            {
                OutputPath = Path.Combine(fileInfo.DirectoryName ?? string.Empty, "Baked"),
                AssetSettings = BakerRegistry.Instance.CreateDefaultSettings(type)
            }
        };

        _queue.Add(asset);
        Log($"Added asset: {asset.Name} ({asset.SizeFormatted}) - Type: {asset.Type}");
        NotifyStateChanged();
    }

    public void RemoveFile(Guid id)
    {
        var asset = _queue.FirstOrDefault(a => a.Id == id);
        if (asset != null)
        {
            _queue.Remove(asset);
            Log($"Removed asset: {asset.Name}");
            NotifyStateChanged();
        }
    }

    public void ClearAll()
    {
        if (_isBaking)
        {
            return;
        }

        _queue.Clear();
        Log("Cleared all items from the queue.");
        NotifyStateChanged();
    }

    public void ClearCompleted()
    {
        if (_isBaking)
        {
            return;
        }

        var completedCount = _queue.RemoveAll(a => a.Status == AssetState.Success || a.Status == AssetState.Failed);
        Log($"Cleared {completedCount} completed/failed items from the queue.");
        NotifyStateChanged();
    }

    public void UpdateAssetSettings(Guid id, BakeSettings settings)
    {
        var index = _queue.FindIndex(a => a.Id == id);
        if (index != -1 && index >= 0)
        {
            _queue[index] = _queue[index] with { Settings = settings };
            NotifyStateChanged();
        }
    }

    public async Task BakeQueueAsync(BakeSettings settings, CancellationToken cancellationToken)
    {
        if (_isBaking || !_queue.Any(a => a.Status == AssetState.Pending))
        {
            return;
        }

        _isBaking = true;
        Log("=== Start Baking Process ===");
        NotifyStateChanged();

        try
        {
            for (var i = 0; i < _queue.Count; i++)
            {
                var asset = _queue[i];
                if (asset.Status != AssetState.Pending)
                {
                    continue;
                }

                // Update status to Baking
                UpdateAssetStatus(asset, AssetState.Baking, 0.0);
                Log($"Baking asset [{i + 1}/{_queue.Count}]: {asset.Name}...");

                // Process asset
                var success = false;
                var baker = BakerRegistry.Instance.GetBaker(asset.Type);
                if (baker != null && asset.Settings.AssetSettings != null)
                {
                    try
                    {
                        using var dst = new MemoryStream(); // Or file stream?
                        await baker.BakeAssetAsync(asset.FilePath, dst, asset.Settings.AssetSettings, cancellationToken);

                        var header = new AssetHeader
                        {
                            assetType = asset.Type,
                            compressionMethod = settings.Compression,
                        };

                        var outDir = asset.Settings.OutputPath;
                        if (!Directory.Exists(outDir))
                        {
                            Directory.CreateDirectory(outDir);
                        }

                        var outFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(asset.Name) + ".g" + asset.Type.ToString().ToLower());

                        // TODO Compress dst based on settings.Compression

                        using var fs = new FileStream(outFile, FileMode.Create, FileAccess.Write);
                        fs.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
                        await dst.CopyToAsync(fs, cancellationToken);

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Log($"Exception: {ex.Message}", isError: true);
                    }
                }
                else
                {
                    Log($"Error: No baker found for asset type {asset.Type} or missing settings.", isError: true);
                }

                if (success)
                {
                    UpdateAssetStatus(asset, AssetState.Success, 100.0);
                    Log($"Success: Baked {asset.Name} to output folder.", isSuccess: true);
                }
                else
                {
                    UpdateAssetStatus(asset, AssetState.Failed, 100.0, "Bake error.");
                    Log($"Failed: Failed to bake {asset.Name}.", isError: true);
                }
            }
        }
        finally
        {
            _isBaking = false;
            Log("=== Baking Process Finished ===");
            NotifyStateChanged();
        }
    }

    private void UpdateAssetStatus(Guid id, AssetState status, double progress, string errorMsg = "")
    {
        var index = _queue.FindIndex(a => a.Id == id);
        if (index >= 0)
        {
            _queue[index] = _queue[index] with
            {
                Status = status,
                Progress = progress,
                ErrorMessage = errorMsg
            };

            NotifyStateChanged();
        }
    }

    private void UpdateAssetStatus(QueuedAsset asset, AssetState status, double progress, string errorMsg = "")
    {
        asset.Status = status;
        asset.Progress = progress;
        asset.ErrorMessage = errorMsg;

        NotifyStateChanged();
    }

    private void Log(string message, bool isError = false, bool isSuccess = false)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = isError ? "[ERROR] " : isSuccess ? "[SUCCESS] " : "";
        var formatted = $"[{timestamp}] {prefix}{message}";

        lock (_logs)
        {
            _logs.Add(formatted);
            if (_logs.Count > 200) // cap size
            {
                _logs.RemoveAt(0);
            }
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
