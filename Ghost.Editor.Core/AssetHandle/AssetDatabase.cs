using Ghost.Data.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.AssetHandle;

/// <summary>
/// Centralized asset database that manages all assets in the project.
/// Handles asset registration, lookup, importing, and dependency management.
/// Uses SQLite for persistent storage and efficient querying.
/// </summary>
public static partial class AssetDatabase
{
    private static FileSystemWatcher? s_watcher;
    private static readonly Lock s_dbLock = new();
    private static readonly Dictionary<Guid, string> s_assetPathLookup = new();
    private static readonly Dictionary<string, Guid> s_pathAssetLookup = new();
    
    // Debouncing for file system watcher to prevent duplicate events
    private static readonly Dictionary<string, DateTime> s_pendingFileOperations = new();
    private static readonly Lock s_pendingOperationsLock = new();
    private static readonly TimeSpan s_debounceDelay = TimeSpan.FromMilliseconds(100);
    
    // Initialization guard
    private static readonly Lock s_initializationLock = new();
    private static bool s_initialized = false;

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
    internal static async void Initialize()
    {
        lock (s_initializationLock)
        {
            if (s_initialized)
            {
                return; // Already initialized, skip
            }
            s_initialized = true;
        }

        if (ProjectService.CurrentProject.Metadata == null)
        {
            throw new InvalidOperationException("Project metadata is not initialized. Ensure that the project is loaded before accessing the AssetDatabase.");
        }

        AssetsDirectory = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(ProjectService.CurrentProject.Path)!, ProjectService.ASSETS_FOLDER));

        // Initialize database
        await InitializeDatabaseAsync();

        // Load asset cache from database
        await LoadAssetCacheFromDatabaseAsync();

        // Initialize file system watcher
        s_watcher = new FileSystemWatcher
        {
            Path = AssetsDirectory.FullName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        InitializeAssetHandle();
        InitializeMetaData();

        // Validate and fix database on startup
        await ValidateAndFixDatabaseAsync();
    }

    /// <summary>
    /// Validate the asset database and fix any inconsistencies.
    /// Checks for missing/corrupted assets and regenerates metadata as needed.
    /// </summary>
    private static async Task<Ghost.Core.Result> ValidateAndFixDatabaseAsync()
    {
        if (AssetsDirectory == null)
        {
            return Ghost.Core.Result.Failure("AssetsDirectory not initialized");
        }

        try
        {
            // Scan all files in assets directory
            var allFiles = Directory.GetFiles(AssetsDirectory.FullName, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(Utilities.FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Ensure all files have metadata
            foreach (var file in allFiles)
            {
                var metaPath = file + Utilities.FileExtensions.META_FILE_EXTENSION;
                if (!File.Exists(metaPath))
                {
                    await GenerateMetaFileAsync(file);
                }
                else
                {
                    // Validate and update database
                    var metaResult = await ReadMetaFileAsync(file);
                    if (metaResult.IsSuccess)
                    {
                        var fileHash = await CalculateFileHashAsync(file);
                        await UpsertAssetAsync(file, metaResult.Value, fileHash);
                    }
                    else
                    {
                        // Corrupted meta file - regenerate
                        await GenerateMetaFileAsync(file);
                    }
                }
            }

            // Remove orphaned entries from database (files that no longer exist)
            await RemoveOrphanedEntriesAsync();

            return Ghost.Core.Result.Success();
        }
        catch (Exception ex)
        {
            return Ghost.Core.Result.Failure($"Failed to validate database: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh the asset database manually.
    /// Scans the project directory for changes.
    /// </summary>
    public static async Task<Ghost.Core.Result> RefreshAsync()
    {
        return await ValidateAndFixDatabaseAsync();
    }

    /// <summary>
    /// Check if a file operation should be processed or debounced.
    /// Returns true if the operation should proceed.
    /// </summary>
    private static bool ShouldProcessFileOperation(string filePath)
    {
        lock (s_pendingOperationsLock)
        {
            var now = DateTime.UtcNow;
            
            // Clean up old entries
            var toRemove = s_pendingFileOperations
                .Where(kvp => now - kvp.Value > s_debounceDelay * 2)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in toRemove)
            {
                s_pendingFileOperations.Remove(key);
            }
            
            // Check if this operation was recently processed
            if (s_pendingFileOperations.TryGetValue(filePath, out var lastTime))
            {
                if (now - lastTime < s_debounceDelay)
                {
                    // Too soon, skip this event
                    return false;
                }
            }
            
            // Update timestamp and allow processing
            s_pendingFileOperations[filePath] = now;
            return true;
        }
    }

    /// <summary>
    /// Register a file operation to prevent the file watcher from processing it.
    /// Used by file operations (move, copy, etc.) to prevent duplicate processing.
    /// </summary>
    private static void RegisterFileOperation(string filePath)
    {
        lock (s_pendingOperationsLock)
        {
            s_pendingFileOperations[filePath] = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Shutdown the asset database.
    /// Disposes resources and closes database connections.
    /// </summary>
    internal static void Shutdown()
    {
        lock (s_initializationLock)
        {
            if (!s_initialized)
            {
                return; // Not initialized, nothing to shutdown
            }

            s_watcher?.Dispose();
            s_watcher = null;

            s_dbConnection?.Close();
            s_dbConnection?.Dispose();
            s_dbConnection = null;

            s_assetPathLookup.Clear();
            s_pathAssetLookup.Clear();
            s_importerInstances.Clear();
            s_importerTypeLookup.Clear();
            s_pendingFileOperations.Clear();

            s_initialized = false;
        }
    }
}
