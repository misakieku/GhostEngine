using Ghost.Core;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Ghost.Editor.Core.AssetHandle;

/// <summary>
/// Command types for asset database operations.
/// </summary>
internal enum AssetCommandType
{
    FileCreated,
    FileModified,
    FileDeleted,
    FileRenamed,
    ManualRefresh
}

/// <summary>
/// Represents a command to process an asset operation.
/// </summary>
internal readonly record struct AssetCommand(
    AssetCommandType Type,
    string Path,
    string? OldPath = null,
    DateTime Timestamp = default
);

/// <summary>
/// Centralized asset database that manages all assets in the project.
/// Handles asset registration, lookup, importing, and dependency management.
/// Uses SQLite for persistent storage and efficient querying.
/// </summary>
public static partial class AssetService
{
    private static FileSystemWatcher? s_watcher;
    private static readonly Lock s_dbLock = new();
    private static readonly Dictionary<Guid, string> s_assetPathLookup = new();
    private static readonly Dictionary<string, Guid> s_pathAssetLookup = new();

    // In-memory dirty asset tracking (for runtime modifications only)
    // TODO: We do not handle the reimporting of dirty assets yet
    private static readonly HashSet<Guid> s_dirtyAssets = new();

    // Command buffer pattern - Channel for file system event commands
    private static Channel<AssetCommand>? s_commandChannel;
    private static Timer? s_commandProcessorTimer;
    private static readonly ConcurrentQueue<AssetCommand> s_waitingCommands = new(); // Commands waiting for manual refresh
    private static bool s_autoRefreshEnabled = true;

    // Initialization guard
    private static readonly Lock s_initializationLock = new();
    private static bool s_initialized = false;

    private static readonly TimeSpan s_debounceDelay = TimeSpan.FromMilliseconds(100);
    private static readonly ManualResetEventSlim s_resetEventSlim = new(false);

    private static readonly JsonSerializerOptions s_defaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static DirectoryInfo? AssetsDirectory
    {
        get;
        private set;
    }

    /// <summary>
    /// Initialize the asset database.
    /// Must be called after project is loaded.
    /// </summary>
    internal static async Task Initialize(CancellationToken token = default)
    {
        lock (s_initializationLock)
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
        }

        AssetsDirectory = new DirectoryInfo(Path.Combine(EditorApplication.CurrentProjectPath, EditorApplication.ASSETS_FOLDER_NAME));

        s_commandChannel = Channel.CreateUnbounded<AssetCommand>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        // Initialize command processor timer (starts disabled, triggered by events)
        s_commandProcessorTimer = new Timer(ProcessPendingCommands, null, Timeout.Infinite, Timeout.Infinite);

        await InitializeDatabaseAsync(token);
        await LoadAssetCacheFromDatabaseAsync(token);

        s_watcher = new FileSystemWatcher
        {
            Path = AssetsDirectory.FullName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        InitializeAssetHandle();
        InitializeMetaData();

        // TODO: Timestamp fake instead of full scan.
        await ValidateAndFixDatabaseAsync(token);
    }

    /// <summary>
    /// Validate the asset database and fix any inconsistencies.
    /// Checks for missing/corrupted assets and regenerates metadata as needed.
    /// </summary>
    private static async Task<Result> ValidateAndFixDatabaseAsync(CancellationToken token = default)
    {
        if (AssetsDirectory == null)
        {
            return Result.Failure("AssetsDirectory not initialized");
        }

        try
        {
            // Scan all files in assets directory
            var allFiles = Directory.EnumerateFiles(AssetsDirectory.FullName, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(Utilities.FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase));

            // Ensure all files have metadata
            foreach (var file in allFiles)
            {
                var metaPath = file + Utilities.FileExtensions.META_FILE_EXTENSION;
                if (!File.Exists(metaPath))
                {
                    await GenerateMetaFileAsync(file, token);
                }
                else
                {
                    // Validate and update database
                    var metaResult = await ReadMetaFileAsync(file, token);
                    if (metaResult.IsSuccess)
                    {
                        var fileHash = await CalculateFileHashAsync(file, token);
                        await UpsertAssetAsync(file, metaResult.Value, fileHash, null, token);
                    }
                    else
                    {
                        // Corrupted meta file - regenerate
                        await GenerateMetaFileAsync(file, token);
                    }
                }
            }

            // Remove orphaned entries from database (files that no longer exist)
            await RemoveOrphanedEntriesAsync(token);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to validate database: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh the asset database manually.
    /// Scans the project directory for changes and processes any queued file system events.
    /// </summary>
    public static async Task<Result> RefreshAsync(CancellationToken token = default)
    {
        // Flush waiting commands to channel
        while (s_waitingCommands.TryDequeue(out var cmd))
        {
            s_commandChannel?.Writer.TryWrite(cmd);
        }

        s_resetEventSlim.Reset();
        s_commandChannel?.Writer.TryWrite(new AssetCommand(AssetCommandType.ManualRefresh, string.Empty));
        s_commandProcessorTimer?.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

        await Task.Run(s_resetEventSlim.Wait, token);
        return Result.Success();
    }

    /// <summary>
    /// Mark an asset as dirty (modified in memory but not yet saved).
    /// This state is NOT persisted and will be lost on application restart.
    /// </summary>
    public static void MarkDirty(Guid assetGuid)
    {
        lock (s_dbLock)
        {
            s_dirtyAssets.Add(assetGuid);
        }
    }

    /// <summary>
    /// Check if an asset is marked as dirty.
    /// </summary>
    public static bool IsDirty(Guid assetGuid)
    {
        lock (s_dbLock)
        {
            return s_dirtyAssets.Contains(assetGuid);
        }
    }

    /// <summary>
    /// Get all dirty assets.
    /// </summary>
    public static Guid[] GetDirtyAssets()
    {
        lock (s_dbLock)
        {
            return s_dirtyAssets.ToArray();
        }
    }

    /// <summary>
    /// Clear dirty flag for an asset (typically after saving).
    /// </summary>
    public static void ClearDirty(Guid assetGuid)
    {
        lock (s_dbLock)
        {
            s_dirtyAssets.Remove(assetGuid);
        }
    }

    /// <summary>
    /// Clear all dirty flags.
    /// </summary>
    public static void ClearAllDirty()
    {
        lock (s_dbLock)
        {
            s_dirtyAssets.Clear();
        }
    }

    /// <summary>
    /// Enable or disable automatic asset database refresh.
    /// When disabled, file system events are queued and processed only when RefreshAsync() is called.
    /// </summary>
    public static void SetAutoRefresh(bool enabled)
    {
        s_autoRefreshEnabled = enabled;
    }

    internal static void FlushPendingCommands()
    {
        // Stop timer temporarily
        s_commandProcessorTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        // Give a tiny bit of time for any in-flight file watcher events to post to channel
        Thread.Sleep(50);

        // Process all commands now
        ProcessPendingCommands(null);
    }

    private static async ValueTask PostCommandAsync(AssetCommand command, CancellationToken token = default)
    {
        if (s_commandChannel == null)
        {
            return;
        }

        if (s_autoRefreshEnabled)
        {
            await s_commandChannel.Writer.WriteAsync(command, token);
            s_commandProcessorTimer?.Change(s_debounceDelay, Timeout.InfiniteTimeSpan);
        }
        else
        {
            s_waitingCommands.Enqueue(command);
        }
    }

    private static async void ProcessPendingCommands(object? state)
    {
        if (s_commandChannel == null)
        {
            return;
        }

        try
        {
            // // Collect all pending commands
            // var commands = new List<AssetCommand>();
            //
            // while (s_commandChannel.Reader.TryRead(out var cmd))
            // {
            //     commands.Add(cmd);
            // }

            // // Group commands by path (last command wins)
            // var commandsByPath = new Dictionary<string, AssetCommand>();
            // foreach (var cmd in commands)
            // {
            //     commandsByPath[cmd.Path] = cmd;
            // }

            // NOTE: We handle the temp file filtering in each command handler now
            // We should able to remove this allocation heavy code

            // Filter out temp files (files that were created then deleted)
            // lock (s_commandLock)
            // {
            //     var pathsToProcess = commandsByPath.Keys.ToList();
            //     foreach (var path in pathsToProcess)
            //     {
            //         // If file was created/modified but doesn't exist anymore, skip
            //         if (!File.Exists(path) && commandsByPath[path].Type != AssetCommandType.FileDeleted)
            //         {
            //             commandsByPath.Remove(path);
            //         }
            //     }
            //
            //     // Clear pending paths
            //     s_pendingCommandPaths.Clear();
            // }

            // Execute commands
            // NOTE: We many don't need to collect all commands first, just process as we read.
            // Channel in c# is thread-safe for multiple readers/writers.
            //await foreach (var cmd in s_commandChannel.Reader.ReadAllAsync())
            //{
            //    await ExecuteCommandAsync(cmd);
            //}

            while (s_commandChannel.Reader.TryRead(out var cmd))
            {
                await ExecuteCommandAsync(cmd);
            }

            await ImportDirtyAssetsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error processing commands: {ex.Message}");
        }
        finally
        {
            s_resetEventSlim.Set();
        }
    }

    private static async ValueTask ExecuteCommandAsync(AssetCommand command)
    {
        switch (command.Type)
        {
            case AssetCommandType.FileCreated:
                await HandleFileCreatedAsync(command.Path);
                break;

            case AssetCommandType.FileModified:
                await HandleFileModifiedAsync(command.Path);
                break;

            case AssetCommandType.FileDeleted:
                await HandleFileDeletedAsync(command.Path);
                break;

            case AssetCommandType.FileRenamed:
                if (command.OldPath != null)
                {
                    await HandleFileRenamedAsync(command.OldPath, command.Path);
                }
                break;

            case AssetCommandType.ManualRefresh:
                await ValidateAndFixDatabaseAsync(CancellationToken.None);
                break;
        }
    }

    private static async ValueTask HandleFileCreatedAsync(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        await GenerateMetaFileAsync(path, CancellationToken.None);
    }

    private static async ValueTask HandleFileModifiedAsync(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        // Check if file hash changed
        var metaResult = await ReadMetaFileAsync(path, CancellationToken.None);
        if (metaResult.IsFailure)
        {
            // No .gmeta file - treat this as a new file creation
            await HandleFileCreatedAsync(path);
            return;
        }

        var newHash = await CalculateFileHashAsync(path, CancellationToken.None);
        var oldHash = await GetFileHashAsync(metaResult.Value.Guid, CancellationToken.None);

        if (oldHash != newHash)
        {
            // File changed - update database and mark as dirty
            await UpsertAssetAsync(path, metaResult.Value, newHash, null, CancellationToken.None);
            MarkDirty(metaResult.Value.Guid);
        }
    }

    private static async ValueTask HandleFileDeletedAsync(string path)
    {
        var metaFileResult = GetMetaFilePath(path);
        if (metaFileResult.IsSuccess && File.Exists(metaFileResult.Value))
        {
            try
            {
                var metaResult = await ReadMetaFileAsync(path, CancellationToken.None);
                if (metaResult.IsSuccess)
                {
                    var meta = metaResult.Value;

                    // Remove from database
                    await RemoveAssetFromDatabaseAsync(meta.Guid, CancellationToken.None);

                    // Mark dependent assets as dirty
                    await MarkDependentAssetsDirtyAsync(meta.Guid);
                }

                File.Delete(metaFileResult.Value);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error deleting asset metadata: {ex.Message}");
            }
        }
    }

    private static async ValueTask HandleFileRenamedAsync(string oldPath, string newPath)
    {
        var oldMetaPath = oldPath + Utilities.FileExtensions.META_FILE_EXTENSION;
        var newMetaPath = newPath + Utilities.FileExtensions.META_FILE_EXTENSION;

        if (File.Exists(newMetaPath))
        {
            // Validate and update
            await GenerateMetaFileAsync(newPath, CancellationToken.None);
        }
        else if (File.Exists(oldMetaPath))
        {
            // Move meta file
            File.Move(oldMetaPath, newMetaPath);

            // Update database with new path and recalculated hash
            var metaResult = await ReadMetaFileAsync(newPath, CancellationToken.None);
            if (metaResult.IsSuccess)
            {
                var fileHash = await CalculateFileHashAsync(newPath, CancellationToken.None);
                await UpsertAssetAsync(newPath, metaResult.Value, fileHash, null, CancellationToken.None);
            }
        }
        else
        {
            // Generate new meta file
            await GenerateMetaFileAsync(newPath, CancellationToken.None);
        }

        // Delete old meta if it still exists
        if (File.Exists(oldMetaPath) && oldMetaPath != newMetaPath)
        {
            try
            {
                File.Delete(oldMetaPath);
            }
            catch
            {
            }
        }
    }

    internal static void Shutdown()
    {
        lock (s_initializationLock)
        {
            if (!s_initialized)
            {
                return;
            }

            s_watcher?.Dispose();
            s_watcher = null;

            s_commandProcessorTimer?.Dispose();
            s_commandProcessorTimer = null;

            s_dbConnection?.Close();
            s_dbConnection?.Dispose();
            s_dbConnection = null;

            s_assetPathLookup.Clear();
            s_pathAssetLookup.Clear();
            s_dirtyAssets.Clear();
            s_waitingCommands.Clear();
            s_importerInstances.Clear();
            s_importerTypeLookup.Clear();

            s_initialized = false;
        }
    }
}
