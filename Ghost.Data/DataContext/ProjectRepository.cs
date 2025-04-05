using Ghost.Data.Models;
using Ghost.Data.Resources;
using System.Data.SQLite;

namespace Ghost.Data.DataContext;

internal static class ProjectRepository
{
    private static class Command
    {
        public const string CONNECTION_STRING = "Data Source={0}\\projects.db;Version=3;";
        public const string CREATE_PROJECT_TABLE_STRING = "CREATE TABLE IF NOT EXISTS Projects (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Path TEXT, EngineVersion TEXT, LastOpened DATETIME);";
        public const string SELECT_PROJECT_STRING = "SELECT * FROM Projects";
        public const string INSERT_PROJECT_STRING = "INSERT INTO Projects (Name, Path, EngineVersion, LastOpened) VALUES (@Name, @Path, @EngineVersion, @LastOpened);";
        public const string REMOVE_PROJECT_STRING = "DELETE FROM Projects WHERE ID = @ID;";
        public const string UPDATE_PROJECT_STRING = "UPDATE Projects SET Name = @Name, Path = @Path, EngineVersion = @EngineVersion, LastOpened = @LastOpened WHERE ID = @ID;";
    }

    private static string GetConnectionString() => string.Format(Command.CONNECTION_STRING, DataPath.APPLICATION_DATA_FOLDER);

    private static async Task EnsureTableCreatedAsync(SQLiteConnection connection)
    {
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = Command.CREATE_PROJECT_TABLE_STRING;
        await createCommand.ExecuteNonQueryAsync();
    }

    public static async IAsyncEnumerable<ProjectInfo> LoadProjectsAsync()
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync();

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
                Path = reader.GetString(2),
                EngineVersion = new Version(reader.GetString(3)),
                LastOpened = reader.GetDateTime(4)
            };

            yield return project;
        }
    }

    public static async Task AddProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = Command.INSERT_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@Path", project.Path);
        command.Parameters.AddWithValue("@EngineVersion", project.EngineVersion.ToString());
        command.Parameters.AddWithValue("@LastOpened", project.LastOpened);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task RemoveProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = Command.REMOVE_PROJECT_STRING;

        command.Parameters.AddWithValue("@ID", project.ID);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task UpdateProjectAsync(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = Command.UPDATE_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@Path", project.Path);
        command.Parameters.AddWithValue("@EngineVersion", project.EngineVersion.ToString());
        command.Parameters.AddWithValue("@LastOpened", project.LastOpened);
        command.Parameters.AddWithValue("@ID", project.ID); // Ensure the ID parameter is added

        await command.ExecuteNonQueryAsync();
    }
}