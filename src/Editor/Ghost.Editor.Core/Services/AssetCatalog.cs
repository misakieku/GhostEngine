using Ghost.Editor.Core.AssetHandler;
using Microsoft.Data.Sqlite;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Thread-safe SQLite-backed asset catalog.
/// Replaces the in-memory dictionary approach with persistent storage.
/// </summary>
internal sealed class AssetCatalog : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _writeLock = new();

    // Prepared statements
    private readonly SqliteCommand _cmdGetGuid;
    private readonly SqliteCommand _cmdGetPath;
    private readonly SqliteCommand _cmdUpsert;
    private readonly SqliteCommand _cmdDelete;
    private readonly SqliteCommand _cmdMarkDirty;
    private readonly SqliteCommand _cmdMarkImported;
    private readonly SqliteCommand _cmdMarkFailed;
    private readonly SqliteCommand _cmdGetReferencers;
    private readonly SqliteCommand _cmdGetDependencies;
    private readonly SqliteCommand _cmdInsertDep;
    private readonly SqliteCommand _cmdClearDeps;
    private readonly SqliteCommand _cmdGetDirty;
    private readonly SqliteCommand _cmdEnumerate;

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
        _cmdUpsert = CreateCommand(@"
            INSERT INTO assets (guid, source_path, handler_type_id, handler_version, state)
            VALUES (@guid, @path, @handler_id, @version, 0)
            ON CONFLICT(guid) DO UPDATE SET
                source_path = excluded.source_path,
                handler_type_id = excluded.handler_type_id,
                handler_version = excluded.handler_version,
                state = 0;");
        _cmdDelete = CreateCommand("DELETE FROM assets WHERE guid = @guid");
        _cmdMarkDirty = CreateCommand("UPDATE assets SET state = 0 WHERE guid = @guid");
        _cmdMarkImported = CreateCommand(@"
            UPDATE assets SET 
                content_hash = @content_hash, 
                settings_hash = @settings_hash, 
                imported_at_ms = @time, 
                state = 1,
                error_message = NULL
            WHERE guid = @guid");
        _cmdMarkFailed = CreateCommand("UPDATE assets SET state = 2, error_message = @msg WHERE guid = @guid");
        _cmdGetReferencers = CreateCommand("SELECT from_guid FROM dependencies WHERE to_guid = @guid");
        _cmdGetDependencies = CreateCommand("SELECT to_guid FROM dependencies WHERE from_guid = @guid");
        _cmdInsertDep = CreateCommand("INSERT INTO dependencies (from_guid, to_guid) VALUES (@from, @to)");
        _cmdClearDeps = CreateCommand("DELETE FROM dependencies WHERE from_guid = @guid");
        _cmdGetDirty = CreateCommand("SELECT guid, source_path FROM assets WHERE state = 0");
        _cmdEnumerate = CreateCommand("SELECT guid, source_path FROM assets");
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
                state           INTEGER NOT NULL DEFAULT 0,
                error_message   TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_assets_path ON assets(source_path);

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
    }

    public Guid GetGuid(string sourcePath)
    {
        _cmdGetGuid.Parameters.Clear();
        _cmdGetGuid.Parameters.AddWithValue("@path", sourcePath);
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
            _cmdUpsert.Parameters.AddWithValue("@path", sourcePath);
            _cmdUpsert.Parameters.AddWithValue("@handler_id", meta.HandlerTypeId?.ToByteArray() ?? (object)DBNull.Value);
            _cmdUpsert.Parameters.AddWithValue("@version", meta.HandlerVersion);
            _cmdUpsert.ExecuteNonQuery();
        }
    }

    public bool Remove(Guid guid)
    {
        lock (_writeLock)
        {
            _cmdDelete.Parameters.Clear();
            _cmdDelete.Parameters.AddWithValue("@guid", guid.ToByteArray());
            return _cmdDelete.ExecuteNonQuery() > 0;
        }
    }

    public void MarkDirty(Guid guid)
    {
        lock (_writeLock)
        {
            _cmdMarkDirty.Parameters.Clear();
            _cmdMarkDirty.Parameters.AddWithValue("@guid", guid.ToByteArray());
            _cmdMarkDirty.ExecuteNonQuery();
        }
    }

    public void MarkImported(Guid guid, string contentHash, string settingsHash)
    {
        lock (_writeLock)
        {
            _cmdMarkImported.Parameters.Clear();
            _cmdMarkImported.Parameters.AddWithValue("@guid", guid.ToByteArray());
            _cmdMarkImported.Parameters.AddWithValue("@content_hash", contentHash);
            _cmdMarkImported.Parameters.AddWithValue("@settings_hash", settingsHash);
            _cmdMarkImported.Parameters.AddWithValue("@time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _cmdMarkImported.ExecuteNonQuery();
        }
    }

    public void MarkFailed(Guid guid, string error)
    {
        lock (_writeLock)
        {
            _cmdMarkFailed.Parameters.Clear();
            _cmdMarkFailed.Parameters.AddWithValue("@guid", guid.ToByteArray());
            _cmdMarkFailed.Parameters.AddWithValue("@msg", error);
            _cmdMarkFailed.ExecuteNonQuery();
        }
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

    public List<(Guid guid, string sourcePath)> GetDirtyAssets()
    {
        using var reader = _cmdGetDirty.ExecuteReader();
        var list = new List<(Guid guid, string sourcePath)>();
        while (reader.Read())
        {
            list.Add((new Guid((byte[])reader[0]), reader.GetString(1)));
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

    public void Dispose()
    {
        _cmdGetGuid.Dispose();
        _cmdGetPath.Dispose();
        _cmdUpsert.Dispose();
        _cmdDelete.Dispose();
        _cmdMarkDirty.Dispose();
        _cmdMarkImported.Dispose();
        _cmdMarkFailed.Dispose();
        _cmdGetReferencers.Dispose();
        _cmdGetDependencies.Dispose();
        _cmdInsertDep.Dispose();
        _cmdClearDeps.Dispose();
        _cmdGetDirty.Dispose();
        _cmdEnumerate.Dispose();
        _connection.Dispose();
    }
}
