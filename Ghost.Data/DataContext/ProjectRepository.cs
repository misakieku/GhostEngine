using Ghost.Data.Models;
using Ghost.Data.Resources;
using System.Data.SQLite;

namespace Ghost.Data.DataContext;

internal static class ProjectRepository
{
    private const string _CONNECTION_STRING = "Data Source={0}\\projects.db;Version=3;";
    private const string _CREATE_PROJECT_TABLE_STRING = "CREATE TABLE IF NOT EXISTS Projects (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Path TEXT, EngineVersion TEXT, LastOpened DATETIME);";
    private const string _SELECT_PROJECT_STRING = "SELECT * FROM Projects";
    private const string _INSERT_PROJECT_STRING = "INSERT INTO Projects (Name, Path, EngineVersion, LastOpened) VALUES (@Name, @Path, @EngineVersion, @LastOpened);";

    private static string GetConnectionString() => string.Format(_CONNECTION_STRING, DataPath.ApplicationDataFolder);

    private static async Task EnsureTableCreatedAsync(SQLiteConnection connection)
    {
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = _CREATE_PROJECT_TABLE_STRING;
        await createCommand.ExecuteNonQueryAsync();
    }

    public static async IAsyncEnumerable<ProjectInfo> LoadProjectsAsync()
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync();

        await EnsureTableCreatedAsync(connection);

        using var command = connection.CreateCommand();
        command.CommandText = _SELECT_PROJECT_STRING;

        using var reader = command.ExecuteReader();
        while (await reader.ReadAsync())
        {
            var project = new ProjectInfo
            {
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
        command.CommandText = _INSERT_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@Path", project.Path);
        command.Parameters.AddWithValue("@EngineVersion", project.EngineVersion.ToString());
        command.Parameters.AddWithValue("@LastOpened", project.LastOpened);
        await command.ExecuteNonQueryAsync();
    }
}