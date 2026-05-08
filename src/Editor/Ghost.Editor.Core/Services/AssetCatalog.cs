using Ghost.Editor.Core.Assets;
using Microsoft.Data.Sqlite;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Thread-safe SQLite-backed asset catalog.
/// Uses connection pooling and local command creation for safe multi-threaded access.
/// </summary>
public sealed partial class AssetCatalog
{
    public readonly record struct SubAssetInfo(Guid Guid, Guid ParentGuid, string Kind, string DisplayName, string StablePath, string SourcePath, Guid AssetTypeId);

    private readonly string _connectionString;

    private const string SqlGetGuid = "SELECT guid FROM assets WHERE source_path = @path";
    private const string SqlGetPath = "SELECT source_path FROM assets WHERE guid = @guid";
    private const string SqlGetAssetTypeId = "SELECT asset_type_id FROM assets WHERE guid = @guid";
    private const string SqlGetImportedAt = "SELECT imported_at_ms FROM assets WHERE guid = @guid";
    private const string SqlUpsert = @"
            INSERT INTO assets (guid, source_path, asset_type_id, handler_version, content_hash, settings_hash, imported_at_ms, parent_guid, subasset_kind, display_name, stable_path)
            VALUES (@guid, @path, @asset_type_id, @version, @content_hash, @settings_hash, @imported_at_ms, @parent_guid, @subasset_kind, @display_name, @stable_path)
            ON CONFLICT(guid) DO UPDATE SET
                source_path = excluded.source_path,
                asset_type_id = excluded.asset_type_id,
                handler_version = excluded.handler_version,
                content_hash = excluded.content_hash,
                settings_hash = excluded.settings_hash,
                imported_at_ms = excluded.imported_at_ms,
                parent_guid = excluded.parent_guid,
                subasset_kind = excluded.subasset_kind,
                display_name = excluded.display_name,
                stable_path = excluded.stable_path";
    private const string SqlDelete = "DELETE FROM assets WHERE guid = @guid";
    private const string SqlGetReferencers = "SELECT from_guid FROM dependencies WHERE to_guid = @guid";
    private const string SqlGetDependencies = "SELECT to_guid FROM dependencies WHERE from_guid = @guid";
    private const string SqlInsertDep = "INSERT INTO dependencies (from_guid, to_guid) VALUES (@from, @to)";
    private const string SqlClearDeps = "DELETE FROM dependencies WHERE from_guid = @guid";
    private const string SqlEnumerate = "SELECT guid, source_path FROM assets";
    private const string SqlEnumerateSubAssets = "SELECT guid, parent_guid, subasset_kind, display_name, stable_path, source_path, asset_type_id FROM assets WHERE parent_guid = @parent_guid ORDER BY stable_path";
    private const string SqlDeleteSubAssetsForParent = "DELETE FROM assets WHERE parent_guid = @parent_guid";

    public AssetCatalog(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true,
            Pooling = true,
        };
        _connectionString = builder.ToString();

        // Initial setup
        using var connection = OpenConnection();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode = WAL;";
            cmd.ExecuteNonQuery();
        }

        CreateSchemaInternal(connection);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void CreateSchemaInternal(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS assets (
                guid            BLOB (16) PRIMARY KEY NOT NULL,
                source_path     TEXT      NOT NULL,
                asset_type_id   BLOB (16),
                handler_version INTEGER   NOT NULL DEFAULT 0,
                content_hash    TEXT,
                settings_hash   TEXT,
                imported_at_ms  INTEGER,
                parent_guid     BLOB (16),
                subasset_kind   TEXT,
                display_name    TEXT,
                stable_path     TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_assets_path ON assets(source_path);
            CREATE INDEX IF NOT EXISTS idx_assets_parent ON assets(parent_guid);
            CREATE INDEX IF NOT EXISTS idx_assets_type_id ON assets(asset_type_id);

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
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlGetGuid;
        cmd.Parameters.AddWithValue("@path", ToUniversalPath(sourcePath));
        var result = cmd.ExecuteScalar();
        return result is byte[] bytes ? new Guid(bytes) : Guid.Empty;
    }

    public string? GetSourcePath(Guid guid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SqlGetPath;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        return cmd.ExecuteScalar() as string;
    }

    private void UpsertInternal(AssetMeta meta, string sourcePath, Guid? parentGuid, string? kind, string? displayName, string? stablePath)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SqlUpsert;
        cmd.Parameters.AddWithValue("@guid", meta.Guid.ToByteArray());
        cmd.Parameters.AddWithValue("@path", ToUniversalPath(sourcePath));
        cmd.Parameters.AddWithValue("@asset_type_id", meta.AssetTypeId?.ToByteArray() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@version", meta.HandlerVersion);
        cmd.Parameters.AddWithValue("@content_hash", meta.ContentHash ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@settings_hash", meta.SettingsHash ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@imported_at_ms", meta.LastImportedUtc?.Ticks ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@parent_guid", parentGuid?.ToByteArray() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@subasset_kind", (object?)kind ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@display_name", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@stable_path", (object?)stablePath ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void Upsert(AssetMeta meta, string sourcePath) => UpsertInternal(meta, sourcePath, null, null, null, null);

    public void UpsertSubAsset(Guid parentGuid, AssetMeta meta, string sourcePath, string kind, string displayName, string stablePath)
        => UpsertInternal(meta, sourcePath, parentGuid, kind, displayName, stablePath);

    public bool Remove(Guid guid)
    {
        var subAssets = GetSubAssets(guid);
        foreach (var sub in subAssets)
        {
            Remove(sub.Guid);
        }

        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlDelete;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        return cmd.ExecuteNonQuery() > 0;
    }

    public Guid GetAssetTypeId(Guid guid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlGetAssetTypeId;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        
        var result = cmd.ExecuteScalar();
        return result is byte[] bytes ? new Guid(bytes) : Guid.Empty;
    }

    public DateTime? GetImportedAt(Guid guid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlGetImportedAt;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        
        var result = cmd.ExecuteScalar();
        return result is long ticks ? new DateTime(ticks, DateTimeKind.Utc) : null;
    }

    public void SetDependencies(Guid assetId, ReadOnlySpan<Guid> dependencies)
    {
        using var connection = OpenConnection();
        using var tx = connection.BeginTransaction();

        using (var clearCmd = connection.CreateCommand())
        {
            clearCmd.Transaction = tx;
            clearCmd.CommandText = SqlClearDeps;
            clearCmd.Parameters.AddWithValue("@guid", assetId.ToByteArray());
            clearCmd.ExecuteNonQuery();
        }

        if (dependencies.Length > 0)
        {
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = tx;
            insertCmd.CommandText = SqlInsertDep;
            var fromParam = insertCmd.Parameters.Add("@from", SqliteType.Blob);
            var toParam = insertCmd.Parameters.Add("@to", SqliteType.Blob);
            fromParam.Value = assetId.ToByteArray();

            foreach (var dep in dependencies)
            {
                toParam.Value = dep.ToByteArray();
                insertCmd.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }

    public List<Guid> GetReferencers(Guid guid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlGetReferencers;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        
        using var reader = cmd.ExecuteReader();
        var list = new List<Guid>();
        while (reader.Read())
        {
            list.Add(new Guid((byte[])reader[0]));
        }

        return list;
    }

    public List<Guid> GetDependencies(Guid guid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlGetDependencies;
        cmd.Parameters.AddWithValue("@guid", guid.ToByteArray());
        
        using var reader = cmd.ExecuteReader();
        var list = new List<Guid>();
        while (reader.Read())
        {
            list.Add(new Guid((byte[])reader[0]));
        }

        return list;
    }

    public IEnumerable<(Guid guid, string sourcePath)> EnumerateAll()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = SqlEnumerate;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return (new Guid((byte[])reader[0]), reader.GetString(1));
        }
    }

    public IEnumerable<Guid> EnumerateByTypes(params Guid[] assetTypeIds)
    {
        if (assetTypeIds.Length == 0)
        {
            yield break;
        }

        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();

        var parameterNames = new List<string>(assetTypeIds.Length);
        for (int i = 0; i < assetTypeIds.Length; i++)
        {
            string paramName = $"@typeId{i}";
            parameterNames.Add(paramName);
            cmd.Parameters.AddWithValue(paramName, assetTypeIds[i].ToByteArray());
        }

        cmd.CommandText = $"SELECT guid FROM assets WHERE asset_type_id IN ({string.Join(", ", parameterNames)})";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return new Guid((byte[])reader[0]);
        }
    }

    public List<SubAssetInfo> GetSubAssets(Guid parentGuid)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        
        cmd.CommandText = SqlEnumerateSubAssets;
        cmd.Parameters.AddWithValue("@parent_guid", parentGuid.ToByteArray());
        
        using var reader = cmd.ExecuteReader();
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
        if (keepGuids.Length == 0)
        {
            using var connection = OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = SqlDeleteSubAssetsForParent;
            cmd.Parameters.AddWithValue("@parent_guid", parentGuid.ToByteArray());
            cmd.ExecuteNonQuery();
            return;
        }

        var keep = new HashSet<Guid>(keepGuids.Length);
        foreach (var guid in keepGuids)
        {
            keep.Add(guid);
        }

        foreach (var subAsset in GetSubAssets(parentGuid))
        {
            if (!keep.Contains(subAsset.Guid))
            {
                Remove(subAsset.Guid);
            }
        }
    }
}