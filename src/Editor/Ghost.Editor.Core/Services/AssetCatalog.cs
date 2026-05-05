using Ghost.Editor.Core.Assets;
using Microsoft.Data.Sqlite;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Thread-safe SQLite-backed asset catalog.
/// Replaces the in-memory dictionary approach with persistent storage.
/// </summary>
public sealed partial class AssetCatalog : IDisposable
{
    public readonly record struct SubAssetInfo(Guid Guid, Guid ParentGuid, string Kind, string DisplayName, string StablePath, string SourcePath, Guid HandlerTypeId);

    private readonly SqliteConnection _connection;
    private readonly Lock _writeLock = new();

    // Prepared statements
    private readonly SqliteCommand _cmdGetGuid;
    private readonly SqliteCommand _cmdGetPath;
    private readonly SqliteCommand _cmdUpsert;
    private readonly SqliteCommand _cmdDelete;

    private readonly SqliteCommand _cmdGetHandlerTypeId;
    private readonly SqliteCommand _cmdGetReferencers;
    private readonly SqliteCommand _cmdGetDependencies;
    private readonly SqliteCommand _cmdGetImportedAt;

    private readonly SqliteCommand _cmdInsertDep;
    private readonly SqliteCommand _cmdClearDeps;
    private readonly SqliteCommand _cmdEnumerate;
    private readonly SqliteCommand _cmdEnumerateSubAssets;
    private readonly SqliteCommand _cmdDeleteSubAssetsForParent;

    public AssetCatalog(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var connString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _connection = new SqliteConnection(connString);
        _connection.Open();

        using (var pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        CreateSchema();

        _cmdGetGuid = CreateCommand("SELECT guid FROM assets WHERE source_path = @path");
        _cmdGetPath = CreateCommand("SELECT source_path FROM assets WHERE guid = @guid");
        _cmdGetHandlerTypeId = CreateCommand("SELECT handler_type_id FROM assets WHERE guid = @guid");
        _cmdGetImportedAt = CreateCommand("SELECT imported_at_ms FROM assets WHERE guid = @guid");

        _cmdUpsert = CreateCommand(@"
            INSERT INTO assets (guid, source_path, handler_type_id, handler_version, content_hash, settings_hash, imported_at_ms, parent_guid, subasset_kind, display_name, stable_path)
            VALUES (@guid, @path, @handler_id, @version, @content_hash, @settings_hash, @imported_at_ms, @parent_guid, @subasset_kind, @display_name, @stable_path)
            ON CONFLICT(guid) DO UPDATE SET
                source_path = excluded.source_path,
                handler_type_id = excluded.handler_type_id,
                handler_version = excluded.handler_version,
                content_hash = excluded.content_hash,
                settings_hash = excluded.settings_hash,
                imported_at_ms = excluded.imported_at_ms,
                parent_guid = excluded.parent_guid,
                subasset_kind = excluded.subasset_kind,
                display_name = excluded.display_name,
                stable_path = excluded.stable_path");
        _cmdDelete = CreateCommand("DELETE FROM assets WHERE guid = @guid");
        _cmdGetReferencers = CreateCommand("SELECT from_guid FROM dependencies WHERE to_guid = @guid");
        _cmdGetDependencies = CreateCommand("SELECT to_guid FROM dependencies WHERE from_guid = @guid");
        
        _cmdInsertDep = CreateCommand("INSERT INTO dependencies (from_guid, to_guid) VALUES (@from, @to)");
        _cmdClearDeps = CreateCommand("DELETE FROM dependencies WHERE from_guid = @guid");
        _cmdEnumerate = CreateCommand("SELECT guid, source_path FROM assets");
        _cmdEnumerateSubAssets = CreateCommand("SELECT guid, parent_guid, subasset_kind, display_name, stable_path, source_path, handler_type_id FROM assets WHERE parent_guid = @parent_guid ORDER BY stable_path");
        _cmdDeleteSubAssetsForParent = CreateCommand("DELETE FROM assets WHERE parent_guid = @parent_guid");
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    private void CreateSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS assets (
                guid            BLOB(16) PRIMARY KEY NOT NULL,
                source_path     TEXT NOT NULL,
                handler_type_id BLOB(16),
                handler_version INTEGER NOT NULL DEFAULT 0,
                content_hash    TEXT,
                settings_hash   TEXT,
                imported_at_ms  INTEGER,
                parent_guid     BLOB(16),
                subasset_kind   TEXT,
                display_name    TEXT,
                stable_path     TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_assets_path ON assets(source_path);
            CREATE INDEX IF NOT EXISTS idx_assets_parent ON assets(parent_guid);

            CREATE TABLE IF NOT EXISTS dependencies (
                from_guid   BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
                to_guid     BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
                PRIMARY KEY (from_guid, to_guid)
            );
            CREATE INDEX IF NOT EXISTS idx_dep_reverse ON dependencies(to_guid);

            CREATE TABLE IF NOT EXISTS labels (
                guid    BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
                label   TEXT NOT NULL,
                PRIMARY KEY (guid, label)
            );
            CREATE INDEX IF NOT EXISTS idx_labels_label ON labels(label);";
        cmd.ExecuteNonQuery();

        TryAddColumn("assets", "parent_guid", "BLOB(16)");
        TryAddColumn("assets", "subasset_kind", "TEXT");
        TryAddColumn("assets", "display_name", "TEXT");
        TryAddColumn("assets", "stable_path", "TEXT");

        using var indexCmd = _connection.CreateCommand();
        indexCmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_assets_parent ON assets(parent_guid);";
        indexCmd.ExecuteNonQuery();
    }

    private void TryAddColumn(string tableName, string columnName, string columnType)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
        }
    }

    private static string ToUniversalPath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }

        return path;
    }

    public Guid GetGuid(string sourcePath)
    {
        _cmdGetGuid.Parameters.Clear();
        _cmdGetGuid.Parameters.AddWithValue("@path", ToUniversalPath(sourcePath));
        var result = _cmdGetGuid.ExecuteScalar();
        return result is byte[] bytes ? new Guid(bytes) : Guid.Empty;
    }

    public string? GetSourcePath(Guid guid)
    {
        _cmdGetPath.Parameters.Clear();
        _cmdGetPath.Parameters.AddWithValue("@guid", guid.ToByteArray());
        return _cmdGetPath.ExecuteScalar() as string;
    }

    public void Upsert(AssetMeta meta, string sourcePath)
    {
        lock (_writeLock)
        {
            _cmdUpsert.Parameters.Clear();
            _cmdUpsert.Parameters.AddWithValue("@guid", meta.Guid.ToByteArray());
            _cmdUpsert.Parameters.AddWithValue("@path", ToUniversalPath(sourcePath));
            _cmdUpsert.Parameters.AddWithValue("@handler_id", meta.HandlerTypeId?.ToByteArray() ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@version", meta.HandlerVersion);
            _cmdUpsert.Parameters.AddWithValue("@content_hash", meta.ContentHash ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@settings_hash", meta.SettingsHash ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@imported_at_ms", meta.LastImportedUtc?.Ticks ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@parent_guid", DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@subasset_kind", DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@display_name", DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@stable_path", DBNull.Value);
            _cmdUpsert.ExecuteNonQuery();
        }
    }

    public void UpsertSubAsset(Guid parentGuid, AssetMeta meta, string sourcePath, string kind, string displayName, string stablePath)
    {
        lock (_writeLock)
        {
            _cmdUpsert.Parameters.Clear();
            _cmdUpsert.Parameters.AddWithValue("@guid", meta.Guid.ToByteArray());
            _cmdUpsert.Parameters.AddWithValue("@path", ToUniversalPath(sourcePath));
            _cmdUpsert.Parameters.AddWithValue("@handler_id", meta.HandlerTypeId?.ToByteArray() ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@version", meta.HandlerVersion);
            _cmdUpsert.Parameters.AddWithValue("@content_hash", meta.ContentHash ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@settings_hash", meta.SettingsHash ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@imported_at_ms", meta.LastImportedUtc?.Ticks ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@parent_guid", parentGuid.ToByteArray());
            _cmdUpsert.Parameters.AddWithValue("@subasset_kind", kind);
            _cmdUpsert.Parameters.AddWithValue("@display_name", displayName);
            _cmdUpsert.Parameters.AddWithValue("@stable_path", stablePath);
            _cmdUpsert.ExecuteNonQuery();
        }
    }

    public bool Remove(Guid guid)
    {
        var subAssets = GetSubAssets(guid);
        foreach (var sub in subAssets)
        {
            Remove(sub.Guid);
        }

        lock (_writeLock)
        {
            _cmdDelete.Parameters.Clear();
            _cmdDelete.Parameters.AddWithValue("@guid", guid.ToByteArray());
            return _cmdDelete.ExecuteNonQuery() > 0;
        }
    }

    public Guid GetHandlerTypeId(Guid guid)
    {
        _cmdGetHandlerTypeId.Parameters.Clear();
        _cmdGetHandlerTypeId.Parameters.AddWithValue("@guid", guid.ToByteArray());
        var result = _cmdGetHandlerTypeId.ExecuteScalar();
        return result is byte[] bytes ? new Guid(bytes) : Guid.Empty;
    }

    public DateTime? GetImportedAt(Guid guid)
    {
        _cmdGetImportedAt.Parameters.Clear();
        _cmdGetImportedAt.Parameters.AddWithValue("@guid", guid.ToByteArray());
        var result = _cmdGetImportedAt.ExecuteScalar();

        if (result is long ticks)
        {
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        return null;
    }

    public void SetDependencies(Guid assetId, ReadOnlySpan<Guid> dependencies)
    {
        lock (_writeLock)
        {
            using var tx = _connection.BeginTransaction();
            _cmdClearDeps.Transaction = tx;
            _cmdClearDeps.Parameters.Clear();
            _cmdClearDeps.Parameters.AddWithValue("@guid", assetId.ToByteArray());
            _cmdClearDeps.ExecuteNonQuery();

            _cmdInsertDep.Transaction = tx;
            foreach (var dep in dependencies)
            {
                _cmdInsertDep.Parameters.Clear();
                _cmdInsertDep.Parameters.AddWithValue("@from", assetId.ToByteArray());
                _cmdInsertDep.Parameters.AddWithValue("@to", dep.ToByteArray());
                _cmdInsertDep.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    public List<Guid> GetReferencers(Guid guid)
    {
        _cmdGetReferencers.Parameters.Clear();
        _cmdGetReferencers.Parameters.AddWithValue("@guid", guid.ToByteArray());

        using var reader = _cmdGetReferencers.ExecuteReader();
        var list = new List<Guid>();
        while (reader.Read())
        {
            list.Add(new Guid((byte[])reader[0]));
        }

        return list;
    }

    public List<Guid> GetDependencies(Guid guid)
    {
        _cmdGetDependencies.Parameters.Clear();
        _cmdGetDependencies.Parameters.AddWithValue("@guid", guid.ToByteArray());

        using var reader = _cmdGetDependencies.ExecuteReader();
        var list = new List<Guid>();
        while (reader.Read())
        {
            list.Add(new Guid((byte[])reader[0]));
        }

        return list;
    }

    public IEnumerable<(Guid guid, string sourcePath)> EnumerateAll()
    {
        using var reader = _cmdEnumerate.ExecuteReader();
        while (reader.Read())
        {
            yield return (new Guid((byte[])reader[0]), reader.GetString(1));
        }
    }

    public List<SubAssetInfo> GetSubAssets(Guid parentGuid)
    {
        _cmdEnumerateSubAssets.Parameters.Clear();
        _cmdEnumerateSubAssets.Parameters.AddWithValue("@parent_guid", parentGuid.ToByteArray());

        using var reader = _cmdEnumerateSubAssets.ExecuteReader();
        var list = new List<SubAssetInfo>();
        while (reader.Read())
        {
            list.Add(new SubAssetInfo(
                new Guid((byte[])reader[0]),
                new Guid((byte[])reader[1]),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                new Guid((byte[])reader[6])));
        }

        return list;
    }

    public void RemoveSubAssetsExcept(Guid parentGuid, ReadOnlySpan<Guid> keepGuids)
    {
        lock (_writeLock)
        {
            if (keepGuids.Length == 0)
            {
                _cmdDeleteSubAssetsForParent.Parameters.Clear();
                _cmdDeleteSubAssetsForParent.Parameters.AddWithValue("@parent_guid", parentGuid.ToByteArray());
                _cmdDeleteSubAssetsForParent.ExecuteNonQuery();
                return;
            }

            var keep = new HashSet<Guid>();
            for (var i = 0; i < keepGuids.Length; i++)
            {
                keep.Add(keepGuids[i]);
            }

            foreach (var subAsset in GetSubAssets(parentGuid))
            {
                if (!keep.Contains(subAsset.Guid))
                {
                    _cmdDelete.Parameters.Clear();
                    _cmdDelete.Parameters.AddWithValue("@guid", subAsset.Guid.ToByteArray());
                    _cmdDelete.ExecuteNonQuery();
                }
            }
        }
    }

    public void Dispose()
    {
        _cmdGetGuid.Dispose();
        _cmdGetPath.Dispose();
        _cmdUpsert.Dispose();
        _cmdDelete.Dispose();
        _cmdGetHandlerTypeId.Dispose();
        _cmdGetReferencers.Dispose();
        _cmdGetDependencies.Dispose();
        _cmdInsertDep.Dispose();
        _cmdClearDeps.Dispose();
        _cmdEnumerate.Dispose();
        _cmdEnumerateSubAssets.Dispose();
        _cmdDeleteSubAssetsForParent.Dispose();
        _connection.Dispose();
    }
}
