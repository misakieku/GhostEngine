using Ghost.Core;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetService
{
    private static SqliteConnection? s_dbConnection;

    /// <summary>
    /// Initialize the SQLite database for asset caching.
    /// </summary>
    private static async Task InitializeDatabaseAsync(CancellationToken token = default)
    {
        if (AssetsDirectory == null)
        {
            throw new InvalidOperationException("AssetsDirectory is not set. Initialize() must be called first.");
        }

        var dbPath = Path.Combine(AssetsDirectory.Parent!.FullName, EditorApplication.CACHES_FOLDER_NAME, "AssetDatabase.db");
        var cacheDir = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir!);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        s_dbConnection = new SqliteConnection(connectionString);
        await s_dbConnection.OpenAsync(token);

        // Create tables
        await using var cmd = s_dbConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Assets (
                Guid TEXT PRIMARY KEY,
                Path TEXT NOT NULL,
                Version INTEGER NOT NULL,
                Tags TEXT,
                FileHash TEXT,
                DependencyGuids TEXT,
                LastModified INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_path ON Assets(Path);
        ";

        await cmd.ExecuteNonQueryAsync(token);
    }

    /// <summary>
    /// Add or update an asset in the database.
    /// </summary>
    /// <param name="assetPath">Full path to the asset file.</param>
    /// <param name="meta">Asset metadata from .gmeta file.</param>
    /// <param name="fileHash">SHA256 hash of the asset file content.</param>
    /// <param name="dependencies">List of GUIDs this asset depends on (extracted during import).</param>
    private static async ValueTask<Result> UpsertAssetAsync(string assetPath, AssetMeta meta, string fileHash, List<Guid>? dependencies = null, CancellationToken token = default)
    {
        if (s_dbConnection == null)
        {
            return Result.Failure("Database not initialized");
        }

        var relativePath = GetRelativePath(assetPath);
        if (relativePath.IsFailure)
        {
            return Result.Failure(relativePath.Message);
        }

        try
        {
            lock (s_dbLock)
            {
                // If this GUID already exists with a different path, remove the old path mapping
                if (s_assetPathLookup.TryGetValue(meta.Guid, out var oldPath) && oldPath != relativePath.Value)
                {
                    s_pathAssetLookup.Remove(oldPath);
                }

                // Update lookups with new path (normalize path separators for consistency)
                var normalizedPath = relativePath.Value.Replace('\\', '/');
                s_assetPathLookup[meta.Guid] = normalizedPath;
                s_pathAssetLookup[normalizedPath] = meta.Guid;
            }

            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Assets (Guid, Path, Version, Tags, FileHash, DependencyGuids, LastModified)
                VALUES (@guid, @path, @version, @tags, @fileHash, @deps, @modified)
            ";
            cmd.Parameters.AddWithValue("@guid", meta.Guid.ToString());
            cmd.Parameters.AddWithValue("@path", relativePath.Value);
            cmd.Parameters.AddWithValue("@version", meta.Version);
            cmd.Parameters.AddWithValue("@tags", JsonSerializer.Serialize(meta.Tags));
            cmd.Parameters.AddWithValue("@fileHash", fileHash);
            cmd.Parameters.AddWithValue("@deps", JsonSerializer.Serialize(dependencies ?? new List<Guid>()));
            cmd.Parameters.AddWithValue("@modified", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            await cmd.ExecuteNonQueryAsync(token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to upsert asset: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove an asset from the database.
    /// </summary>
    private static async Task<Result> RemoveAssetFromDatabaseAsync(Guid guid, CancellationToken token = default)
    {
        if (s_dbConnection == null)
        {
            return Result.Failure("Database not initialized");
        }

        try
        {
            lock (s_dbLock)
            {
                if (s_assetPathLookup.TryGetValue(guid, out var path))
                {
                    s_assetPathLookup.Remove(guid);
                    s_pathAssetLookup.Remove(path);
                }
            }

            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "DELETE FROM Assets WHERE Guid = @guid";
            cmd.Parameters.AddWithValue("@guid", guid.ToString());

            await cmd.ExecuteNonQueryAsync(token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to remove asset: {ex.Message}");
        }
    }



    /// <summary>
    /// Load all assets from the database into memory cache.
    /// </summary>
    private static async Task LoadAssetCacheFromDatabaseAsync(CancellationToken token = default)
    {
        if (s_dbConnection == null)
        {
            return;
        }

        try
        {
            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "SELECT Guid, Path FROM Assets";

            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var guidStr = reader.GetString(0);
                var path = reader.GetString(1);

                if (Guid.TryParse(guidStr, out var guid))
                {
                    lock (s_dbLock)
                    {
                        s_assetPathLookup[guid] = path;
                        s_pathAssetLookup[path] = guid;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load asset cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get assets by tag.
    /// </summary>
    private static async Task<List<Guid>> GetAssetsByTagAsync(string tag, CancellationToken token = default)
    {
        var result = new List<Guid>();

        if (s_dbConnection == null)
        {
            return result;
        }

        try
        {
            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "SELECT Guid, Tags FROM Assets";

            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var guidStr = reader.GetString(0);
                var tagsJson = reader.GetString(1);

                if (Guid.TryParse(guidStr, out var guid))
                {
                    var tags = JsonSerializer.Deserialize<List<string>>(tagsJson);
                    if (tags != null && tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(guid);
                    }
                }
            }
        }
        catch
        {
            // Silently fail
        }

        return result;
    }

    /// <summary>
    /// Get the file hash for an asset from the database.
    /// </summary>
    private static async Task<string?> GetFileHashAsync(Guid guid, CancellationToken token = default)
    {
        if (s_dbConnection == null)
        {
            return null;
        }

        try
        {
            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "SELECT FileHash FROM Assets WHERE Guid = @guid";
            cmd.Parameters.AddWithValue("@guid", guid.ToString());

            var result = await cmd.ExecuteScalarAsync(token);
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the dependencies for an asset from the database.
    /// </summary>
    private static async Task<List<Guid>> GetDependenciesAsync(Guid guid, CancellationToken token = default)
    {
        if (s_dbConnection == null)
        {
            return new List<Guid>();
        }

        try
        {
            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "SELECT DependencyGuids FROM Assets WHERE Guid = @guid";
            cmd.Parameters.AddWithValue("@guid", guid.ToString());

            var result = await cmd.ExecuteScalarAsync(token);
            if (result != null)
            {
                var json = result.ToString();
                return JsonSerializer.Deserialize<List<Guid>>(json ?? "[]") ?? new List<Guid>();
            }
        }
        catch
        {
            // Silently fail
        }

        return new List<Guid>();
    }

    /// <summary>
    /// Find assets by name pattern using database query with wildcards.
    /// </summary>
    /// <param name="namePattern">Pattern supporting * (any chars) and ? (single char).</param>
    private static async Task<List<Guid>> GetAssetsByNameAsync(string namePattern, CancellationToken token = default)
    {
        var results = new List<Guid>();

        if (s_dbConnection == null)
        {
            return results;
        }

        try
        {
            // Convert wildcard pattern to SQL LIKE pattern
            var sqlPattern = namePattern.Replace('*', '%').Replace('?', '_');

            await using var cmd = s_dbConnection.CreateCommand();

            // Extract just the filename from the path for matching
            // SQLite doesn't have a built-in path manipulation, so we search in the full path
            // and filter by checking if the pattern matches the filename part
            cmd.CommandText = @"
                SELECT Guid, Path FROM Assets 
                WHERE Path LIKE '%' || @pattern || '%'
            ";
            cmd.Parameters.AddWithValue("@pattern", sqlPattern);

            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var guidStr = reader.GetString(0);
                var path = reader.GetString(1);

                // Extract filename and check if it matches the pattern
                var fileName = Path.GetFileName(path);

                // Convert pattern to regex for proper matching
                var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(namePattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (Guid.TryParse(guidStr, out var guid))
                    {
                        results.Add(guid);
                    }
                }
            }
        }
        catch
        {
            // Silently fail
        }

        return results;
    }

    /// <summary>
    /// Remove orphaned entries from database (assets that no longer exist on disk).
    /// </summary>
    private static async Task RemoveOrphanedEntriesAsync(CancellationToken token = default)
    {
        if (s_dbConnection == null || AssetsDirectory == null)
        {
            return;
        }

        try
        {
            var orphanedGuids = new List<Guid>();

            await using var cmd = s_dbConnection.CreateCommand();
            cmd.CommandText = "SELECT Guid, Path FROM Assets";

            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var guidStr = reader.GetString(0);
                var path = reader.GetString(1);

                if (Guid.TryParse(guidStr, out var guid))
                {
                    // Check if file exists
                    var fullPath = Path.Combine(AssetsDirectory.FullName, path);
                    if (!File.Exists(fullPath))
                    {
                        orphanedGuids.Add(guid);
                    }
                }
            }

            // Remove orphaned entries
            foreach (var guid in orphanedGuids)
            {
                await RemoveAssetFromDatabaseAsync(guid, token);
            }
        }
        catch
        {
            // Silently fail - cleanup is best effort
        }
    }
}
