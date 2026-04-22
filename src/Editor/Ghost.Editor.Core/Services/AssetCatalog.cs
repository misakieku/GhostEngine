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
    private readonly Lock _writeLock = new();

    // Prepared statements
    private readonly SqliteCommand _cmdGetGuid;
    private readonly SqliteCommand _cmdGetPath;
    private readonly SqliteCommand _cmdUpsert;
    private readonly SqliteCommand _cmdDelete;
    private readonly SqliteCommand _cmdGetHandlerTypeId;
    private readonly SqliteCommand _cmdGetReferencers;
    private readonly SqliteCommand _cmdGetDependencies;
    private readonly SqliteCommand _cmdInsertDep;
    private readonly SqliteCommand _cmdClearDeps;
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
        _cmdGetHandlerTypeId = CreateCommand("SELECT handler_type_id FROM assets WHERE guid = @guid");
        _cmdUpsert = CreateCommand(@"
            INSERT INTO assets (guid, source_path, handler_type_id, handler_version)
            VALUES (@guid, @path, @handler_id, @version)
            ON CONFLICT(guid) DO UPDATE SET
                source_path = excluded.source_path,
                handler_type_id = excluded.handler_type_id,
                handler_version = excluded.handler_version");
        _cmdDelete = CreateCommand("DELETE FROM assets WHERE guid = @guid");
        _cmdGetReferencers = CreateCommand("SELECT from_guid FROM dependencies WHERE to_guid = @guid");
        _cmdGetDependencies = CreateCommand("SELECT to_guid FROM dependencies WHERE from_guid = @guid");
        _cmdInsertDep = CreateCommand("INSERT INTO dependencies (from_guid, to_guid) VALUES (@from, @to)");
        _cmdClearDeps = CreateCommand("DELETE FROM dependencies WHERE from_guid = @guid");
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
                imported_at_ms  INTEGER
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

    public Guid GetHandlerTypeId(Guid guid)
    {
        _cmdGetHandlerTypeId.Parameters.Clear();
        _cmdGetHandlerTypeId.Parameters.AddWithValue("@guid", guid.ToByteArray());
        var result = _cmdGetHandlerTypeId.ExecuteScalar();
        return result is byte[] bytes ? new Guid(bytes) : Guid.Empty;
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
        _connection.Dispose();
    }
}
