using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ghost.AssetBaker.Models;

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

        var type = DetectAssetType(fileInfo.Extension);
        var asset = new QueuedAsset
        {
            FilePath = fileInfo.FullName,
            Name = fileInfo.Name,
            SizeInBytes = fileInfo.Length,
            Type = type,
            Status = AssetState.Pending,
            Settings = new BakeSettings
            {
                OutputPath = Path.Combine(fileInfo.DirectoryName ?? string.Empty, "Baked")
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
        if (_isBaking) return;
        _queue.Clear();
        Log("Cleared all items from the queue.");
        NotifyStateChanged();
    }

    public void ClearCompleted()
    {
        if (_isBaking) return;
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

    public async Task BakeQueueAsync(BakeSettings globalSettings)
    {
        if (_isBaking || !_queue.Any(a => a.Status == AssetState.Pending)) return;

        _isBaking = true;
        Log("=== Start Baking Process ===");
        NotifyStateChanged();

        try
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                var asset = _queue[i];
                if (asset.Status != AssetState.Pending) continue;

                // Update status to Baking
                UpdateAssetStatus(asset.Id, AssetState.Baking, 0.0);
                Log($"Baking asset [{i + 1}/{_queue.Count}]: {asset.Name}...");

                // Simulate processing
                bool success = await SimulateBakeAsync(asset, globalSettings);

                if (success)
                {
                    UpdateAssetStatus(asset.Id, AssetState.Success, 100.0);
                    Log($"Success: Baked {asset.Name} to output folder.", isSuccess: true);
                }
                else
                {
                    UpdateAssetStatus(asset.Id, AssetState.Failed, 100.0, "Bake error simulated.");
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
        if (index != -1 && index >= 0)
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

    private async Task<bool> SimulateBakeAsync(QueuedAsset asset, BakeSettings globalSettings)
    {
        int steps = 10;
        var compression = asset.Settings.Compression;
        var bundle = globalSettings.BundleOutput;

        for (int step = 1; step <= steps; step++)
        {
            await Task.Delay(200); // 2 seconds total simulation time per asset
            double progress = (step / (double)steps) * 100.0;
            
            UpdateAssetStatus(asset.Id, AssetState.Baking, progress);

            // Log details based on asset type
            if (step == 3)
            {
                switch (asset.Type)
                {
                    case AssetType.Mesh:
                        Log($"  [Mesh] Parsing vertex data... found {new Random().Next(5000, 100000)} vertices.");
                        break;
                    case AssetType.Texture:
                        Log($"  [Texture] Analyzing dimensions... format identified as RGB.");
                        break;
                    case AssetType.Shader:
                        Log("  [Shader] Preprocessing preprocessor directives...");
                        break;
                    case AssetType.Audio:
                        Log("  [Audio] Decoding audio sample rate...");
                        break;
                }
            }
            else if (step == 6)
            {
                switch (asset.Type)
                {
                    case AssetType.Mesh:
                        if (asset.Settings.OptimizeMesh)
                            Log("  [Mesh] Optimizing vertex cache & index buffers...");
                        if (asset.Settings.GenerateLods)
                            Log("  [Mesh] Generating LOD levels...");
                        break;
                    case AssetType.Texture:
                        Log($"  [Texture] Compressing with mode: {compression}");
                        if (asset.Settings.GenerateMipmaps)
                            Log("  [Texture] Generating mipmap chain...");
                        break;
                    case AssetType.Shader:
                        Log("  [Shader] Compiling DXIL / SPIR-V bytecode...");
                        break;
                    case AssetType.Audio:
                        Log("  [Audio] Encoding to engine-native PCM stream...");
                        break;
                }
            }
            else if (step == 9)
            {
                if (bundle)
                    Log("  [Packer] Queueing asset for bundle packing...");
                else
                    Log($"  [Writer] Writing baked asset output file: {asset.Name.Substring(0, asset.Name.LastIndexOf('.'))}.g{asset.Type.ToString().ToLower()}");
            }
        }

        // Simulate rare failures for Shader/Other
        if (asset.Type == AssetType.Shader && new Random().Next(0, 10) == 0)
        {
            Log("  [Shader] Shader compilation error: syntax error in main entry point.", isError: true);
            return false;
        }

        return true;
    }

    private void Log(string message, bool isError = false, bool isSuccess = false)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        string prefix = isError ? "[ERROR] " : isSuccess ? "[SUCCESS] " : "";
        string formatted = $"[{timestamp}] {prefix}{message}";
        
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

    private AssetType DetectAssetType(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return AssetType.Other;
        extension = extension.ToLowerInvariant();

        string[] meshes = { ".fbx", ".obj", ".gltf", ".glb", ".dae", ".3ds" };
        string[] textures = { ".png", ".jpg", ".jpeg", ".tga", ".dds", ".hdr", ".bmp", ".tif", ".tiff" };
        string[] shaders = { ".hlsl", ".glsl", ".shader", ".ghsl", ".frag", ".vert" };
        string[] audios = { ".wav", ".mp3", ".ogg", ".flac", ".m4a" };

        if (meshes.Contains(extension)) return AssetType.Mesh;
        if (textures.Contains(extension)) return AssetType.Texture;
        if (shaders.Contains(extension)) return AssetType.Shader;
        if (audios.Contains(extension)) return AssetType.Audio;

        return AssetType.Other;
    }
}
