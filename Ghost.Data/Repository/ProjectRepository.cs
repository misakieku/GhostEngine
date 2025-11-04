using Ghost.Data.Models;
using Ghost.Data.Resources;
using System.Data.SQLite;

namespace Ghost.Data.Repository;

internal static class ProjectRepository
{
    private static class Command
    {
        public const string CONNECTION_STRING = "Data Source={0}\\projects.db;Version=3;";
        public const string CREATE_PROJECT_TABLE_STRING = "CREATE TABLE IF NOT EXISTS Projects (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, MetadataPath TEXT);";
        public const string SELECT_PROJECT_STRING = "SELECT * FROM Projects";
        public const string INSERT_PROJECT_STRING = "INSERT INTO Projects (Name, MetadataPath) VALUES (@Name, @MetadataPath);";
        public const string REMOVE_PROJECT_STRING = "DELETE FROM Projects WHERE ID = @ID;";
        public const string UPDATE_PROJECT_STRING = "UPDATE Projects SET Name = @Name, MetadataPath = @MetadataPath WHERE ID = @ID;";
    }

    private static async Task EnsureTableCreatedAsync(SQLiteConnection connection)
    {
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = Command.CREATE_PROJECT_TABLE_STRING;
        await createCommand.ExecuteNonQueryAsync();
    }

    public static async IAsyncEnumerable<ProjectInfo> GetAllProjectsAsync()
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.SELECT_PROJECT_STRING;

        using var reader = command.ExecuteReader();
        while (await reader.ReadAsync())
        {
            var project = new ProjectInfo
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                MetadataPath = reader.GetString(2),
            };

            yield return project;
        }
    }

    public static async Task<ProjectInfo?> GetProjectByIdAsync(int id)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.SELECT_PROJECT_STRING + " WHERE ID = @ID;";

        command.Parameters.AddWithValue("@ID", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ProjectInfo
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                MetadataPath = reader.GetString(2),
            };
        }

        return null;
    }

    public static async Task<ProjectInfo?> GetProjectByNameAsync(string name)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.SELECT_PROJECT_STRING + " WHERE Name = @Name;";

        command.Parameters.AddWithValue("@Name", name);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProjectInfo
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                MetadataPath = reader.GetString(2),
            };
        }

        return null;
    }

    public static async Task<ProjectInfo?> GetProjectByMetadataPathAsync(string metadataPath)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.SELECT_PROJECT_STRING + " WHERE MetadataPath = @MetadataPath;";

        command.Parameters.AddWithValue("@MetadataPath", metadataPath);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProjectInfo
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                MetadataPath = reader.GetString(2),
            };
        }

        return null;
    }

    public static async Task AddProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.INSERT_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@MetadataPath", project.MetadataPath);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task RemoveProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = Command.REMOVE_PROJECT_STRING;

        command.Parameters.AddWithValue("@ID", project.ID);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task UpdateProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, DataPath.s_applicationDataFolder));
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = Command.UPDATE_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@MetadataPath", project.MetadataPath);
        command.Parameters.AddWithValue("@ID", project.ID);

        await command.ExecuteNonQueryAsync();
    }
}